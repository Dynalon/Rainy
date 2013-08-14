function LoginCtrl($scope, $location, clientService, notyService, $rootScope) {

    $scope.username = '';
    $scope.password = '';
    $scope.rememberMe = false;

    $scope.allowSignup = true;
    $scope.allowRememberMe = true;

    if (clientService.userIsLoggedIn()) {
        $location.path('/notes/');
    }

    var useStorage = window.localStorage && window.sessionStorage;
    if (useStorage) {
        $scope.username = window.sessionStorage.getItem('username');
        $scope.$watch('username', function (newval, oldval) {
            window.sessionStorage.setItem('username', newval);
        });


        $scope.rememberMe = window.sessionStorage.getItem('rememberMe') === 'true';
        $scope.$watch('rememberMe', function (newval, oldval) {
            window.sessionStorage.setItem('rememberMe', newval);
        });
    }

    if ($scope.username.length === 0)
        $('#inputUsername').focus();
    else
        $('#inputPassword').focus();

    $scope.doLogin = function () {
        var remember = $scope.allowRememberMe && $scope.rememberMe;

        clientService.login($scope.username, $scope.password, remember)
        .then(function () {
            $location.path('/notes/');
        }, function (error) {
            notyService.error('Login failed. Check username and password.');
        });
    };
}
//LoginCtrl.$inject = [ '$scope','$http' ];
