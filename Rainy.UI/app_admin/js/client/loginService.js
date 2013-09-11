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
            window.sessionStorage.removeItem('accessToken');
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
