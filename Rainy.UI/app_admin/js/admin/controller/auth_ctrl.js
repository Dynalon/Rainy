/*global admin_pw:true*/
var admin_pw='';
function AuthCtrl($scope, $route, $location, adminPassword) {

    var url_pw = ($location.search()).password;
    if (url_pw !== undefined && url_pw.length > 0) {
        // new admin pw, update teh cookie
        admin_pw = url_pw;
    } else if (!$location.path().startsWith('/login')) {
        $('#loginModal').modal();
        $('#loginModal').find(':password').focus();
    }

    $scope.doLogin = function() {
        // test request to the server
        // check if pw was correct by
        // doing dummy request
        admin_pw = $scope.adminPassword;

        $scope.backend.ajax('api/admin/status/')
        .success (function() {
            $('#loginModal').modal('hide');
            admin_pw = $scope.adminPassword;
            adminPassword = $scope.adminPassword;
        }).fail(function () {
            $scope.adminPassword='';
            $('#loginModal').find(':password').focus();
            $scope.$apply();
        });
        $route.reload();
    };
}
AuthCtrl.$inject = [ '$scope','$route', '$location', 'adminPassword' ];
