Rainy - simple note syncing server for Tomboy
=============================================

(This documentation is best viewed through the [markdown.io renderer][this])

Introduction
------------

Rainy is a synchronization/cloud server intended for use with [Tomboy][tomboy] and other Tomboy-like clients (like [Tomdroid][tomdroid]). Although there exists [Snowy][snowy], Rainy is more lightweight and designed for smaller, private groups like friends and families. Rainy does not use a heavy SQL database as backend but a simple directory to store its notes and metadata.

### Facts:

  * written in C# and runs with [mono][mono] on all major platforms (could for example be hosted at a small home server or NAS, cheap VPS/cloud server, or even an a OpenWRT router)
  * re-uses the existing [tomboy-library][tomboylib], which is a C# library that should one day put in place into Tomboy, and could be used by other potential clients
  * uses the awesome [ServiceStack][servicestack] framework for providing the [Tomboy REST API][tomboyrest]
  * licensed under the free [GNU AGPLv3 license][agplv3]

  [this]: http://markdown.io/https://raw.github.com/Dynalon/Rainy/master/docs/README.md
  [tomboy]: http://projects.gnome.org/tomboy/
  [tomboylib]: https://github.com/trepidity/tomboy-library
  [tomdroid]: https://launchpad.net/tomdroid
  [tomboyrest]: https://live.gnome.org/Tomboy/Synchronization/REST/1.0
  [snowy]: http://git.gnome.org/browse/snowy
  [servicestack]: http://www.servicestack.net/
  [mono]: http://www.mono-project.com
  [agplv3]: http://www.gnu.org/licenses/agpl-3.0.html


Getting Started
---------------

See the [build instructions][build] on how to build and run Rainy.

  [build]: http://markdown.io/https://raw.github.com/Dynalon/Rainy/master/docs/BUILDING.md



Contribute
----------

Patches (or even better, pull requests) are highly welcome! You can also report any issues through the GitHub issue tracker. You can also reach me at twitter (see below) if you have any problems or questions.


Credits
-------

Initial Rainy build done and maintained  by [Timo Dörr](https://twitter.com/timodoerr). Tomboy-library created by [Jared Jennings](https://twitter.com/jaredljennings) and others.
