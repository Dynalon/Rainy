function MainCtrl($scope, $routeParams, $route, $location) {

    if ($location.path() === "/login") {
        $scope.hideNav = true;
        $scope.dontAskForPassword = true;
    }
}
