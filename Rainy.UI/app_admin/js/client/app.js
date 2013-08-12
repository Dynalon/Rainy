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
            controller: ClientCtrl
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