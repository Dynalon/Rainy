// Declare app level module which depends on filters, and services
var app = angular.module('adminApp', [
    'adminApp.filters',
    'adminApp.services',
    'adminApp.directives',
    'ngRoute'
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

.service('backendService', ['$rootScope', '$http', function($rootScope, $http) {
    var self = this;
    self.adminPassword = '';
    self.isAuthenticated = false;



    self.ajax = function(rel_url, options) {
        var backend_url = '/';

        options = options || {};
        options.url = backend_url + rel_url;
        options.headers = options.headers || {};

        var authHeader = { Authority: self.adminPassword };
        $.extend(options.headers, authHeader);

        var prm =  $http(options);

        prm.success(function(response) {
            self.isAuthenticated = true;
        });

        prm.error(function(jqxhr, textStatus) {
            if (jqxhr.status === 401 || jqxhr.status === 403) {
                self.isAuthenticated = false;
            }
        });

        return prm;
    };
}])

.run(['$rootScope', 'backendService', function($rootScope, backendService) {
    $rootScope.showLogin = true;
    $rootScope.$watch(
        function() {
            return backendService.isAuthenticated;
        }, function(newVal, oldVal) {
            $rootScope.showLogin = !newVal;
        }
    );
}])

;
