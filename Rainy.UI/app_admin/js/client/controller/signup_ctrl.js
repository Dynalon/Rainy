function SignupCtrl($scope, $location) {
    $scope.username = '';
    $scope.usernameOk = true;
    $scope.password1 = '';
    $scope.password2 = '';
    $scope.email = '';
    $scope.toc = false;

    $scope.$watch(combinedPassword, function (newval, oldval) {
        console.log(newval);
        if (newval === oldval) return;
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
        if ($scope.password1 !== $scope.password2)
            $scope.formSignup.$setValidity('passwdmatch', false);
        else
            $scope.formSignup.$setValidity('passwdmatch', true);
    }
}
