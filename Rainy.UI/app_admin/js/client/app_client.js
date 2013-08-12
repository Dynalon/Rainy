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
            templateUrl: 'login_client.html',
            controller: LoginCtrl
        });

        // web client interface
        $routeProvider.when('/main', {
            templateUrl: 'client.html',
            controller: NoteCtrl 
        });

        // default is the admin overview
        $routeProvider.otherwise({
            redirectTo: '/main'
        });
    }
])
// disable the X-Requested-With header
.config(['$httpProvider', function($httpProvider) {
        delete $httpProvider.defaults.headers.common["X-Requested-With"];
    }
])
.config(['$locationProvider',
    function($locationProvider) {
        //      $locationProvider.html5Mode(true);
    }
])
;

// FILTERS
angular.module('clientApp.filters', []).
  filter('interpolate', ['version', function(version) {
    return function(text) {
      return String(text).replace(/\%VERSION\%/mg, version);
    };
  }]);

// SERVICES
angular.module('clientApp.services', [])
    .value('version', '0.1');

app.factory('clientService', function($q, $http) {
    var clientService = {
        notes: [],
        last_sync_revision: 0,
        accessToken: '',
        userDetails: {
            username: 'johndoe'
        } 
    };

    clientService.getTemporaryAccessToken = function() {
        var deferred = $q.defer();
        var credentials = { Username: "johndoe", Password: "none" };

        $http.post('/oauth/temporary_access_token', credentials)
        .success(function (data, status, headers, config) {
            clientService.accessToken = data.AccessToken;
            deferred.resolve(clientService.accessToken);
        });
        return deferred.promise;
    };

    clientService.fetchNotes = function() {
        clientService.getTemporaryAccessToken().then(function() {
            $http({
                method: 'GET',
                url: '/api/1.0/johndoe/notes?include_notes=true',
                headers: { 'AccessToken': clientService.accessToken }
            }).success(function (data, status, headers, config) {
                clientService.notes = data.notes;
            });
        });
    };

    clientService.saveNote = function(note) {

        clientService.latest_sync_revision++;
        var req = {
            "latest-sync-revision": clientService.latest_sync_revision,
        }
        req['note-changes'] = [ note ];

        $http({
            method: 'PUT',
            url: '/api/1.0/johndoe/notes',
            headers: { 'AccessToken': clientService.accessToken },
            data: req
        }).success(function (data, status, headers, config) {
            console.log('successfully saved note');
        });
    };

    return clientService;
});

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