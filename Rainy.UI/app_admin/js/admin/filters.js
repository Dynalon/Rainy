/*global $:false */
/*global angular:false */

angular.module('adminApp.filters', []).
    filter('interpolate', ['version', function(version) {
    return function(text) {
        return String(text).replace(/\%VERSION\%/mg, version);
    };
}]);
