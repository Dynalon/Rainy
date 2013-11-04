angular.module('clientApp').factory('configService', function($http) {
    var configService = {};
    var conf = {};

    Object.defineProperty(configService, 'serverConfig', {
        get: function () {
            return conf;
        }
    });

    $http.get('/api/config').success(function (data) {
        conf = $.extend(conf, data);
    });

    return configService;
});
