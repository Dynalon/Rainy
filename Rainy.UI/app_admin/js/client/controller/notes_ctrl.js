function ClientCtrl ($scope, $http, $q, clientService) {

    $scope.notes = clientService.notes;

    // TODO find a better way to watch on that service
    $scope.clientService = clientService;
    $scope.$watch('clientService.notes', function (oldval, newval) {
        $scope.notes = clientService.notes;
    });
    clientService.fetchNotes();
}

function NoteCtrl($scope, clientService, $routeParams, $location) {

    $scope.notebooks = [ 'None' ];
    // TODO find a better way to watch on that service
    $scope.clientService = clientService;
    $scope.$watch('clientService.notes', function (oldval, newval) {
        $scope.notes = clientService.notes;
        $scope.selectedNote = _.findWhere($scope.notes, {guid: $routeParams.guid});
        $scope.notebooks = buildNotebooks($scope.notes);
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
                return nb == notebook_name;
            });
        } else {
            // return notes that don't have a notebook
            return _.filter(notes, function (note) {
                return getNotebookFromNote(note) === null;
            });
        }
    }

    $scope.saveNote = function() {
        console.log("attempting to save note");
        clientService.saveNote($scope.selectedNote);
    };

    $scope.selectNote = function(note) {
        var guid = note.guid;
        $location.path('/main/' + guid);
        //$("#txtarea").wysihtml5();
    };

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
        return notebooks;
    }
}