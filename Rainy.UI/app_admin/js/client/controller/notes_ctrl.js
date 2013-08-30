function NoteCtrl($scope, $location, $routeParams, $q, $rootScope, noteService) {

    $scope.notebooks = {};
    $scope.notes = [];
    $scope.noteService = noteService;

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
        console.log('DO A SYNC!');
        noteService.uploadChanges();
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
