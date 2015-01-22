function MainCtrl($scope, $location) {

    $scope.checkLocation = function() {
        if (!$location.path().startsWith('/login')) {
            $scope.hideAdminNav = false;
            $scope.dontAskForPassword = false;
        } else {
            $scope.hideAdminNav = true;
            $scope.dontAskForPassword = true;
        }
    };
    $scope.checkLocation();
}

MainCtrl.$inject = [ '$scope', '$location' ];