app.factory('noteService', function($http, $rootScope, $q, loginService) {

    var noteService = {};
    var notes, latest_sync_revision, manifest;

    function initialize () {
        notes = null;

        latest_sync_revision = -1;
        manifest = {
            taintedNotes: [],
            deletedNotes: [],
        };
    }

    initialize();

    Object.defineProperty(noteService, 'notebooks', {
        get: function () {
            return buildNotebooks(notes);
        }
    });

    Object.defineProperty(noteService, 'notes', {
        get: function () {
            return filterDeletedNotes(notes);
        }
    });

    Object.defineProperty(noteService, 'needsSyncing', {
        get: function () {
            return manifest.taintedNotes.length > 0 || manifest.deletedNotes.length > 0;
        }
    });

    $rootScope.$on('loginStatus', function(ev, isLoggedIn) {
        if (!isLoggedIn) {
            console.log('cleaning note service');
            initialize();
        }
    });

    function getNotebookFromNote (note) {
        var nb_name = null;
        _.each(note.tags, function (tag) {
            if (tag.startsWith('system:notebook:')) {
                nb_name = tag.substring(16);
            }
        });
        return nb_name;
    }

    function notesByNotebook (notes, notebook_name) {
        if (notebook_name) {
            return _.filter(notes, function (note) {
                var nb = getNotebookFromNote(note);
                return nb === notebook_name;
            });
        } else {
            // return notes that don't have a notebook
            return _.filter(notes, function (note) {
                return getNotebookFromNote(note) === null;
            });
        }
    }

    function buildNotebooks (notes) {
        var notebooks = {};
        var notebook_names = [];

        notebooks.Unsorted = notesByNotebook(notes);

        _.each(notes, function (note) {
            var nb = getNotebookFromNote (note);
            if (nb)
                notebook_names.push(nb);
        });
        notebook_names = _.uniq(notebook_names);

        _.each(notebook_names, function(name) {
            notebooks[name] = notesByNotebook(notes, name);
        });

        // filter out notes marked as deleted & empty notebooks
        var filtered_nb = {};
        for (var nb in notebooks) {
            var filtered = filterDeletedNotes(notebooks[nb]);
            if (filtered.length > 0)
                filtered_nb[nb] =  filtered;
        }

        return filtered_nb;
    }

    function filterDeletedNotes(notes) {
        var filtered = _.filter(notes, function(note) {
            return !_.contains(manifest.deletedNotes, note.guid);
        });
        return filtered;
    }

    function guid () {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            var r = Math.random()*16|0, v = c === 'x' ? r : (r&0x3|0x8);
            return v.toString(16);
        });
    }

    // PUBLIC functions
    //
    noteService.getNoteByGuid = function (guid) {
        if (noteService.notes.length === 0)
            return null;
        return _.findWhere(notes, {guid: guid});
    };

    noteService.fetchNotes = function(force) {

        if (notes !== null && !force) return;

        manifest.taintedNotes = [];
        manifest.deletedNotes = [];

        $http({
            method: 'GET',
            url: '/api/1.0/' + loginService.username + '/notes?since=' + latest_sync_revision + '&include_notes=true&notes_as_html=true',
            headers: { 'AccessToken': loginService.accessToken }
        }).success(function (data, status, headers, config) {
            notes = data.notes;
            latest_sync_revision = data['latest-sync-revision'];
        }).error(function () {
            // console.log('fail');
        });
    };

    noteService.uploadChanges = function () {
        var dfd_complete = $q.defer();
        var note_changes = [];
        _.each(manifest.taintedNotes, function(guid) {
            var n = noteService.getNoteByGuid(guid);
            note_changes.push(n);
        });
        _.each(manifest.deletedNotes, function(guid) {
            var n = noteService.getNoteByGuid(guid);
            n.command = 'delete';
            note_changes.push(n);
        });

        if (note_changes.length > 0) {
            latest_sync_revision++;
            var req = {
                'latest-sync-revision': latest_sync_revision,
            };
            req['note-changes'] = note_changes;

            console.log(req);

            $http({
                method: 'PUT',
                url: '/api/1.0/' + loginService.username + '/notes?notes_as_html=true',
                headers: { 'AccessToken': loginService.accessToken },
                data: req
            }).success(function (data, status, headers, config) {
                console.log('successfully synced');
                noteService.fetchNotes(true);
                dfd_complete.resolve(note_changes);
            }).error(function () {
                dfd_complete.reject();
            });
        } else {
            console.log ('no changes, not syncing');
            dfd_complete.resolve();
        }
        return dfd_complete.promise;
    };

    noteService.deleteNote = function (note) {
        if (!_.contains(manifest.deletedNotes, note)) {
            manifest.deletedNotes.push(note.guid);
        }
    };

    noteService.newNote = function (initial_note) {
        var proto = {};
        proto.title = 'New note';
        proto['note-content'] = 'Enter your note.';
        proto['create-date'] = new Date().toISOString();
        proto.guid = guid();
        proto.tags = [];

        var note = $.extend(proto, initial_note);
        notes.push(note);
        return note;
    };

    noteService.markAsTainted = function (note) {
        var now = new Date().toISOString();
        note['last-change-date'] = now;
        note['last-metadata-change-date'] = now;

        if (!_.contains(manifest.taintedNotes, note.guid)) {
            console.log('marking note ' + note.guid + ' as tainted');
            manifest.taintedNotes.push(note.guid);
        }
    };

    return noteService;
});
