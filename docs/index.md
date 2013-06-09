Rainy is an open source synchronization server that allows to sync notes with Tomboy and Tomdroid.
Follow @timodoerr for all rainy-related announcements!
* * *
[gimmick:twitterfollow](@timodoerr) &nbsp; [gimmick:FacebookLike ( layout: 'buttoncount') ](http://www.facebook.com/pages/Rainy-note-sync-server-for-Tomboy/116321368557123) &nbsp; <script type="text/javascript" src="https://apis.google.com/js/plusone.js"></script> <g:plusone size="medium" href="http://www.notesync.org"></g:plusone>
* * *

###### June, 2. 2013
## Rainy accepted at Google Summer of Code 2013!

I am glad to announce that I have been accepted as a [Google Summer of Code][gsoc] student to work on Rainy during this summer. The [Mono project][monoproject] is the mentoring organization that will help and mentor me in this enjoyable endeavour. As a result, there are several feature planned to be available by the end of the summer, see the [my original proposal][proposal] for the GSoC.

If you want to track my progress on the project, checkout [my blog][myblog] and the [GitHub][github-rainy] project website regularly!


  [myblog]: http://exceptionrethrown.wordpress.com
  [gsoc]: https://developers.google.com/open-source/soc/
  [proposal]: http://www.google-melange.com/gsoc/project/google/gsoc2013/dynalon/27001
  [monoproject]: http://mono-project.com
  [github-rainy]: httpw://github.com/Dynalon/Rainy

- - -

###### March, 15. 2013
## New website, new release!

I commited some changes to the website today. Most notable change (besides the switch to the [bootswatch united theme][united]) is this _news_ section. I intend to use it to put news about Rainy releases and important information on the website, apart of [my blog][blog].

Documentation was also updated, and includes now instructions of how to perform SSL setup.

## Introducing the 0.2.X release

The 0.2.X release series brings the long-awaited SSL support. This means that connections are now finally encrypted, which makes rainy so much more usable and secure.

Thanks to the work of [Stefan Hammer][stefan], the latest [Tomdroid][tomdroid] releases (stable from the market + development build) do now also support self-signed certificates for SSL connection, so if you upgrade to latest Tomdroid you are ready to sync with Rainy via SSL.
[![](http://launchpadlibrarian.net/79043149/icon-64.png)](https://launchpad.net/tomdroid)

ATTENTION: The 0.2.X release uses a different database scheme, so if you are using the sqlite backend and upgraded from the 0.1.X series you must delete the `rainy.db` file and start over (your notes from Tomboy/Tomdroid will be saved in the new DB upon first sync). The settings.conf file format has changed, too, so better start from scratch!


[united]: http://bootswatch.com/
[blog]: http://exceptionrethrown.wordpress.com/
[stefan]: https://plus.google.com/107845688101586158412
[tomdroid]: https://launchpad.net/tomdroid
