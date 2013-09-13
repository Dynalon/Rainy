function NoteCtrl($scope,$location, $routeParams, $timeout, $q, $rootScope, noteService, loginService, notyService) {

    $scope.notebooks = {};
    $scope.notes = [];
    $scope.noteService = noteService;
    $scope.username = loginService.username;
    $scope.enableSyncButton = false;


    // deep watching, will get triggered if a note content's changes, too
    $scope.$watch('noteService.notes', function (newval, oldval) {
        if (newval && newval.length === 0) return;

        if (oldval && oldval.length === 0 && newval && newval.length > 0) {
            // first time the notes become ready
        }

        $scope.notebooks = noteService.notebooks;
        $scope.notes = newval;

        loadNote();

    }, true);


    var initialAutosyncSeconds = 300;
    function startAutosyncTimer () {

        $timeout.cancel($rootScope.timer_dfd);
        $rootScope.autosyncSeconds = initialAutosyncSeconds;
        $scope.enableSyncButton = true;
        $rootScope.timer_dfd = $timeout(function autosync(){
            if ($rootScope.autosyncSeconds % 10 === 0)
                console.log('next sync in: ' + $rootScope.autosyncSeconds + ' seconds');
            if ($rootScope.autosyncSeconds <= 0) {
                $scope.sync();
                return;
            }
            else {
                $rootScope.autosyncSeconds--;
                $rootScope.timer_dfd = $timeout(autosync, 1000);
            }
        }, 1000);

    }

    function stopAutosyncTimer () {
        $rootScope.autosyncSeconds = initialAutosyncSeconds;
        $timeout.cancel($rootScope.timer_dfd);
        $scope.enableSyncButton = false;
    }

    function setSyncButtonTooltip () {
        // we need to recreate the tooltip every mouseover due to bootstrap internals
        $('#sync_btn').mouseenter(function() {
            var caption = 'Next autosync in ' + $rootScope.autosyncSeconds + ' seconds or press to perform manual sync';
            $('#sync_btn').data('tooltip', false);
            if ($scope.enableSyncButton) {
                $('#sync_btn').tooltip({ title: caption });
                $('#sync_btn').tooltip('show');
            }
        });
    }
    setSyncButtonTooltip ();

    function setWindowCloseMessage () {
        window.onbeforeunload = function () {
            if ($scope.enableSyncButton) {
                return 'There are unsaved notes, please push the synchronize button!';
            } else {
            }
        };
    }
    setWindowCloseMessage();


    $scope.$watch('noteService.needsSyncing', function (newval, oldval) {
        $scope.enableSyncButton = newval;
        if (newval === true && oldval === false) {
            startAutosyncTimer();
        }
    });

    function loadNote () {
        var guid = $routeParams.guid;

        if (!guid) return;
        var n = noteService.getNoteByGuid(guid);
        if (!n) return;
        if ($scope.selectedNote && n.guid === $scope.selectedNote.guid) return;
        $scope.selectedNote = n;
        $scope.setWysiText(n['note-content']);
    }

    $scope.onNoteChange = function (note) {
        noteService.markAsTainted(note);
    };

    $scope.selectNote = function (note) {
        var guid = note.guid;
        $location.path('/notes/' + guid);
    };

    $scope.sync = function () {
        if ($scope.enableSyncButton === false)
            return;

        $('#sync_btn').tooltip('hide');
        stopAutosyncTimer();
        $scope.flushWysi();
        // HACK we give 100ms before we sync to wait for the editor to flush
        $timeout(function () {
            noteService.uploadChanges().then(function (note_changes) {
                notyService.success('Successfully synced ' + note_changes.length + ' notes.', 2000);
            },function () {
                notyService.error('Error occured during syncing!');
            });

        }, 100);
    };

    $scope.deleteNote = function () {
        noteService.deleteNote($scope.selectedNote);
        $location.path('/notes/');
    };

    $scope.newNote = function () {
        var note = noteService.newNote();
        noteService.markAsTainted(note);
        $scope.selectNote(note);
    };
}
