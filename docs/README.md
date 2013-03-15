Rainy - simple note syncing server for Tomboy
=============================================

About
-----

Rainy is a free synchronization/cloud server intended for use with [Tomboy][tomboy] and other Tomboy-like clients (like [Tomdroid][tomdroid]). Rainy can be easily hosted in your own private cloud, and gives you the control over your data and privacy.

### Some Facts:

  * has two different data backends that can be chosen: plain XML files (one per note) or single sqlite database
  * written in C# and runs with [mono][mono] on all major platforms (could for example be hosted at a small home server, NAS or cheap VPS/cloud server)
  * easy deployment through single _Rainy.exe_ file that only requires mono to run, all other libraries are packed, merged and statically linked
  * re-uses the existing [tomboy-library][tomboylib], which is a C# library that should one day put in place into Tomboy, and could be used by other potential clients
  * uses the awesome [ServiceStack][servicestack] framework for providing the [Tomboy REST API][tomboyrest] and the [ServiceStack.ORMLite][ss-ormlite] O/R Mapper  for database access
  * licensed under the free [GNU AGPLv3 license][agplv3]

  [tomboy]: http://projects.gnome.org/tomboy/
  [tomboylib]: https://github.com/trepidity/tomboy-library
  [tomdroid]: https://launchpad.net/tomdroid
  [tomboyrest]: https://live.gnome.org/Tomboy/Synchronization/REST/1.0
  [snowy]: http://git.gnome.org/browse/snowy
  [servicestack]: http://www.servicestack.net/
  [ss-ormlite]: https://github.com/ServiceStack/ServiceStack.OrmLite
  [mono]: http://www.mono-project.com
  [agplv3]: http://www.gnu.org/licenses/agpl-3.0.html


Getting Started
---------------

See the [Getting Started guide][gettingstarted] on how to get and setup Rainy.

  [gettingstarted]: http://www.notesync.org/GETTING_STARTED.md


Credits
-------

Initial Rainy build done and maintained  by [Timo Dörr](https://twitter.com/timodoerr). Tomboy-library created by [Jared Jennings](https://twitter.com/jaredljennings) and others.


  [tomboy-ml]: http://lists.beatniksoftware.com/listinfo.cgi/tomboy-list-beatniksoftware.com
  [issue-tracker]: https://github.com/Dynalon/Rainy/issues

