function StatusCtrl($scope, $http, $route) {
    $scope.serverStatus = {};

    $scope.getStatus = function () {
        $scope.backend.ajax('api/admin/status/')
        .success(function(data) {
            $scope.serverStatus = data;

            var today = new Date();
            var then = new Date(data.Uptime);
            var dt = today - then;

            $scope.upSinceDays = Math.round(dt / 86400000); // days
            $scope.upSinceHours = Math.round((dt % 86400000) / 3600000); // hours
            $scope.upSinceMinutes = Math.round(((dt % 86400000) % 3600000) / 60000); // minutes

            $scope.$apply();
        });
    }();

}
StatusCtrl.$inject = [ '$scope', '$http', '$route' ];
