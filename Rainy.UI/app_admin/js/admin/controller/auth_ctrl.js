angular.module('adminApp').controller('AuthCtrl', [
    '$scope',
    '$rootScope',
    '$route',
    '$location',
    'backendService',

    function(
        $scope,
        $rootScope,
        $route,
        $location,
        backendService
    ) {

        $scope.adminPassword = '';

        // TODO move this into a custom route
        var url_pw = ($location.search()).password;
        if (url_pw !== undefined && url_pw.length > 0) {
            // new admin pw, update teh cookie
            backendService.adminPassword = url_pw;
        } else if (!$location.path().startsWith('/login')) {
            $rootScope.showAdminLogin = true;
//            $('#loginModal').modal();
//            $('#loginModal').find(':password').focus();
        }


        $scope.doLogin = function() {
            backendService.adminPassword = $scope.adminPassword;
            backendService.ajax('api/admin/status/')
            .success (function() {
 //               $('#loginModal').modal('hide');
                $rootScope.showAdminLogin = false;
                $rootScope.$digest();
                $scope.$apply();
            }).fail(function () {
                backendService.adminPassword = $scope.adminPassword = '';
 //               $('#loginModal').find(':password').focus();
                $rootScope.showAdminLogin = true;
                $scope.$apply();
                $rootScope.$digest();
            });
            $route.reload();
        };
    }
]);