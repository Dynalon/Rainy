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
