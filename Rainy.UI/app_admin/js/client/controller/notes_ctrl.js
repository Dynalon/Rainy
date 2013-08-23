function NoteCtrl($scope, $location, $routeParams, $q, noteService) {

    $scope.notebooks = {};
    $scope.notes = [];
    $scope.selectedNote = null;

    $scope.noteContent = null;

    $scope.noteService = noteService;
    $scope.$watch('noteService.notes', function (newval, oldval) {
        $scope.notebooks = noteService.notebooks;
        $scope.notes = newval;

        $scope.$watch('noteContentValue', function (newval, oldval) {
            if (newval === oldval) return;
            $scope.updateContent ();
        });

        var guid = $routeParams.guid;
        if (guid) {
            if ($scope.selectedNote === null || guid !== $scope.selectedNote.guid) {
                var n = noteService.getNoteByGuid($routeParams.guid);
                $scope.selectNote(n);
                }
        }


    }, true);

    function noteContentValue () {
        return $('#txtarea').val();
    }

    var once = false;
    var wysi_editor;

    function setupWysi () {
        if (once) return;
        once = true;

        if ($('#txtarea').is(':visible')) {
            $('#txtarea').wysihtml5({
                html: true,
                link: false,
                image: false,
                color: false,
                stylesheets: [],
                events: {
                    change: function() {
                        $scope.updateContent();
                    },
                    load: function () {
                        var editor = $('#txtarea').data('wysihtml5').editor;
                        var $doc = $(editor.composer.doc);
                        $doc.keydown(function (evt) {
                            $scope.updateContent();
                        });
                    }
                }
            });
            wysi_editor = $('#txtarea').data('wysihtml5').editor;
            // HACK we modify/remove the bootstrap-wysihtml5 elements from the bar
            $('[data-wysihtml5-command=insertOrderedList]').remove();
            $('[data-wysihtml5-command=Outdent]').remove();
            $('[data-wysihtml5-command=Indent]').remove();

            $('[data-wysihtml5-command=underline]').remove();
            $('[data-wysihtml5-command-value=h1]').text('Huge');
            $('[data-wysihtml5-command-value=h2]').text('Large');
            $('[data-wysihtml5-command-value=h3]').replaceWith('<a data-wysihtml5-command=​"formatBlock" data-wysihtml5-command-value=​"h4" href=​"javascript:​;​" unselectable=​"on">Small</a>​');
        }
    }

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
            wysi_editor.setValue(note['note-content']);

            var dereg_watcher = $scope.$watch('selectedNote["note-content"]', function (newval, oldval) {

                checkIfTainted (newval, oldval, dereg_watcher);
            });

            var guid = note.guid;
            $location.path('/notes/' + guid);
        } else
            $scope.selectedNote = null;
    };

    $scope.updateContent = function () {
        if ($scope && $scope.selectedNote && $scope.selectedNote['note-content']) {
            $scope.selectedNote['note-content'] = $('#txtarea').val();
            console.log('setting note-content value');
        }
        if(!$scope.$$phase) {
            $scope.$digest();
        }

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

    setupWysi();
}
