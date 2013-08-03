
function AllUserCtrl($scope, $route) {

    $scope.reload_user_list = function() {
        $scope.backend.ajax('api/admin/alluser/').success(function(data) {
            $scope.alluser = data;
            console.log(data);
            $scope.$apply();
        });
    };
    $scope.reload_user_list();

    $scope.currently_edited_user = null;
    $scope.new_user = null;

    $scope.add_new_user = function() {
        save_user(new_user, true);
    };

    $scope.start_edit = function(user) {
        $scope.currently_edited_user = jQuery.extend(true, {}, user);
        $scope.currently_edited_user.Password = "";
    };
    $scope.stop_edit = function() {
        $scope.currently_edited_user = null;
    };

    $scope.save_user = function(user, is_new) {
        var ajax_req;
        if(is_new === true) {
            ajax_req = $scope.backend.ajax('api/admin/user/', {
               data: JSON.stringify($scope.new_user),
               type:"POST",
               contentType:"application/json; charset=utf-8",
               dataType:"json"
            });
        } else {
            // update user is done via PUT request
            ajax_req = $scope.backend.ajax('api/admin/user/', {
               data: JSON.stringify($scope.currently_edited_user),
               type:"PUT",
               contentType:"application/json; charset=utf-8",
               dataType:"json"
            });
        }
        ajax_req.done(function() {
            if (is_new === true) {
                $scope.new_user = null;
            } else {
                $scope.stop_edit();
            }
            $scope.reload_user_list();
       });
    };

    $scope.delete_user = function(user, $event) {
        $event.stopPropagation();
        if(!confirm("Really delete user \"" + user.Username + "\" ?")) {
            return;
        }
        $scope.backend.ajax('api/admin/user/' + user.Username, {
            type:"DELETE",
            data: JSON.stringify(user),
            contentType:"application/json; charset=utf-8",
            dataType:"json"
        }).done(function() {
            $scope.reload_user_list();
        });
    };
}
AllUserCtrl.$inject = [ '$scope','$route' ];
