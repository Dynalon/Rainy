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
            notifiyLoginFailed();
        });
    };

    function notifiyLoginFailed () {
        var n = noty({
            text: 'Login failed. Check username and password',
            layout: 'topCenter',
            timeout: 5000,
            type: 'error' 
        });
    }
}
//LoginCtrl.$inject = [ '$scope','$http' ];
