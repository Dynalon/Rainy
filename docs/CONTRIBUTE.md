Contribute
==========

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

New code is always welcome, just clone the [Rainy sourcecode][rainy] and send a pull-request. If you are not familiar with git, patches are welcome too!

If you want to implement a feature, it would be nice if you open up an issue on the [issue tracker]][gh-issue-tracker] describing the feature and assign yourself to it, so others can see what areas are worked on.

Improve documentation and website
---------------------------------

The documenation and website is included in the `docs` folder in the source code. Standard [Markdown][markdown] is used for formatting. To display the most recent docs from github as the website you are reading *right now*, the [markdown realtime renderer markdown.io][markdown.io] is used. This means that you can easily contribute to the documentation/website if you fork on github, edit the file in the docs folder, and send a pull-request. Changes to the website will take effect immediately after your pull-request is merged.


[gh-issue-tracker]: https://github.com/Dynalon/Rainy/issues
[rainy]: https://github.com/Dynalon/Rainy/
[markdown]: http://daringfireball.net/projects/markdown/
[markdown.io]: http://www.markdown.io/


