app.factory('clientService', function($q, $http, $rootScope) {
    var clientService = {
        notes: [],
        last_sync_revision: 0,
        accessToken: '',
        userDetails: {
            username: 'johndoe'
        }
    };

    var useStorage = window.sessionStorage && window.localStorage;
    if (useStorage) {
        clientService.accessToken = window.localStorage.getItem ('accessToken');
    } 

    clientService.login = function (user, pass, remember) {
        var deferred = $q.defer();
        var expiry = 1440; // 1d

        if (remember)
            expiry = 14 * 1440; // 14d

        var credentials = {
            Username: user,
            Password: pass,
            Expiry: expiry
        };

        $http.post('/oauth/temporary_access_token', credentials)
        .success(function (data, status, headers, config) {
            clientService.accessToken = data.AccessToken;
            if (useStorage && remember) {
                window.localStorage.setItem('accessToken', data.AccessToken);
            }
            $rootScope.$broadcast('loginStatus', true);
            deferred.resolve();
        })
        .error(function (data, status) {
            deferred.reject(status);
            $rootScope.$broadcast('loginStatus', false);
        });
        return deferred.promise;
    };

    clientService.logout = function () {
        clientService.accessToken = '';
        if (useStorage) {
            window.localStorage.removeItem('accessToken');
        }
        $rootScope.$broadcast('loginStatus', false);
    };

    clientService.userIsLoggedIn = function () {
        // TODO check for expiry
        var logged = !(clientService.accessToken === '' ||
            clientService.accessToken === undefined);

        if (useStorage && logged)
            return window.localStorage.getItem('accessToken') || false;  
        else return logged;
    };

    clientService.fetchNotes = function() {
        $http({
            method: 'GET',
            url: '/api/1.0/johndoe/notes?include_notes=true',
            headers: { 'AccessToken': clientService.accessToken }
        }).success(function (data, status, headers, config) {
            clientService.notes = data.notes;
        });
    };

    clientService.saveNote = function(note) {

        clientService.latest_sync_revision++;
        var req = {
            'latest-sync-revision': clientService.latest_sync_revision,
        };
        req['note-changes'] = [ note ];

        $http({
            method: 'PUT',
            url: '/api/1.0/johndoe/notes',
            headers: { 'AccessToken': clientService.accessToken },
            data: req
        }).success(function (data, status, headers, config) {
            console.log('successfully saved note');
        });
    };

    return clientService;
});