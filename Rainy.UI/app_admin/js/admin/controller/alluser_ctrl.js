angular.module('adminApp').controller('AllUserCtrl', [
    '$scope',
    '$rootScope',
    'backendService',

    function(
        $scope,
        $rootScope,
        backendService
    ) {
        $scope.currently_edited_user = null;
        $scope.new_user = {};

        /*$scope.sendMail = true;
        $scope.sendMailDisabled = true;
        $scope.$watch('new_user.Password', function() {
            if ($scope.new_user.Password === undefined) {
                $scope.sendMail = true;
                $scope.sendMailDisabled = true;
            } else {
                $scope.sendMailDisabled = false;
            }
        });*/

        $scope.$watch(
            function() { return backendService.isAuthenticated; },
            function(newVal, oldVal) {
                if (newVal === true) {
                    $scope.reload_user_list(); 
                }
            }
        );
        
        $scope.reload_user_list = function() {
            backendService.ajax('api/admin/alluser/').success(function(data) {
                $scope.alluser = data;
            });
        };

        $scope.start_edit = function(user) {
            $scope.currently_edited_user = jQuery.extend(true, {}, user);
            $scope.currently_edited_user.Password = '';
        };
        $scope.stop_edit = function() {
            $scope.currently_edited_user = null;
        };

        $scope.save_user = function(is_new) {
            var ajax_req;
            $scope.new_user.IsActivated = true;
            $scope.new_user.IsVerified = true;
            if(is_new === true) {
                ajax_req = backendService.ajax('api/admin/user/', {
                    data: JSON.stringify($scope.new_user),
                    method:'POST',
                    headers: {
                        'Content-Type': 'application/json; charset=utf-8'
                    }
                });
            } else {
                // update user is done via PUT request
                ajax_req = backendService.ajax('api/admin/user/', {
                    data: JSON.stringify($scope.currently_edited_user),
                    method:'PUT',
                    headers: {
                        'Content-Type': 'application/json; charset=utf-8'
                    }
                });
            }
            ajax_req.finally(function() {
                if (is_new === true) {
                    $scope.new_user = {};
                } else {
                    $scope.stop_edit();
                }
                $scope.reload_user_list();
                $('#inputUsername').focus();
            });
        };

        $scope.delete_user = function(user, $event) {
            $event.stopPropagation();
            /* global confirm: true */
            if(!confirm('Really delete user \'' + user.Username + '\' ?')) {
                return;
            }
            backendService.ajax('api/admin/user/' + user.Username, {
                method :'DELETE',
                data: JSON.stringify(user),
                headers: {
                    'Content-Type': 'application/json; charset=utf-8',
                }
            }).success(function() {
                $scope.reload_user_list();
            });
        };
    }
]);
