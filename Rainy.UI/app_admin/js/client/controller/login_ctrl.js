function LoginCtrl($scope, clientService, $location) {

    $scope.username = '';
    $scope.password = '';

    $scope.doLogin = function () {
        clientService.getTemporaryAccessToken($scope.username, $scope.password)
        .then(function (token) {
            console.log('login ok, token is ' + token);
            clientService.accessToken = token;
            $location.path('/notes/');
        }, function (error) {
            console.log('auth failed: ' + error);
        });
    };
}
//LoginCtrl.$inject = [ '$scope','$http' ];
