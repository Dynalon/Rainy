function ClientCtrl ($scope, $http, $q) {

    $scope.notes = [
        {
            "Title": "Note1",
            "Text": "This is a sample note"
        },
        {
            "Title": "Note2",
            "Text": "This is just another sample note"
        },
        {
            "Title": "Note3",
            "Text": "This is the third sample note in the notebook"
        }
    ];
    $scope.selectedNote = $scope.notes[1];

    $scope.selectNote = function(index) {
        console.log("foobar");
        $scope.selectedNote = $scope.notes[index];
    };

    $scope.getTemporaryAccessToken = function() {

        var deferred = $q.defer();

        var credentials = { Username: "johndoe", Password: "none" };
        var token = "";

        $http.post('/oauth/temporary_access_token', credentials)
        .success(function (data, status, headers, config) {
            token = data.AccessToken;
            deferred.resolve(token);
        });
        return deferred.promise;
    };

    $scope.doit = function() {
        $scope.getTemporaryAccessToken ().then(function(token) {
            $http({
                method: 'GET',
                url: '/api/1.0/johndoe/notes?include_notes=true',
                headers: { 'AccessToken': token }
            }).success(function (data, status, headers, config) {
                console.log(data);
                $scope.notes = data.notes;
            });
        });
    };
}
