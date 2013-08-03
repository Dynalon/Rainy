/*global $:false */
/*global angular:false */
angular.module('myApp.directives', [])
    .directive('appVersion', ['version',
        function(version) {
            return function(scope, elm, attrs) {
                elm.text(version);
            };
        }
    ])
    .directive('bsNavbar', function($location) {
        'use strict';

        return {
            restrict: 'A',
            link: function postLink(scope, element, attrs, controller) {
                // Watch for the $location
                scope.$watch(function() {
                    return $location.path();
                }, function(newValue, oldValue) {

                    $('li[data-match-route]', element).each(function(k, li) {
                        var $li = angular.element(li),
                            // data('match-rout') does not work with dynamic attributes
                            pattern = $li.attr('data-match-route'),
                            regexp = new RegExp('^' + pattern + '$', ['i']);

                        if (regexp.test(newValue)) {
                            $li.addClass('active');
                        } else {
                            $li.removeClass('active');
                        }

                    });
                });
            }
        };
    })
    .directive('showtab', function() {
        return {
            link: function(scope, element, attrs) {
                element.click(function(e) {
                    e.preventDefault();
                    $(element).siblings().removeClass('active');
                    $(element).addClass('active');
                });
            }
        };
    });
