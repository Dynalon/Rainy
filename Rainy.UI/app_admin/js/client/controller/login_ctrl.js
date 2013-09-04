function LoginCtrl($scope, $location, $rootScope,
                   loginService, notyService, configService) {

    $scope.username = '';
    $scope.password = '';
    $scope.rememberMe = false;

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
        }, function (error) {
            notyService.error('Login failed. Check username and password.');
        });
    };
}
//LoginCtrl.$inject = [ '$scope','$http' ];
