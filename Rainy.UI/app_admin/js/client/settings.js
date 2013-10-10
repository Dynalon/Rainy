function SettingsCtrl($scope, $location, loginService) {
    $scope.enableAutosync = false;

    $scope.username = loginService.username;
}
