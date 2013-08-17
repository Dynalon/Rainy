function ClientCtrl ($scope, $http, $q, clientService) {

    $scope.notes = clientService.notes;

    // TODO find a better way to watch on that service
    $scope.clientService = clientService;
    $scope.$watch('clientService.notes', function (oldval, newval) {
        $scope.notes = clientService.notes;
        console.log($scope.notes);
    });
    clientService.fetchNotes();

}

function NoteCtrl($scope, clientService) {

    // TODO find a better way to watch on that service
    $scope.clientService = clientService;
    $scope.$watch('clientService.notes', function (oldval, newval) {
        $scope.notes = clientService.notes;
    });
    $scope.selectedNote = null;

    $scope.saveNote = function() {
        console.log('attempting to save note');
        clientService.saveNote($scope.selectedNote);
    };

    $scope.selectNote = function(index) {
        $scope.selectedNote = $scope.notes[index];
        //$("#txtarea").wysihtml5();
    };

}
