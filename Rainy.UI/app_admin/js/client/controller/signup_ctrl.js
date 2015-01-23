angular.module('clientApp').controller('SignupCtrl', [
    '$scope',
    '$location',
    '$http',
    '$timeout',
    'notyService',
    function (
        $scope,
        $location,
        $http,
        $timeout,
        notyService
    ) {
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

        $scope.signUp = function () {
            var new_user = {
                Username: $scope.username,
                Password: $scope.password1,
                EmailAddress: $scope.email
            };
            $http.post('/api/user/signup/new/', new_user).success(function (data) {
                window.sessionStorage.setItem('username', $scope.username);
                notyService.success('Signup successfull! You will be redirected to the login page...');
                $timeout(function () {
                    $location.path('#/login/');
                }, 2000);
            }).error(function (data, status, headers, config) {
                notyService.error('ERROR: ' + status);
            });
        };
    }
]);
