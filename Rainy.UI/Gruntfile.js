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
                    'bower_components/jquery/jquery.min.js',
                    'bower_components/bootstrap.zip/js/bootstrap.min.js',
                    'bower_components/angular/angular.min.js',
                    'bower_components/angular-strap/dist/angular-strap.min.js',
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
                    'bower_components/wysihtml5/dist/wysihtml5-0.3.0.min.js',
                    'bower_components/bootstrap-wysihtml5/dist/bootstrap-wysihtml5-0.0.2.min.js',
                    'app_admin/js/client/**/*.js',
                    'app_admin/js/shared/**/*.js'
                ],
                dest: 'dist/client.js'
            },
            css_client: {
                src: [
                    'bower_components/bootstrap-wysihtml5/dist/bootstrap-wysihtml5-0.0.2.css'
                ],
                dest: 'dist/client.css'
            },
            admin_ui: {
                src: ['app_admin/admin.html', 'app_admin/html/views_admin/*.html'],
                dest: 'dist/admin/index.html'
            },
            client_ui: {
                src: ['app_admin/client.html', 'app_admin/html/views_client/*.html'],
                dest: 'dist/index.html'
            },
            wysihtml5_style: {
                src: [ 'app_admin/wysihtml5_style.css' ],
                dest: 'dist/wysihtml5_style.css'
            }
        },
        watch: {
            files: ['app_admin/**/*.js', 'app_admin/**/*.html', 'Gruntfile.js'],
            tasks: ['default']
        },
        reload: {
            port: 35729,
            liveReload: {}
        }
    });

    grunt.loadNpmTasks('grunt-contrib-jshint');
    grunt.loadNpmTasks('grunt-contrib-watch');
    grunt.loadNpmTasks('grunt-contrib-concat');
    grunt.loadNpmTasks('grunt-reload');

    // Default task.
    grunt.registerTask('default', ['concat', 'reload', 'jshint', 'watch']);

};
