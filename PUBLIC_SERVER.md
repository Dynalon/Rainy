Rainy public hosted demo server
================================

## About

There is a test server running the latest rainy release available, that can be used by anyone who wants to try out rainy, or want to develop a [Tomboy sync API][restapi] compatible application.

Since this is for testing purposes only, there are no private account registration possible (yet).  Below you will find a list of valid username/password combinations, each representing a single account. If you pick one randomly, chances are good that no one will interfere with your notes while testing.

  [restapi]: https://live.gnome.org/Tomboy/Synchronization/REST

## HTML5 WebGUI

To use the HTML5/Javascript based in-browser client, use any of belows username/password pairs and follow this link:

[Open Demoserver with Web GUI](https://rainy-demoserver.latecrew.de/)

Note: You will have to add a security exception for the SSL certification, as the test server does not use an expensive CA signed certificate but a self-signed one.

## Tomboy/Tomdroid

Pick a username/password combination below, and then use this URL in Tomboy/Tomdroid as a sync url:

	https://rainy-demoserver.latecrew.de/

You will have to enter the username/password pair later, after you added this server in Tomboy/Tomdroid.

Note: Double-check you enter _HTTPS_ (with a trailing 's'), as using _HTTP_ will not work.

You can also use unattended authentication by appending `/<username>/<password>/` to the Url:

	https://rainy-demoserver.latecrew.de/<username>/<password>/

- - -
## Public account list

	User	Password
	-----------------
	testuser	testpass
	aiden	QSmCmH
	alexander	fcOYGZ
	alexis	XwG4Hy
	allison	Fm84Pz
	alyssa	msS0yK
	amelia	MmFTkh
	andrew	dhFHJu
	anna	jMmkjo
	anthony	sbck8m
	ashley	NkPu9U
	aubrey	Q0JkFr
	audrey	WNmaru
	ava	vxpGuz
	avery	fQZPjm
	benjamin	QlRHFr
	brandon	9EQUYz
	brayden	TERA4w
	brianna	480eZe
	brooklyn	bl3cqZ
	caleb	b9IIS3
	camila	jb4QR5
	carter	Og5630
	charlotte	SM9yUr
	chloe	xy0gfH
	christian	JFpfFr
	christopher	gXEuhD
	claire	Tks9GN
	daniel	7djYGV
	david	uT4kWZ
	dylan	lPinW0
	elijah	yW9YQY
	elizabeth	VNquj0
	ella	vPMMfj
	emily	Y5LLgf
	emma	oH7Lda
	ethan	UdGHfc
	evan	rnKrac
	evelyn	DTjkV1
	gabriel	3qRnkp
	gabriella	IujWTS
	gavin	bFDyb5
	grace	zEHd9O
	hailey	jgOwtp
	hannah	SN5OPs
	isaac	WS71tv
	isabella	ghrF6b
	isaiah	9cUEET
	jack	ZINEES
	jackson	x7kslI
	jacob	hhx9q0


## Private accounts

If you are _a developer_ and for some reason need a private account for testing, [contact me][mymail] and I will set you up with an account.

  [mymail]: mailto:timo@latecrew.de
