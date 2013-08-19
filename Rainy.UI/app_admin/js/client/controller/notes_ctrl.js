function NoteCtrl($scope, $location, $routeParams, noteService) {

    $scope.notebooks = {};
    $scope.notes = [];
    $scope.selectedNote = null;

    $scope.noteService = noteService;
    $scope.$watch('noteService.notes', function (newval, oldval) {
        $scope.notebooks = noteService.notebooks;
        $scope.notes = newval;

        var guid = $routeParams.guid;
        if (guid) {
            if ($scope.selectedNote === null || guid !== $scope.selectedNote.guid) {
                var n = noteService.getNoteByGuid($routeParams.guid);
                $scope.selectNote(n);
            }
        }

    }, true);

    function checkIfTainted (newval, oldval, dereg) {
        if (newval === oldval)
            return;
        // mark this note as tainted
        noteService.markAsTainted($scope.selectedNote);
        dereg();
    }

    $scope.saveNote = function () {
        noteService.saveNote($scope.selectedNote);
    };

    $scope.selectNote = function (note) {
        if (!!note) {
            $scope.selectedNote = note;

            var dereg_watcher = $scope.$watch('selectedNote["note-content"]', function (newval, oldval) {
                checkIfTainted (newval, oldval, dereg_watcher);
            });

            var guid = note.guid;
            $location.path('/notes/' + guid);
        } else
            $scope.selectedNote = null;
        //$("#txtarea").wysihtml5();
    };

    $scope.sync = function () {
        //noteService.debug();
        noteService.uploadChanges();
    };

    $scope.deleteNote = function () {
        noteService.deleteNote($scope.selectedNote);
        $location.path('/notes/');
    };

    $scope.newNote = function () {
        var note = noteService.newNote();
        $scope.selectNote(note);
    };
}
