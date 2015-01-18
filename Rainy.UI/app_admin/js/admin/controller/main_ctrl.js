function MainCtrl($scope, $routeParams, $route, $location) {

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

    // bug in angular prevents this from firing when the back button is used
    // (fixed in 1.1.5) - see https://github.com/angular/angular.js/pull/2206
    $scope.$on('$locationChangeStart', function(ev, oldloc, newloc) {
        $scope.checkLocation();
    });
}

MainCtrl.$inject = [ '$scope', '$routeParams', '$route', '$location' ];