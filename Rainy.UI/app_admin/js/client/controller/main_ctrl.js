function MainCtrl ($scope, loginService) {
    $scope.isLoggedIn = loginService.userIsLoggedIn();

    $scope.$on('loginStatus', function(ev, isLoggedIn) {
        $scope.isLoggedIn = isLoggedIn;
    });
}