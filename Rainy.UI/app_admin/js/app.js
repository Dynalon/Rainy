
// Declare app level module which depends on filters, and services
var app = angular.module('myApp', ['myApp.filters', 'myApp.services', 'myApp.directives']).
  config(['$routeProvider', function($routeProvider) {
    $routeProvider.when('/user', {templateUrl: 'user.html', controller: AllUserCtrl});
    $routeProvider.when('/overview', {templateUrl: 'overview.html', controller: StatusCtrl});
    $routeProvider.when('/login', {templateUrl: 'login.html', controller: LoginCtrl});
    $routeProvider.otherwise({redirectTo: '/overview'});
  }])

  // disable the X-Requested-With header
  .config(['$httpProvider', function($httpProvider) {
      delete $httpProvider.defaults.headers.common["X-Requested-With"];
  }])
  .config(['$locationProvider', function($locationProvider) {
//      $locationProvider.html5Mode(true);
  }]);


