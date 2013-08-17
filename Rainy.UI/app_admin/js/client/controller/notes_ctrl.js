function NoteCtrl($scope, $location, $routeParams, noteService) {

    $scope.notebooks = {};
    $scope.notes = [];
    $scope.selectedNote = null;

    $scope.noteService = noteService;
    $scope.$watch('noteService.notes', function (newval, oldval) {
        $scope.notebooks = noteService.notebooks;
        $scope.notes = newval;

        if ($routeParams.guid) {
            var n = noteService.getNoteByGuid($routeParams.guid);
            $scope.selectNote(n);
        }

    }, true);


    $scope.saveNote = function () {
        noteService.saveNote($scope.selectedNote);
    };

    $scope.selectNote = function (note) {
        if (!!note) {
            $scope.selectedNote = note;
            var guid = note.guid;
            $location.path('/notes/' + guid);
        } else
            $scope.selectedNote = null;
        //$("#txtarea").wysihtml5();
    };

    $scope.sync = function () {
        //noteService.uploadChanges();
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
