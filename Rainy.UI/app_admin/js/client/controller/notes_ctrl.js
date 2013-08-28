function NoteCtrl($scope, $location, $routeParams, $q, noteService) {

    $scope.notebooks = {};
    $scope.notes = [];
    $scope.selectedNote = null;

    $scope.noteContent = null;

    $scope.noteService = noteService;

    // deep watching, will get triggered if a note content's changes, too
    $scope.$watch('noteService.notes', function (newval, oldval) {

        if (newval && newval.length === 0) return;

        loadNote();

        if (oldval && oldval.length === 0 && newval && newval.length > 0) {
            // first time the notes become ready
        }

        $scope.notebooks = noteService.notebooks;
        $scope.notes = newval;


    }, true);

    $scope.$watch('noteContentValue', function (newval, oldval) {
        if (newval && newval === oldval) return;
        $scope.updateContent ();
    });

    function loadNote () {
        var guid = $routeParams.guid;

        if (!guid) return;
        var n = noteService.getNoteByGuid(guid);
        if (!n) return;
        if ($scope.selectedNote && n.guid === $scope.selectedNote.guid) return;

        $scope.selectedNote = n;
        //if ($scope.selectedNote === null || guid !== $scope.selectedNote.guid) {
        wysi_editor.setValue(n['note-content']);

        var dereg_watcher = $scope.$watch('selectedNote["note-content"]', function (newval, oldval) {
            checkIfTainted (newval, oldval, dereg_watcher);
        });
    }

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

    $scope.selectNote = function (note) {
        var guid = note.guid;
        $location.path('/notes/' + guid);
    };

    $scope.updateContent = function () {
        console.log('updateContent');
        if ($scope && $scope.selectedNote && $scope.selectedNote['note-content']) {
            $scope.selectedNote['note-content'] = $('#txtarea').val();
            if(!$scope.$$phase) {
                $scope.$digest();
            }
        }
    };

    $scope.sync = function () {
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
