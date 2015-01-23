angular.module('clientApp').controller('LogoutCtrl', [
    '$location',
    'loginService',
    function (
        $location,
        loginService
    ) {
        loginService.logout();
        $location.path('/login/');
    }
]);
