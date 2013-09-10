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


function AllUserCtrl($scope, $route) {
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

    $scope.reload_user_list = function() {
        $scope.backend.ajax('api/admin/alluser/').success(function(data) {
            $scope.alluser = data;
            $scope.$apply();
        });
    };
    $scope.reload_user_list();

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
            ajax_req = $scope.backend.ajax('api/admin/user/', {
                data: JSON.stringify($scope.new_user),
                type:'POST',
                contentType:'application/json; charset=utf-8',
                dataType:'json'
            });
        } else {
            // update user is done via PUT request
            ajax_req = $scope.backend.ajax('api/admin/user/', {
                data: JSON.stringify($scope.currently_edited_user),
                type:'PUT',
                contentType:'application/json; charset=utf-8',
                dataType:'json'
            });
        }
        ajax_req.done(function() {
            if (is_new === true) {
                $scope.new_user = null;
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
        $scope.backend.ajax('api/admin/user/' + user.Username, {
            type:'DELETE',
            data: JSON.stringify(user),
            contentType:'application/json; charset=utf-8',
            dataType:'json'
        }).done(function() {
            $scope.reload_user_list();
        });
    };
}

/*global admin_pw:true*/
var admin_pw='';
function AuthCtrl($scope, $route, $location) {

    var url_pw = ($location.search()).password;
    if (url_pw !== undefined && url_pw.length > 0) {
        // new admin pw, update teh cookie
        admin_pw = url_pw;
    } else if (!$location.path().startsWith('/login')) {
        $('#loginModal').modal();
        $('#loginModal').find(':password').focus();
    }

    $scope.doLogin = function() {
        // test request to the server
        // check if pw was correct by
        // doing dummy request
        admin_pw = $scope.adminPassword;

        $scope.backend.ajax('api/admin/status/')
        .success (function() {
            $('#loginModal').modal('hide');
            admin_pw = $scope.adminPassword;
        }).fail(function () {
            $scope.adminPassword='';
            $('#loginModal').find(':password').focus();
            $scope.$apply();
        });
        $route.reload();
    };
}
AuthCtrl.$inject = [ '$scope','$route', '$location' ];


function LoginCtrl($scope, $rootScope, $http, notyService) {

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
                notyService.error('Login failed. Check username and password');
            });
    };
}
//LoginCtrl.$inject = [ '$scope','$http' ];

function MainCtrl($scope, $routeParams, $route, $location) {

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

    // bug in angular prevents this from firing when the back button is used
    // (fixed in 1.1.5) - see https://github.com/angular/angular.js/pull/2206
    $scope.$on('$locationChangeStart', function(ev, oldloc, newloc) {
        $scope.checkLocation();
    });
}

function StatusCtrl($scope, $http, $route) {
    $scope.serverStatus = {};

    $scope.getStatus = function () {
        $scope.backend.ajax('api/admin/status/')
        .success(function(data) {
            $scope.serverStatus = data;

            var today = new Date();
            var then = new Date(data.Uptime);
            var dt = today - then;

            $scope.upSinceDays = Math.round(dt / 86400000); // days
            $scope.upSinceHours = Math.round((dt % 86400000) / 3600000); // hours
            $scope.upSinceMinutes = Math.round(((dt % 86400000) % 3600000) / 60000); // minutes

            $scope.$apply();
        });
    }();

}
StatusCtrl.$inject = [ '$scope', '$http', '$route' ];

/*global $:false */
/*global angular:false */
angular.module('myApp.directives', [])
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

angular.module('myApp.filters', []).
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
angular.module('myApp.services', [])
    .value('version', '0.1');

