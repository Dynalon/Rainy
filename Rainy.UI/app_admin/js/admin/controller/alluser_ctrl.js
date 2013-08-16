
function AllUserCtrl($scope, $route) {
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

    $scope.reload_user_list = function() {
        $scope.backend.ajax('api/admin/alluser/').success(function(data) {
            $scope.alluser = data;
            $scope.$apply();
        });
    };
    $scope.reload_user_list();

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
            ajax_req = $scope.backend.ajax('api/admin/user/', {
                data: JSON.stringify($scope.new_user),
                type:'POST',
                contentType:'application/json; charset=utf-8',
                dataType:'json'
            });
        } else {
            // update user is done via PUT request
            ajax_req = $scope.backend.ajax('api/admin/user/', {
                data: JSON.stringify($scope.currently_edited_user),
                type:'PUT',
                contentType:'application/json; charset=utf-8',
                dataType:'json'
            });
        }
        ajax_req.done(function() {
            if (is_new === true) {
                $scope.new_user = null;
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
        $scope.backend.ajax('api/admin/user/' + user.Username, {
            type:'DELETE',
            data: JSON.stringify(user),
            contentType:'application/json; charset=utf-8',
            dataType:'json'
        }).done(function() {
            $scope.reload_user_list();
        });
    };
}
AllUserCtrl.$inject = [ '$scope','$route' ];
