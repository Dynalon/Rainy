function SignupCtrl($scope, $location, $http) {
    $scope.username = '';
    $scope.password1 = '';
    $scope.password2 = '';
    $scope.email = '';
    $scope.toc = false;

    $scope.$watch(combinedPassword, function (newval, oldval) {
        if (newval === oldval)
            return;
        passwordMatch();
    });

    $scope.$watch('toc', function (newval) {
        if (newval === true)
            $scope.formSignup.$setValidity('toc', true);
        else
            $scope.formSignup.$setValidity('toc', false);
    });
    
    function combinedPassword () {
        return $scope.password1 + ' ' + $scope.password2;
    }

    function passwordMatch () {
        if ($scope.password1 === $scope.password2)
            $scope.formSignup.$setValidity('passwdmatch', true);
        else
            $scope.formSignup.$setValidity('passwdmatch', false);
    }
}
