angular.module('clientApp').controller('LoginCtrl', [
    '$scope',
    '$location',
    'loginService',
    'notyService',
    'configService',
    function (
        $scope,
        $location,
        loginService,
        notyService,
        configService
    ) {

        $scope.username = '';
        $scope.password = '';
        $scope.rememberMe = false;
        $scope.isTestServer = $location.host() === 'testserver.notesync.org';

        $scope.serverConfig = configService.serverConfig;

        if (loginService.userIsLoggedIn()) {
            $location.path('/notes/');
        }

        var useStorage = window.localStorage && window.sessionStorage;
        if (useStorage) {
            $scope.username = window.sessionStorage.getItem('username');

            $scope.$watch('username', function (newval, oldval) {
                if (!newval)
                    window.sessionStorage.removeItem('username');
                else
                    window.sessionStorage.setItem('username', newval);
            });


            $scope.rememberMe = window.sessionStorage.getItem('rememberMe') === 'true';
            $scope.$watch('rememberMe', function (newval, oldval) {
                window.sessionStorage.setItem('rememberMe', newval);
            });
        }

        if (!$scope.username || $scope.username.length === 0)
            $('#inputUsername').focus();
        else
            $('#inputPassword').focus();

        $scope.doLogin = function () {
            var remember = $scope.rememberMe;

            loginService.login($scope.username, $scope.password, remember)
            .then(function () {
                $location.path('/notes/');
            }, function (error_status) {
                console.log(error_status);
                if (error_status === 412)
                    notyService.error('Login failed. User ' + $scope.username + ' requires activation by an admin (moderation is enabled)');
                else
                    notyService.error('Login failed. Check username and password.');
            });
        };
    }
]);