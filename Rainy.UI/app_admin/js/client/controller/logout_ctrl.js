function LogoutCtrl($location, loginService) {
    
    loginService.logout();
    $location.path('/login/');
}
LogoutCtrl.$inject = [ '$location', 'loginService' ];