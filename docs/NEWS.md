* * *
[gimmick:twitterfollow](@timodoerr) Follow @timodoerr for all rainy-related announcements!
* * *

## New webiste, new release!
###### March, 15. 2013

I commited some changes to the website today. Most notable change (besides the switch to the [bootswatch united theme][united]) is this _news_ section. I intend to use it to put news about Rainy releases and important information on the website, apart of [my blog][blog].

Documentation was also updated, and includes now instructions of how to perform SSL setup.

# Introducing the 0.2.X release

The 0.2.X release series brings the long-awaited SSL support. This means that connections are now finally encrypted, which makes rainy so much more usable and secure.

Thanks to the work of [Stefan Hammer][stefan], the latest [Tomdroid][tomdroid] releases (stable from the market + development build) do now also support self-signed certificates for SSL connection, so if you upgrade to latest Tomdroid you are ready to sync with Rainy via SSL.
[![](http://launchpadlibrarian.net/79043149/icon-64.png)](https://launchpad.net/tomdroid)

ATTENTION: The 0.2.X release uses a different database scheme, so if you are using the sqlite backend and upgraded from the 0.1.X series you must delete the `rainy.db` file and start over (your notes from Tomboy/Tomdroid will be saved in the new DB upon first sync). The settings.conf file format has changed, too, so better start from scratch!


[united]: http://bootswatch.com/
[blog]: http://exceptionrethrown.wordpress.com/
[stefan]: https://plus.google.com/107845688101586158412
[tomdroid]: https://launchpad.net/tomdroid
