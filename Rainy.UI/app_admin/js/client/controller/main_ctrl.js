function MainCtrl ($scope, clientService) {
    $scope.isLoggedIn = clientService.userIsLoggedIn();

    $scope.$on('loginStatus', function(ev, isLoggedIn) {
        $scope.isLoggedIn = isLoggedIn;
    });
}