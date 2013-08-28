app.factory('noteService', function($http, $rootScope, loginService) {

    var noteService = {};
    var notes = [];

    var latest_sync_revision = 0;
    var manifest = {
        taintedNotes: [],
        deletedNotes: [],
    };

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

    $rootScope.$on('loginStatus', function(ev, isLoggedIn) {
        // TODO is this needed at all?
        if (!isLoggedIn) {
            latest_sync_revision = 0;
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

        notebooks.All = notesByNotebook(notes);


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

    noteService.fetchNotes = function() {
        manifest.taintedNotes = [];
        manifest.deletedNotes = [];

        $http({
            method: 'GET',
            url: '/api/1.0/' + loginService.username + '/notes?include_notes=true&notes_as_html=true',
            headers: { 'AccessToken': loginService.accessToken }
        }).success(function (data, status, headers, config) {
            notes = data.notes;
            latest_sync_revision = data['latest-sync-revision'];
        }).error(function () {
            // console.log('fail');
        });
    };

    noteService.uploadChanges = function () {
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
                noteService.fetchNotes();
            });
        } else {
            console.log ('no changes, not syncing');
        }
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
        proto.guid = guid();
        proto.tags = [];

        var note = $.extend(proto, initial_note);

        notes.push(note);
        return note;
    };

    noteService.markAsTainted = function (note) {
        if (!_.contains(manifest.taintedNotes, note.guid)) {
            console.log('marking note ' + note.guid + ' as tainted');
            manifest.taintedNotes.push(note.guid);
        }
    };

    noteService.fetchNotes();

    return noteService;
});
