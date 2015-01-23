angular.module('clientApp').controller('MainCtrl', [
    '$scope',
    'loginService',
    function (
        $scope,
        loginService
    ) {
        $scope.isLoggedIn = loginService.userIsLoggedIn();

        $scope.$on('loginStatus', function(ev, isLoggedIn) {
            $scope.isLoggedIn = isLoggedIn;
        });
    }
]);