Build and run Rainy
===================

### 0. Install build requirements:
  * git
  * mono (it is best to install mono-complete or similiar meta-package on your linux distro to get everything and avoid missing libraries)

### 1. Fetch and prepare latest source code from github:

	git clone https://github.com/Dynalon/Rainy.git
	cd Rainy
	git submodule init
	git submodule update

### 2. Start compilation

	xbuild Rainy.sln

(if this step fails, you might want to update to latest mono)

### 3. Edit settings.conf

There is a sample settings.conf which needs to be edited, i.e. change username/password and set the `DataPath`, which will tell Rainy where to store your notes. _Double check_ your settings.conf is valid JSON (besides the comment lines and the missing quotes for keys). Pay special attention note to have any invalid value delimiters (like a comma) in place where it does not belong.

### 4. Start Rainy

	cd Rainy/bin/Debug/
	mono Rainy.exe -c ../../settings.conf

If you want more verbose output (helpfull when supplying bug reports), you can change the loglevel by supplying the `-vvvv` parameter:

	mono Rainy.exe -c ../../settings.conf -vvvv

### 5. First sync in Tomboy

Now open up Tomboy (or another client, like [Tomdroid][tomdroid]), and point the synchronisation url to Rainy:

	http://yourserver.com:8080/<username>/<password>/

For the default settings.conf, there is a user `johndoe` with password set to `none`. In this case the example url would be:

	http://yourserver.com:8080/johndoe/none/

Click "Connect to server"; a browser instance should fire up and telling you immediatelly that the Tomboy authorization was successfull. You can now close the browser and start the first sync in Tomboy.

![](tomboy-url.png "Sample configuration in Tomboy")

### 6. A word of warning & disclaimer

**Rainy is currently in alpha quality and not meant for production use, but for testing and development only. If you lose notes, don't blame me or any of the developers, you've been warned. Additionally, HTTPS is currently not supported - you will not want to sync your notes in a foreign network environment where someone can sniff your authentication data or note content.**

### 7. Known issues

There are currently some issues in Tomboy and Tomdroid that you should know of before using Rainy.

* Tomboy
  * There is a [bug in Tomboy][tomboy-bug-1] regarding note templates, especially the "New Note Template" note that will always reappear and cause conflicts. If you happen to get a window in Tomboy telling you that a note with the title "New Note Template" already exists, choose to *rename the local note version*. **Do not chose overwrite the local note!**. The renamed note must be kept and not deleted.

* Tomdroid
  * Due to a [bug in the stable version][tomdroid-bug-1], it is only capable of pulling notes *from* the server. Do not *modify* any notes and push them back, this will result in Tomboy to fail on the next syncing attempt.
  * Due to [another bug][tomdroid-bug-2], the first time you sync in Tomdroid might not show any notes. Just create more notes in *Tomboy* and do multiple syncs. Then sync Tomdroid.
  * I'm in contact with a Tomdroid maintainer to work those bugs out, and those will be fixed soon.


  [tomdroid]: https://launchpad.net/tomdroid
  [tomboy-bug-1]: https://bugzilla.gnome.org/show_bug.cgi?id=665679
  [tomdroid-bug-1]: https://bugs.launchpad.net/tomdroid/+bug/1074602
  [tomdroid-bug-2]: https://bugs.launchpad.net/tomdroid/+bug/1074676
