Getting started
===============

Rainy currently runs on Linux and OS X. Windows may be supported, but more feedback is needed. When run on Windows, you will need to use the mono runtime instead of Microsoft's .NET. and a copy of a native sqlite3.dll (get it [here][sqlite]).

The following instructions cover Linux setup only.

  [sqlite]: http://www.sqlite.org

Using a downloaded binary release
---------------------------------

### 0. Install requirements:
  * mono
  * sqlite3 (usually no need to install this, as it comes with most distros by default)

On a Debian/Ubuntu based system, you could install the above requirements with:

	sudo apt-get install mono-complete libsqlite3-0

ATTENTION: It is advised to install the `mono-complete` or similiar meta-package that your distro ships, in order to avoid missing libraries exceptions.

### 1. Download and unzip

Fetch the latest version from the [download page](DOWNLOAD.md) and unzip:

    # replace the X.Y.Z with latest version
    wget http://rainy.notesync.org/release/rainy-X.Y.Z.zip

    # unzip and cd
    unzip rainy-X.Y.Z.zip

### 2. Edit settings.conf

There is a sample settings.conf which needs to be edited, i.e. change username/password and set the `DataPath`, which will tell Rainy where to store your notes. _Double check_ your settings.conf is valid JSON (besides the comment lines and the missing quotes for keys). Pay special attention not to have any invalid value delimiters (like a comma) in place where it does not belong.

### 3. SSL setup

If you use a `ListenUrl` that starts with the 'https://' prefix, rainy will use SSL for communication (recommended). Upon first start, a self-signed certificate and a private key will be created and stored in your `DataPath` (default: './data/').

Using a self-signed certificate with rainy is secure and imposes no risks. However, you will receive a warning message when the browser authentication takes place, as the certificate is not in your system's trusted certificate store. You have to manually add an exception for this certificate to proceed.

If you want to have your certificate signed by a CA, you can use the `makecert` tool that comes with mono to create a certificate. After your CA signed that, you can use the `--cert` parameter to tell rainy to use this certificate for SSL. Rainy only understands the .cer certificate format (not .pem!).

ATTENTION: When you change the `ListenUrl` to point to new domain, you have to generate a new certificate. Delete the ssl-cert.cer/.pvk from the `DataPath` and restart rainy to do this!

### 4. Start Rainy

    mono Rainy.exe -c settings.conf

If you want more verbose output (when supplying bug reports), you can change the loglevel by supplying the `-vvvv` parameter:

    mono --debug Rainy.exe -c settings.conf -vvvv

There is no daemon mode, but you can use and install `screen` on Linux to run rainy in the background:

    screen mono Rainy.exe -c settings.conf

After you have started a screen session, you may detach it by typing `CTRL+A`, then `CTRL+D`.

Alternately, if you want to start Rainy on startup in detached mode, you can use:

    screen -X rainy-session -d -m mono 'Rainy.exe -c settings.conf'

or use `mono-service`:

    mono-service Rainy.exe -c settings.conf

### 5. First sync in Tomboy

Now open up Tomboy (or another client, like [Tomdroid][tomdroid]), and point the synchronisation url to Rainy:

    https://yourserver.com:8080/<username>/<password>/

For the default settings.conf, there is a user `johndoe` with password set to `none`. In this case the example url would be:

    https://yourserver.com:8080/johndoe/none/

Click "Connect to server"; a browser instance should fire up and telling you immediatelly that the Tomboy authorization was successfull. You can now close the browser and start the first sync in Tomboy.

![](tomboy-url.png "Sample configuration in Tomboy")

### 6. Known issues

There are currently some issues in Tomboy and Tomdroid that you should know of before using Rainy.

* Tomboy
  * There is a [bug in Tomboy][tomboy-bug] regarding note templates, especially the "New Note Template" note that will always reappear and cause conflicts. If you happen to get a window in Tomboy telling you that a note with the title "New Note Template" already exists, choose to *rename the local note version*. **Do not chose overwrite the local note!**. The renamed note must be kept and not deleted.

* Tomdroid
  * The Tomdroid version in the Google Play store is outdated and syncing might not work
  * Syncing works best (two-way sync) when using at least version 0.7.2 from the [Tomdroid website][tomdroid]


  [tomboy-bug]: https://bugzilla.gnome.org/show_bug.cgi?id=665679
  [tomdroid]: https://launchpad.net/tomdroid

Building from source
--------------------

### 0. Install requirements:
  * Same as when using a binary release, **plus**
  * git
  * automake / autotools

On Debian/Ubuntu:
```
	sudo apt-get install build-essential automake git mono-complete libsqlite3-0
```

### 1. Building from source:

	git clone https://github.com/Dynalon/Rainy.git

	# will fetch any deps and compile rainy from source result will be compiled
	# into ./release/Rainy.exe single packed .exe that can be run with mono
	make

	# OPTIONAL: create a single, statically linked bundle for easy deployment
	# which required no other dependencies except sqlite3 (so no mono is required
	# to run, but executable will not be plattform independent).
	# The executable can then be found in ./release/linux/
	make linux_bundle

Now follow the same steps as when using a binary release.

