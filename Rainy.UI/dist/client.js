// Declare app level module which depends on filters, and services
var app = angular.module('clientApp', [
    'clientApp.filters',
    'clientApp.services',
    'clientApp.directives',

    // anguar-strap.js
    '$strap.directives'
])
.config(['$routeProvider',
    function($routeProvider) {
        // login page
        $routeProvider.when('/login', {
            templateUrl: 'login.html',
            controller: 'LoginCtrl'
        });

        $routeProvider.when('/notes/:guid', {
            templateUrl: 'notes.html',
            controller: 'NoteCtrl'
        });
        $routeProvider.when('/logout', {
            template: '<div ng-controller="LogoutCtrl"></div>',
            controller: 'LogoutCtrl'
        });
        $routeProvider.when('/signup', {
            templateUrl: 'signup.html',
            controller: 'SignupCtrl'
        });

        $routeProvider.otherwise({
            redirectTo: '/login'
        });
    }
])
// disable the X-Requested-With header
.config(['$httpProvider', function($httpProvider) {
        delete $httpProvider.defaults.headers.common['X-Requested-With'];
    }
])
.config(['$locationProvider',
    function($locationProvider) {
        $locationProvider.html5Mode(false);
    }
]);

// register the interceptor as a service
app.factory('loginInterceptor', function($q, $location) {
    return function(promise) {
        return promise.then(function(response) {
            // do something on success
            return response;
        }, function(response) {
            // do something on error
            if (response.status === 401) {
                if (window.localStorage)
                    window.localStorage.removeItem('accessToken');
                $location.path('/login');
            }
            return $q.reject(response);
        });
    };
});
app.config(['$httpProvider', function($httpProvider) {
        $httpProvider.responseInterceptors.push('loginInterceptor');
    }
]);


// FILTERS
angular.module('clientApp.filters', [])
    .filter('interpolate', ['version', function(version) {
        return function(text) {
            return String(text).replace(/\%VERSION\%/mg, version);
        };
    }]);

// SERVICES
angular.module('clientApp.services', [])
    .value('version', '0.1');


// DIRECTIVES
angular.module('clientApp.directives', [])
    .directive('appVersion', ['version',
        function(version) {
            return function(scope, elm, attrs) {
                elm.text(version);
            };
        }
    ])
;

if (typeof String.prototype.startsWith !== 'function') {
    String.prototype.startsWith = function(str) {
        return this.slice(0, str.length) === str;
    };
}

function LoginCtrl($scope, $location, loginService, notyService, $rootScope) {

    $scope.username = '';
    $scope.password = '';
    $scope.rememberMe = false;

    $scope.allowSignup = true;
    $scope.allowRememberMe = true;

    if (loginService.userIsLoggedIn()) {
        $location.path('/notes/');
    }

    var useStorage = window.localStorage && window.sessionStorage;
    if (useStorage) {
        $scope.username = window.sessionStorage.getItem('username');

        $scope.$watch('username', function (newval, oldval) {
            if (!newval)
                window.sessionStorage.removeItem('username');
            else
                window.sessionStorage.setItem('username', newval);
        });


        $scope.rememberMe = window.sessionStorage.getItem('rememberMe') === 'true';
        $scope.$watch('rememberMe', function (newval, oldval) {
            window.sessionStorage.setItem('rememberMe', newval);
        });
    }

    if (!$scope.username || $scope.username.length === 0)
        $('#inputUsername').focus();
    else
        $('#inputPassword').focus();

    $scope.doLogin = function () {
        var remember = $scope.allowRememberMe && $scope.rememberMe;

        loginService.login($scope.username, $scope.password, remember)
        .then(function () {
            $location.path('/notes/');
        }, function (error) {
            notyService.error('Login failed. Check username and password.');
        });
    };
}
//LoginCtrl.$inject = [ '$scope','$http' ];

function LogoutCtrl($location, loginService) {
    
    loginService.logout();
    $location.path('/login/');
}

function MainCtrl ($scope, loginService) {
    $scope.isLoggedIn = loginService.userIsLoggedIn();

    $scope.$on('loginStatus', function(ev, isLoggedIn) {
        $scope.isLoggedIn = isLoggedIn;
    });
}
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

function SignupCtrl($scope, $location) {
    $scope.username = '';
    $scope.usernameOk = true;
    $scope.password1 = '';
    $scope.password2 = '';
    $scope.email = '';
    $scope.toc = false;

    $scope.$watch(combinedPassword, function (newval, oldval) {
        console.log(newval);
        if (newval === oldval) return;
        passwordMatch();
    });

    $scope.$watch('toc', function (newval) {
        if (newval === true)
            $scope.formSignup.$setValidity('toc', true);
        else
            $scope.formSignup.$setValidity('toc', false);
    });

    function combinedPassword () {
        return $scope.password1 + ' ' + $scope.password2;
    }

    function passwordMatch () {
        if ($scope.password1 !== $scope.password2)
            $scope.formSignup.$setValidity('passwdmatch', false);
        else
            $scope.formSignup.$setValidity('passwdmatch', true);
    }
}

app.factory('loginService', function($q, $http, $rootScope) {
    var loginService = {
        username: '',
        accessToken: ''
    };

    var useStorage = window.sessionStorage && window.localStorage;
    if (useStorage) {
        loginService.accessToken = window.localStorage.getItem('accessToken');
        loginService.username = window.localStorage.getItem('username');
    } 

    loginService.login = function (user, pass, remember) {
        var deferred = $q.defer();
        var expiry = 1440; // 1d

        if (remember)
            expiry = 14 * 1440; // 14d

        var credentials = {
            Username: user,
            Password: pass,
            Expiry: expiry
        };

        $http.post('/oauth/temporary_access_token', credentials)
        .success(function (data, status, headers, config) {
            loginService.accessToken = data.AccessToken;
            loginService.username = user;

            if (useStorage && remember) {
                window.localStorage.setItem('username', user);
                window.localStorage.setItem('accessToken', data.AccessToken);
            }
            $rootScope.$broadcast('loginStatus', true);
            deferred.resolve();
        })
        .error(function (data, status) {
            deferred.reject(status);
            $rootScope.$broadcast('loginStatus', false);
        });
        return deferred.promise;
    };

    loginService.logout = function () {
        loginService.accessToken = '';
        loginService.username = '';
        loginService.notes = [];

        if (useStorage) {
            window.localStorage.removeItem('accessToken');
        }
        $rootScope.$broadcast('loginStatus', false);
    };

    loginService.userIsLoggedIn = function () {
        // TODO check for expiry
        var logged = !(loginService.accessToken === '' ||
            loginService.accessToken === undefined);

        if (useStorage && logged) {
            var ret = window.localStorage.getItem('accessToken') && true;  
            return ret;
        }
        else return logged;
    };

    loginService.isLoggedIn = loginService.userIsLoggedIn();

    return loginService;
});

app.factory('noteService', function($http, $rootScope, loginService) {

    var noteService = {};
    var notes = [];

    var latest_sync_revision = 0;
    var manifest = {
        taintedNotes: [],
        deletedNotes: [],
    };

    Object.defineProperty(noteService, 'notebooks', {
        get: function () {
            return buildNotebooks(notes);
        }
    });

    Object.defineProperty(noteService, 'notes', {
        get: function () {
            return filterDeletedNotes(notes);
        }
    });

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

        // filter out notes marked as deleted & empty notebooks
        var filtered_nb = {};
        for (var nb in notebooks) {
            var filtered = filterDeletedNotes(notebooks[nb]);
            if (filtered.length > 0)
                filtered_nb[nb] =  filtered;
        }

        return filtered_nb;
    }

    function filterDeletedNotes(notes) {
        var filtered = _.filter(notes, function(note) {
            return !_.contains(manifest.deletedNotes, note.guid);
        });
        return filtered;
    }

    function guid () {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            var r = Math.random()*16|0, v = c === 'x' ? r : (r&0x3|0x8);
            return v.toString(16);
        });
    }

    noteService.getNoteByGuid = function (guid) {
        if (noteService.notes.length === 0)
            return null;
        return _.findWhere(notes, {guid: guid});
    };

    noteService.fetchNotes = function() {
        manifest.taintedNotes = [];
        manifest.deletedNotes = [];

        $http({
            method: 'GET',
            url: '/api/1.0/' + loginService.username + '/notes?include_notes=true',
            headers: { 'AccessToken': loginService.accessToken }
        }).success(function (data, status, headers, config) {
            notes = data.notes;
        }).error(function () {
            // console.log('fail');
        });
    };

    noteService.uploadChanges = function () {
        var note_changes = [];
        _.each(manifest.taintedNotes, function(guid) {
            var n = noteService.getNoteByGuid(guid);
            note_changes.push(n);
        });
        _.each(manifest.deletedNotes, function(guid) {
            var n = noteService.getNoteByGuid(guid);
            n.command = 'delete';
            note_changes.push(n);
        });

        if (note_changes.length > 0) {
            latest_sync_revision++;
            var req = {
                'latest-sync-revision': latest_sync_revision,
            };
            req['note-changes'] = note_changes;

            console.log(req);

            $http({
                method: 'PUT',
                url: '/api/1.0/' + loginService.username + '/notes',
                headers: { 'AccessToken': loginService.accessToken },
                data: req
            }).success(function (data, status, headers, config) {
                console.log('successfully synced');
                noteService.fetchNotes();
            });
        } else {
            console.log ('no changes, not syncing');
        }
    };

    noteService.deleteNote = function (note) {
        if (!_.contains(manifest.deletedNotes, note)) {
            manifest.deletedNotes.push(note.guid);
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

    noteService.markAsTainted = function (note) {
        if (!_.contains(manifest.taintedNotes, note.guid)) {
            console.log('marking note ' + note.guid + ' as tainted');
            manifest.taintedNotes.push(note.guid);
        }
    };

    noteService.fetchNotes();

    return noteService;
});

app.factory('notyService', function($rootScope) {
    var notyService = {};

    function showNoty (msg, type, timeout) {
        timeout = timeout || 5000;
        var n = noty({
            text: msg,
            layout: 'topCenter',
            timeout: 5000,
            type: 'error'
        });
    }

    $rootScope.$on('$routeChangeStart', function() {
        $.noty.clearQueue();
        $.noty.closeAll();
    });
    notyService.error = function (msg, timeout) {
        return showNoty(msg, 'error', timeout);
    };
    notyService.warn = function (msg, timeout) {
        return showNoty(msg, 'warn', timeout);
    };

    return notyService;
});