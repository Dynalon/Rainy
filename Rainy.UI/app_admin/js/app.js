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
        $routeProvider.when('/admin/user', {
            templateUrl: 'user.html',
            controller: AllUserCtrl
        });
        $routeProvider.when('/admin/overview', {
            templateUrl: 'overview.html',
            controller: StatusCtrl
        });
        $routeProvider.when('/login', {
            templateUrl: 'login.html',
            controller: LoginCtrl
        });
        $routeProvider.otherwise({
            redirectTo: '/admin/user'
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
.run(['$rootScope', '$modal', '$route', '$q', function($rootScope, $modal, $route, $q)  {
    var backend = {
        ajax: function(rel_url, options) {
            var backend_url = "/";

            if (options === undefined)
                options = {};

            var abs_url = backend_url + rel_url;
            options.beforeSend = function(request) {
                request.setRequestHeader("Authority", admin_pw);
            };
            var ret = $.ajax(abs_url, options);

            ret.fail(function(jqxhr, textStatus) {
                if (jqxhr.status == 401) {
                    $("#loginModal").modal();
                    $('#loginModal').find(":password").focus();
                }
            });
            return ret;
        }
    };
    $rootScope.backend = backend;
}]);
