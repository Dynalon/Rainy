// Declare app level module which depends on filters, and services
var app = angular.module('myApp', [
    'myApp.filters',
    'myApp.services',
    'myApp.directives',

    // anguar-strap.js
    '$strap.directives'
])
.config(['$routeProvider',
    function($routeProvider) {
        // admin interface
        $routeProvider.when('/user', {
            templateUrl: 'user.html',
            controller: 'AllUserCtrl'
        });
        $routeProvider.when('/overview', {
            templateUrl: 'overview.html',
            controller: 'StatusCtrl'
        });

        // login page for OAUTH
        $routeProvider.when('/login', {
            templateUrl: 'login.html',
            controller: 'LoginCtrl'
        });

        // default is the admin overview
        $routeProvider.otherwise({
            redirectTo: '/user'
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
])
.factory('notyService', function($rootScope) {
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
})

.run(['$rootScope', '$modal', '$route', '$q', function($rootScope, $modal, $route, $q)  {
    var backend = {
        ajax: function(rel_url, options) {
            var backend_url = '/';

            if (options === undefined)
                options = {};

            var abs_url = backend_url + rel_url;
            options.beforeSend = function(request) {
                request.setRequestHeader('Authority', admin_pw);
            };
            var ret = $.ajax(abs_url, options);

            ret.fail(function(jqxhr, textStatus) {
                if (jqxhr.status === 401) {
                    $('#loginModal').modal();
                    $('#loginModal').find(':password').focus();
                }
            });
            return ret;
        }
    };
    $rootScope.backend = backend;
}]);
