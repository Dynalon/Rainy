Hacking HOWTO
=============

- - -

The latest source code for Rainy is hosted over at [github](http://www.github.com/Dynalon/Rainy). Get the source code with

    git clone https://github.com/Dynalon/Rainy.git

Feel free to send pull requests once you made changes that you want to have upstreamed.

Backend (C#/.NET)
-----------------

This section describes how to setup a build environment to start hacking on Rainy. If you only want to build Rainy from source (i.e. to package it for your distribution), check the [building from source section in the getting started guide][buildfromsource]

  [buildfromsource]: ../GETTING_STARTED.md#Building_from_source

### Requirements
You will need to install [MonoDevelop](http://www.monodevelop.org) (Linux) or [Xamarin Studio](http://www.xamarin.com/download) (OS X, Windows). Visual Studio on Windows might also work. Also, a copy of [git](http://www.gitscm.org) is needed.

Note: If you are on __Linux__, you need to install the `mono-complete` metapackage that comes with major distros (Debian, openSUSE, Fedora) to avoid missing libraries excetions.

### Fetch dependencies

Rainy depends on some external git repositories which needs to be checked out first. To do so, run

    make checkout

Rainy also requires some binary CIL libraries that are fetched via NuGet. Rainy comes with a pre-build NuGet.exe, so no need to install that seperately. In the first step, you have to install latest trusted SSL certificate into mono's trust root:

    mozroots --import --sync

Then, to fetch all nuget packages, run

    make deps

### Edit configuration

There is a settings.conf located at `$PROJECT_ROOT/Rainy/settings.conf` that needs some adjustment. It is well-commented, as should explain itself. During development it is a good idea to turn SSL of by using a `http://` prefixed url, so that you can use network monitors to debug data exchange. Another options is  to turn the `Development` option to true. This will add a user `dummy` with password `foobar123` on first startup and initialize the database for you.

### Start coding

You can now fire up MonoDevelop/XamarinStudio and open the `Rainy.sln` file in the project's root directory. Building and running works from within MonoDevelop/XamarinStudio.

### Documentation

  * This wiki
  * [Tomboy REST API Specification 1.0](https://wiki.gnome.org/Apps/Tomboy/Synchronization/REST/1.0)
  * [Rainys extended REST API documentation](http://www.notesync.org/apidoc/)
  * [ServiceStack Wiki](https://github.com/ServiceStack/ServiceStack/wiki)

Don't hesitate to email the maintainer(s) directly per Email if you have questions about the sorce code or architecture, contributors are welcome!

Happy hacking!

- - -

Frontend (HTML5/Javascript)
---------------------------

The HTML5 frontend is contained in the `Rainy.UI` folder in the project root. To get started, you need to compile from source as described in the above backend section, with the exception that you don't need to install XamarinStudio or Monodevelop (only `mono-complete` is required). Instead, just call `make build` as the last step. To run Rainy, change into the `$PROJECT_ROOT/Rainy/bin/Debug` folder, and run Rainy via `mono --debug Rainy.exe -vvvv`.

Note: It is *mandatory* to set `Development: true` in the `settings.conf` file for frontend development, else your changes to the Rainy.UI folder will not be served to the browser (instead, static copies that are embedded in Rainy.exe are servered).

### Install dependencies

You will need to have installed:

  * node.js >= 0.8 with npm

All other deps are installed via `npm` (including [bower][bower]). Change into the `Rainy.UI` directory and run:

    npm install

### Start coding

Start grunt (which got installed by npm):

    grunt --force

or from your local directory:

    ./node_modules/.bin/grunt --force

If you now make any changes to the HTML or Javascript files, everything gets compiled and automatically put into the `dist/` folder.

Note: Don't edit any files within the __dist/__ folder. They will get overwritten on each build.

You will most likely want to start with the `app_admin` folder, which is the [AngularJS][angular] root.

### Documentation

  * [AngularJS][angular]
  * [AngularUI](http://angular-ui.github.io/)
  * [Bootstrap](http://getbootstrap.com)
  * [Bootstrap-Wysihtml5](http://jhollingworth.github.io/bootstrap-wysihtml5/)
  * [Grunt](http://gruntjs.com/)


  [angular]: https://angularjs.org
  [bower]: https://github.com/bower/bower
