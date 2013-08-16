function NoteCtrl($scope, $location, $routeParams, noteService) {

    $scope.notebooks = {};
    $scope.selectedNote = null;

    $scope.d = noteService.fetchNotes();
    $scope.d.then(function() {
        $scope.notebooks = noteService.notebooks; 
        $scope.selectedNote = noteService.getNoteByGuid($routeParams.guid);
    });

    if ($routeParams.guid) 
        $scope.selectedNote = noteService.getNoteByGuid($routeParams.guid);


    $scope.saveNote = function() {
        noteService.saveNote($scope.selectedNote);
    };

    $scope.selectNote = function(note) {
        var guid = note.guid;
        $location.path('/notes/' + guid);
        //$("#txtarea").wysihtml5();
    };

    $scope.sync = function () {
        //noteService.uploadChanges();
    };

    $scope.deleteNote = function () {
        noteService.deleteNote($scope.selectedNote);
        $scope.notebooks = noteService.notebooks;
    };

    $scope.newNote = function () {
        var note = noteService.newNote();
        $scope.notebooks = noteService.notebooks;
        $scope.selectedNote = note;
    };
}