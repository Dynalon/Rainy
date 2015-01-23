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

.service('backendService', function($rootScope) {
    var self = this;
    self.adminPassword = '';

    self.ajax = function(rel_url, options) {
        var backend_url = '/';

        if (options === undefined)
            options = {};

        var abs_url = backend_url + rel_url;
        options.beforeSend = function(request) {
            request.setRequestHeader('Authority', self.adminPassword);
        };
        var ret = $.ajax(abs_url, options);

        ret.fail(function(jqxhr, textStatus) {
            if (jqxhr.status === 401) {
                $('#loginModal').modal();
                $('#loginModal').find(':password').focus();
                $rootScope.$digest();
            }
        });
        return ret;
    };
})

;
