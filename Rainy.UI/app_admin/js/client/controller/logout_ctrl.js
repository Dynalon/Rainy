function LogoutCtrl($scope, clientService, $location) {
    
    clientService.logout();
    $location.path('/login/');
}
