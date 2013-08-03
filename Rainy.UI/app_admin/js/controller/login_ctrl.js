
function LoginCtrl($scope, $rootScope, $http) {

    $scope.getUrlVars = function() {
        var vars = [], hash;
        var hashes = window.location.href.slice(window.location.href.indexOf('?') + 1).split('&');
        for(var i = 0; i < hashes.length; i++)
        {
            hash = hashes[i].split('=');
            vars.push(hash[0]);
            vars[hash[0]] = hash[1];
        }
        return vars;
    };

    var url_vars = $scope.getUrlVars();

    $scope.authData = { Username: "", Password: "", RequestToken: "" };
    $scope.authData.RequestToken = url_vars['oauth_token'];

    $scope.doLogin = function () {
        $http.post('/oauth/authenticate', $scope.authData)
            .success(function (data, status, headers, config) {
                window.document.location = data.RedirectUrl;
            });
    };
}
//LoginCtrl.$inject = [ '$scope','$http' ];
