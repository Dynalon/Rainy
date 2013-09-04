angular.module('clientApp').factory('configService', function($http) {
    var configService = {};
    var conf = {};

    Object.defineProperty(configService, 'serverConfig', {
        get: function () {
            // always return a copy as we don't allow edits
            return conf;
        }
    });

    $http.get('/api/config').success(function (data) {
        conf = $.extend(conf, data);
    });

    return configService;
});
