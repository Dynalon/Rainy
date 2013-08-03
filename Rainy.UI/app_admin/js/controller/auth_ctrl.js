/*global admin_pw:true*/
function AuthCtrl($scope, $route, $location ) {
    var url_pw = ($location.search()).admin_pw;
    if (url_pw !== undefined && url_pw.length > 0) {
        // new admin pw, update teh cookie
        admin_pw = url_pw;
    } else {
        admin_pw = "admin";
/*        $('#loginModal').modal();
        $('#loginModal').find(":password").focus();
*/
    }


    $scope.doLogin = function() {
        // test request to the server
        // check if pw was correct by
        // doing dummy request
        admin_pw = $scope.adminPassword;

        backend.ajax('api/admin/status/')
        .success (function() {
            $('#loginModal').modal('hide');
        }).fail(function () {
            $scope.adminPassword="admin";
            admin_pw = $scope.adminPassword;
            $scope.$apply();
            //$('#loginModal').find(":password").focus();
        });
        $route.reload();
    };

}
AuthCtrl.$inject = [ '$scope','$route', '$location' ];
