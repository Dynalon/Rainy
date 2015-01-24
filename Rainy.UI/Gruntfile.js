module.exports = function (grunt) {

    // Project configuration.
    grunt.initConfig({
        jshint: {
            options: {
                jshintrc: '.jshintrc'
            },
            files: [ 'app_admin/**/*.js' ]
        },
        concat: {
            options: {
                separator: '\n'
            },
            scripts_common: {
                src: [
                    'bower_components/jquery/dist/jquery.min.js',
                    'bower_components/bootstrap/dist/js/bootstrap.min.js', 
/*                    'bower_components/bootstrap.zip/js/bootstrap.min.js', */
                    'bower_components/angular/angular.js',
                    'bower_components/angular-route/angular-route.js',
                    'bower_components/underscore/underscore-min.js',
                    'bower_components/noty/js/noty/jquery.noty.js',
                    'bower_components/noty/js/noty/themes/default.js',
                    'bower_components/noty/js/noty/layouts/topCenter.js'
                ],
                dest: 'dist/common.js'
            },
            scripts_admin: {
                src: [
                    'app_admin/js/admin/**/*.js'
                ],
                dest: 'dist/admin/admin.js'
            },
            scripts_client: {
                src: [
/*                    'bower_components//dist/wysihtml5-0.3.0.min.js', */
                    'bower_components/bootstrap3-wysihtml5-bower/dist/bootstrap3-wysihtml5.all.js',
                    'app_admin/js/client/**/*.js',
                    'app_admin/js/shared/**/*.js'
                ],
                dest: 'dist/client.js'
            },
            css_client: {
                src: [
                    'bower_components/bootstrap/dist/css/bootstrap.min.css',
                    'bower_components/bootstrap3-wysihtml5-bower/dist/bootstrap3-wysihtml5.css',
/*                    'bower_components/bootstrap.zip/css/bootstrap.min.css', */
/*                    'bower_components/bootstrap-wysihtml5/dist/bootstrap-wysihtml5-0.0.2.css', */
/*                    'bower_components/bootstrap-switch/static/stylesheets/bootstrap-switch.css' */
                ],
                dest: 'dist/client.css'
            },
            admin_client : {
                src: [
                    'bower_components/bootstrap/dist/css/bootstrap.min.css'
                /*    'bower_components/bootstrap.zip/css/bootstrap.min.css', */
                ],
                dest: 'dist/admin/admin.css'
            },
            admin_ui: {
                src: ['app_admin/admin.html', 'app_admin/html/views_admin/*.html'],
                dest: 'dist/admin/index.html'
            },
            client_ui: {
                src: ['app_admin/client.html', 'app_admin/html/views_client/*.html'],
                dest: 'dist/index.html'
            },
        },
        copy: {
            wysi: {
                expand: false,
                flatten: true,
                src: [ 'app_admin/css/wysihtml5_style.css' ],
                dest: 'dist/wysihtml5_style.css'
            },
            favicon: {
                expand: false,
                flatten: true,
                src: [ 'app_admin/favicon.ico' ],
                dest: 'dist/favicon.ico'
            }
        },
        watch: {
            files: [
                'app_admin/**/*.js',
                'app_admin/**/*.html',
                'app_admin/**/*.css',
                'Gruntfile.js'
            ],
            tasks: ['default']
        },
        reload: {
            port: 35729,
            liveReload: {}
        }
    });

    grunt.loadNpmTasks('grunt-contrib-jshint');
    grunt.loadNpmTasks('grunt-contrib-watch');
    grunt.loadNpmTasks('grunt-contrib-copy');
    grunt.loadNpmTasks('grunt-contrib-concat');
    grunt.loadNpmTasks('grunt-reload');

    // Default task.
    grunt.registerTask('default', ['concat', 'copy', 'jshint' ]);
    grunt.registerTask('devel', [ 'reload', 'watch' ]);
};
