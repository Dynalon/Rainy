module.exports = function (grunt) {

    // Project configuration.
    grunt.initConfig({
        jshint: {
            options: {
                browser: true
            },
            files: ['app_*/js/*.js']
        },
        concat: {
            options: {
                separator: '\n'
            },
            scripts: {
                src: [
                    'bower_components/angular/angular.min.js',
                    'bower_components/angular-strap/dist/angular-strap.min.js',
                    'app_admin/js/**/*.js',
                ],
                dest: 'dist/built.js'
            },
            css: {
                src: [
                    'lib/*.css'
                ],
                dest: 'dist/style.css'
            },
            admin_ui: {
                src: ['app_admin/index.html', 'app_admin/html/*.html'],
                dest: 'dist/manage.html'
            },
        },
        watch: {
            files: ['app_*/**/*.js', 'app_*/**/*.html'],
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
