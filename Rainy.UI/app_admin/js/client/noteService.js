app.factory('noteService', function($http, $q, $rootScope, loginService) {

    var notes = [];
    var noteService = {};

    Object.defineProperty(noteService, 'notebooks', {
        get: function () {
            return buildNotebooks(notes);
        }
    });

    var latest_sync_revision = 0;

    var manifest = {
        taintedNotes: [],
        deletedNotes: [],
    };

    $rootScope.$on('loginStatus', function(ev, isLoggedIn) {
        // TODO is this needed at all?
        if (!isLoggedIn) {
            //noteService.notes = [];
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
        
        var unassigned_notes = notesByNotebook(notes);

        if (unassigned_notes.length > 0) {
            notebooks.All = _.filter(unassigned_notes, function(note) {
                return !_.contains(manifest.deletedNotes, note);
            });
        }

        _.each(notes, function (note) {
            var nb = getNotebookFromNote (note);
            if (nb)
                notebook_names.push(nb);
        });
        notebook_names = _.uniq(notebook_names);

        _.each(notebook_names, function(name) {
            notebooks[name] = notesByNotebook(notes, name);
        });
        return notebooks;
    }

    function guid () {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            var r = Math.random()*16|0, v = c === 'x' ? r : (r&0x3|0x8);
            return v.toString(16);
        });
    }

    noteService.getNoteByGuid = function (guid) {
        return _.findWhere(notes, {guid: guid});
    };

    noteService.fetchNotes = function() {
        var defered = $q.defer();
        $http({
            method: 'GET',
            url: '/api/1.0/' + loginService.username + '/notes?include_notes=true',
            headers: { 'AccessToken': loginService.accessToken }
        }).success(function (data, status, headers, config) {
            notes = data.notes;
            defered.resolve();
        }).error(function () {
            // console.log('fail');
            defered.reject();
        });
        return defered.promise;
    };

    noteService.uploadChanges = function () {
        var note_changes = [];
        _.each(manifest.taintedNotes, function(note) {
            note_changes.push(note); 
        });
        _.each(manifest.deletedNotes, function(note) {
            note.command = 'delete';
            note_changes.push(note);
        });

        if (note_changes.length > 0) {
            latest_sync_revision++;
            var req = {
                'latest-sync-revision': latest_sync_revision,
            };
            req['note-changes'] = note_changes;

            $http({
                method: 'PUT',
                url: '/api/1.0/' + loginService.username + '/notes',
                headers: { 'AccessToken': loginService.accessToken },
                data: req
            }).success(function (data, status, headers, config) {
                console.log('successfully synced');
            });
        } else {
            console.log ('no changes, not syncing');
        }
    };

    noteService.deleteNote = function (note) {
        if (!_.contains(manifest.deletedNotes, note)) {
            manifest.deletedNotes.push(note);
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

    return noteService;
});