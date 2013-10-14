Contribute
==========

- - -

Test and report bugs
--------------------

The easiest way to help out rainy development is to test unstable and stable versions, and report all bugs that you encounter to the [issue tracker][gh-issue-tracker]. Please try to be as specific as possible when reporting a bug, and give as much details as you can, including

* what you did to hit the bug, preferably a step-by-step description of how to reproduce the bug
* the version of Rainy that was used
* the version of Tomboy or Tomdroid
* Operating system and distribution version

If possible, inlude a full debug log of Rainy when hitting the bug. A log will be saved to the file `debug.log` if you start Rainy with the `-vvvv` parameter:

    mono Rainy.exe -c settings.conf -vvvv


Add code or features
--------------------

Currently Rainy consists of two major parts that you can contribute to independently, depending on your technical background and skills:

### Backend (C#/.NET)

If you are familiar with C# (or Java which is very similiar) or another OO-language, your help is greatly welcome. You don't need Windows or Visual Studio, as Rainy is mainly developed using [MonoDevelop][monodevelop] (Linux) or [Xamarin Studio][xamstudio] (OS X / Windows) (both are free as in beer).

Check the [backend section in the Hacking HOWTO][hackingbackend] to get started!

### Frontend (HTML5/Javascript)

If you are more a web developer type, you can also help to improve Rainy. Rainy's frontend is built using [AngularJS][angular] and [Bootstrap][bootstrap]. Even if you don't know any of those, basic HTML/CSS/Javascript knowledge is enough to get started. There is __no need__ to know any backend technologies, as the frontend is a standalone single-page application completely independent of any server-side code.

Check the [frontend section in the Hacking HOWTO][hackingfrontend] to get started!

 [bootstrap]: http://getbootstrap.com
 [angular]: http://www.angularjs.org
 [monodevelop]: http://www.monodevelop.com
 [xamstudio]: http://www.xamarin.com/download
 [hackingbackend]: /developer/hacking.md#backend
 [hackingfrontend]: /developer/hacking.md#frontend

Improve documentation and website
---------------------------------

The documenation and website is included in the `docs` folder in the source code and displayed with [MDwiki][mdwiki]. Standard [Markdown][markdown] is used for formatting. This means that you can easily contribute to the documentation/website if you fork on github, edit the file in the docs folder, and send a pull-request. Changes to the website will take effect immediately after your pull-request is merged.


  [gh-issue-tracker]: https://github.com/Dynalon/Rainy/issues
  [rainy]: https://github.com/Dynalon/Rainy/
  [markdown]: http://daringfireball.net/projects/markdown/
  [MDwiki]: http://www.mdwiki.info


