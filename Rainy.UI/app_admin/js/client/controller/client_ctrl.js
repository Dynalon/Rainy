function ClientCtrl ($scope, $http, $q) {

    $scope.notes = [];
    $scope.selectedNote = null;
    $scope.accessToken = '';

    $scope.selectNote = function(index) {
        $scope.selectedNote = $scope.notes[index];
    	//$("#txtarea").wysihtml5();
    };

    $scope.getTemporaryAccessToken = function() {

        var deferred = $q.defer();

        var credentials = { Username: "johndoe", Password: "none" };

        $http.post('/oauth/temporary_access_token', credentials)
        .success(function (data, status, headers, config) {
            $scope.accessToken = data.AccessToken;
            deferred.resolve($scope.accessToken);
        });
        return deferred.promise;
    };

    $scope.doit = function() {
        $scope.getTemporaryAccessToken ().then(function() {
            $http({
                method: 'GET',
                url: '/api/1.0/johndoe/notes?include_notes=true',
                headers: { 'AccessToken': $scope.accessToken }
            }).success(function (data, status, headers, config) {
                console.log(data);
                $scope.notes = data.notes;
            });
        });
    };

    $scope.saveNote = function() {
    	var note = $scope.selectedNote;

    	var req = {
    		"latest-sync-revision": $scope.notes['latest-sync-revision'] + 1,
    	}
    	req['note-changes'] = [ note ];

    	console.log(req);

    	$http({
    		method: 'PUT',
    		url: '/api/1.0/johndoe/notes',
    		headers: { 'AccessToken': $scope.accessToken },
    		data: req
    	}).success(function (data, status, headers, config) {
    		console.log(data);
    		console.log(status);
    	});
    };

    $scope.doit();
}
