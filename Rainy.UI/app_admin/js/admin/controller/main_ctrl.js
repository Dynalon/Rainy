function MainCtrl($scope, $routeParams, $route, $location, adminPassword) {

    $scope.checkLocation = function() {
        if (!$location.path().startsWith('/login')) {
            $scope.hideAdminNav = false;
            $scope.dontAskForPassword = false;
        } else {
            $scope.hideAdminNav = true;
            $scope.dontAskForPassword = true;
        }
    };
    $scope.adminPassword = adminPassword;
    $scope.checkLocation();
}

MainCtrl.$inject = [ '$scope', '$routeParams', '$route', '$location' ];