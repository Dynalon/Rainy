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

.run(['$rootScope', function($rootScope) {
    $rootScope.section = 'overview';
    $rootScope.$on('$routeChangeSuccess', function(ev, next, current) {
        if (!next || !next.originalPath) return;
        if (next.originalPath.indexOf('overview') >= 0)
            $rootScope.section = 'overview';
        if (next.originalPath.indexOf('user') >= 0)
            $rootScope.section = 'user';
    });
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

angular.module('adminApp').controller('AllUserCtrl', [
    '$scope',
    '$rootScope',
    'backendService',

    function(
        $scope,
        $rootScope,
        backendService
    ) {
        $scope.currently_edited_user = null;
        $scope.new_user = {};

        /*$scope.sendMail = true;
        $scope.sendMailDisabled = true;
        $scope.$watch('new_user.Password', function() {
            if ($scope.new_user.Password === undefined) {
                $scope.sendMail = true;
                $scope.sendMailDisabled = true;
            } else {
                $scope.sendMailDisabled = false;
            }
        });*/

        $scope.$watch(
            function() { return backendService.isAuthenticated; },
            function(newVal, oldVal) {
                if (newVal === true) {
                    $scope.reload_user_list(); 
                }
            }
        );
        
        $scope.reload_user_list = function() {
            backendService.ajax('api/admin/alluser/').success(function(data) {
                $scope.alluser = data;
            });
        };

        $scope.start_edit = function(user) {
            $scope.currently_edited_user = jQuery.extend(true, {}, user);
            $scope.currently_edited_user.Password = '';
        };
        $scope.stop_edit = function() {
            $scope.currently_edited_user = null;
        };

        $scope.save_user = function(is_new) {
            var ajax_req;
            $scope.new_user.IsActivated = true;
            $scope.new_user.IsVerified = true;
            if(is_new === true) {
                ajax_req = backendService.ajax('api/admin/user/', {
                    data: JSON.stringify($scope.new_user),
                    method:'POST',
                    headers: {
                        'Content-Type': 'application/json; charset=utf-8'
                    }
                });
            } else {
                // update user is done via PUT request
                ajax_req = backendService.ajax('api/admin/user/', {
                    data: JSON.stringify($scope.currently_edited_user),
                    method:'PUT',
                    headers: {
                        'Content-Type': 'application/json; charset=utf-8'
                    }
                });
            }
            ajax_req.finally(function() {
                if (is_new === true) {
                    $scope.new_user = {};
                } else {
                    $scope.stop_edit();
                }
                $scope.reload_user_list();
                $('#inputUsername').focus();
            });
        };

        $scope.delete_user = function(user, $event) {
            $event.stopPropagation();
            /* global confirm: true */
            if(!confirm('Really delete user \'' + user.Username + '\' ?')) {
                return;
            }
            backendService.ajax('api/admin/user/' + user.Username, {
                method :'DELETE',
                data: JSON.stringify(user),
                headers: {
                    'Content-Type': 'application/json; charset=utf-8',
                }
            }).success(function() {
                $scope.reload_user_list();
            });
        };
    }
]);

angular.module('adminApp').controller('AuthCtrl', [
    '$scope',
    '$rootScope',
    '$route',
    '$location',
    'backendService',

    function(
        $scope,
        $rootScope,
        $route,
        $location,
        backendService
    ) {

        $scope.adminPassword = '';

        // TODO move this into a custom route
        var url_pw = ($location.search()).password;
        if (url_pw !== undefined && url_pw.length > 0) {
            // new admin pw, update teh cookie
            backendService.adminPassword = url_pw;
        } else if (!$location.path().startsWith('/login')) {
        }

        $scope.doLogin = function() {
            backendService.adminPassword = $scope.adminPassword;
            backendService.ajax('api/admin/status/');
        };
    }
]);
angular.module('adminApp').controller('LoginCtrl', [
    '$scope',
    '$http',
    'notyService',
    function (
        $scope,
        $http,
        notyService
    ) {

        $scope.getUrlVars = function() {
            var vars = [], hash;
            var hashes = window.location.href.slice(window.location.href.indexOf('?') + 1).split('&');
            for(var i = 0; i < hashes.length; i++)
            {
                hash = hashes[i].split('=');
                vars.push(hash[0]);
                vars[hash[0]] = hash[1];
            }
            return vars;
        };

        var url_vars = $scope.getUrlVars();

        $scope.authData = { Username: '', Password: '', RequestToken: '' };
        $scope.authData.RequestToken = url_vars['oauth_token'];

        $scope.doLogin = function () {
            $http.post('/oauth/authenticate', $scope.authData)
                .success(function (data, status, headers, config) {
                    window.document.location = data.RedirectUrl;
                })
                .error(function (data, status, headers, config) {
                    if (status === 412)
                        notyService.error('Login failed. User ' + $scope.authData.Username + ' requires activation by an admin (Moderation is enabled)');
                    else
                        notyService.error('Login failed. Check username and password');
                });
        };
    }
]);

angular.module('adminApp').controller('MainCtrl', [
    '$scope',
    '$location',
    function (
        $scope,
        $location
    ) {
        $scope.checkLocation = function() {
            if (!$location.path().startsWith('/login')) {
                $scope.hideAdminNav = false;
                $scope.dontAskForPassword = false;
            } else {
                $scope.hideAdminNav = true;
                $scope.dontAskForPassword = true;
            }
        };
        $scope.checkLocation();
    }
]);

angular.module('adminApp').controller('StatusCtrl', [
    '$scope',
    '$rootScope',
    'backendService',
    function(
        $scope,
        $rootScope,
        backendService
    ) {
        $scope.serverStatus = {};

        $scope.getStatus = function () {
            backendService.ajax('api/admin/status/')
            .success(function(data) {
                $scope.serverStatus = data;

                var today = new Date();
                var then = new Date(data.Uptime);
                var dt = today - then;

                $scope.upSinceDays = Math.round(dt / 86400000); // days
                $scope.upSinceHours = Math.round((dt % 86400000) / 3600000); // hours
                $scope.upSinceMinutes = Math.round(((dt % 86400000) % 3600000) / 60000); // minutes

            });
        };

        $rootScope.$watch(
            function() { return backendService.isAuthenticated; },
            function(newValue, oldValue) {
                if (newValue === true) {
                    $scope.getStatus();
                }
            }
        );
    }
]);
/*global $:false */
/*global angular:false */
angular.module('adminApp.directives', [])
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

/*global $:false */
/*global angular:false */

angular.module('adminApp.filters', []).
    filter('interpolate', ['version', function(version) {
    return function(text) {
        return String(text).replace(/\%VERSION\%/mg, version);
    };
}]);

/*global $:false */
/*global angular:false */
/*global myModule:true*/

/* Services */


// Demonstrate how to register services
// In this case it is a simple value service.
angular.module('adminApp.services', [])
    .value('version', '0.1');

