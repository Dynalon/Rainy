// Declare app level module which depends on filters, and services
var app = angular.module('clientApp', [
    'clientApp.filters',
    'clientApp.services',
    'clientApp.directives',
    'ngRoute'
])
.config(['$routeProvider',
    function($routeProvider) {
        // login page
        $routeProvider.when('/login', {
            templateUrl: 'login.html',
            controller: 'LoginCtrl'
        });

        $routeProvider.when('/notes/:guid?', {
            templateUrl: 'notes.html',
            controller: 'NoteCtrl'
        });
        $routeProvider.when('/logout', {
            template: '<div ng-controller="LogoutCtrl"></div>',
            controller: 'LogoutCtrl'
        });
        $routeProvider.when('/settings', {
            templateUrl: 'settings.html',
            controller: 'SettingsCtrl'
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
.config(['$locationProvider',
    function($locationProvider) {
        $locationProvider.html5Mode(false);
    }
]);

// register the interceptor as a service
app.factory('loginInterceptor', function($q, $location) {
    return {
        responseError: function(response) {
            // do something on error
            if (window.localStorage)
                window.localStorage.removeItem('accessToken');
            $location.path('/login');
            return $q.reject(response);
        }
    };
});
app.config(['$httpProvider', function($httpProvider) {
    $httpProvider.interceptors.push('loginInterceptor');
}]);


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
    .directive('uniqueUsername', ['$http', function ($http) {
        return {
            require:'ngModel',
            restrict: 'A',
            link:function (scope, el, attrs, ctrl) {

                var check_username_avail = _.debounce(function (username) {
                    $http.get('/api/user/signup/check_username/' + username)
                    .success(function (data) {
                        console.log(data);
                        if (data.Available === true) {
                            ctrl.$setValidity('username_avail', true);
                            //scope.username = data.Username;
                        }
                        else
                            ctrl.$setValidity('username_avail', false);
                    });
                }, 800);

                // push to the end of all other validity parsers
                ctrl.$parsers.push(function (viewValue) {
                    if (viewValue) {
                        check_username_avail(viewValue);
                        return viewValue;
                    }
                });
            }
        };
    }])
;

if (typeof String.prototype.startsWith !== 'function') {
    String.prototype.startsWith = function(str) {
        return this.slice(0, str.length) === str;
    };
}
