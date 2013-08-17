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

app.factory('noteService', function($http, $q, $rootScope, loginService) {

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
        return _.findWhere(noteService.notes, {guid: guid});
    };

    noteService.fetchNotes = function() {
        var defered = $q.defer();
        $http({
            method: 'GET',
            url: '/api/1.0/' + loginService.username + '/notes?include_notes=true',
            headers: { 'AccessToken': loginService.accessToken }
        }).success(function (data, status, headers, config) {
            notes = data.notes;
            defered.resolve();
        }).error(function () {
            // console.log('fail');
            defered.reject();
        });
        return defered.promise;
    };

    noteService.uploadChanges = function () {
        var note_changes = [];
        _.each(manifest.taintedNotes, function(note) {
            note_changes.push(note);
        });
        _.each(manifest.deletedNotes, function(note) {
            note.command = 'delete';
            note_changes.push(note);
        });

        if (note_changes.length > 0) {
            latest_sync_revision++;
            var req = {
                'latest-sync-revision': latest_sync_revision,
            };
            req['note-changes'] = note_changes;

            $http({
                method: 'PUT',
                url: '/api/1.0/' + loginService.username + '/notes',
                headers: { 'AccessToken': loginService.accessToken },
                data: req
            }).success(function (data, status, headers, config) {
                console.log('successfully synced');
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