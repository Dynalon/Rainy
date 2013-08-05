function MainCtrl($scope, $routeParams, $route, $location) {

    $scope.checkLocation = function() {
        if ($location.path().startsWith("/admin")) {
            console.log("_" + $location.path());
            $scope.hideAdminNav = false;
            $scope.dontAskForPassword = false;
        } else {
            $scope.hideAdminNav = true;
            $scope.dontAskForPassword = true;
        }
    };
    $scope.checkLocation();

    $scope.$on('$locationChangeStart', function(ev, oldloc, newloc) {
        console.log("_" + $location.path());
        $scope.checkLocation();
    });
}

if (typeof String.prototype.startsWith !== 'function') {
    String.prototype.startsWith = function(str) {
        return this.slice(0, str.length) === str;
    };
}
