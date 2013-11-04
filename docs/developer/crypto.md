Rainy's Crypto System
=====================

- - -

Rainy implements a pure _server side_ crypto system that encrypts and decrypts notes upon synchronization on the fly. This page documents how the encryptions works and should be considered a developer manual.

The crypto system only takes care of the _stored_ data, not how the data is securely transfered. This is done by relying on HTTP over SSL (HTTPS).

Definitions
-----------

  * `Client`: A synchronization client, like Tomboy or Tomdroid
  * `Server`: The Rainy instance
  * `Client data store`: The data store of any client (i.e. the configuration settings that contain the oauth tokens, sync GUIDs etc) like Tomboy or Tomdroid, which consists of the data of a _single_ user
  * `Server data store`: Rainy's data store, which is usually a database like SQLite or PostgreSQL and contains data of _all_ users
  * `Client device`: A device like a laptop or mobile phone that carries the (unencrypted) client data store

Requirements
------------

Rainy's server-side crypto system is designed with the following requirements in mind:

  * Client-side encryption is not currently present but can't be added at any later time without interfering with the server-side encryption
  * Loss of the server data store (i.e. through a system compromise / hacking attack) does not expose the cleartext note content of any user, nor the password
  * Single notes can be made publicly available in future Rainy versions by supplying a per-note key. The public knowledge of the per-note key should not harm security for all other notes.
  * Loss of a client device can be non-fatal if the user revokes the OAuth token / key immediately by authentication with his password from another device. Revoking a key __should not__ require any re-encryption and leaves all other keys on other devices intact and secure.
  * The user can change his password at any time without re-authenticating any already authenticated device

Note: Whether or not the notes on a client device are exposed upon loss depends on the client data store implementations, i.e. if the client data store is encrypted or not. This is beyond the scope of Rainy.

Client requirements
-------------------

In order to establish a secure system, the clients should follow some simple rules:

  * The users password should not be stored permanently but only asked for during OAuth token exchange. After the exchange, only the oauth access token is stored.
  * SSL certificates should be checked for trusted issuer or require user confirmation upon first connection

Implementation
--------------

### General

AES256 symmetric encryption is used for all encryption, and [PBKDF2][pbkdf2] is used for key derivation / hashing. No other crypto algorithms are employed.

### Setup

Upon user creation we initialize a few crypto fields:

  * a randomly generated 128 bit field that serves as the users password salt
  * a randomly generated 128 bit field that serves as the `MasterKeySalt`
  * a randomly generated 256 bit symmetric AES key which serves as the `MasterKey` (on a per-user basis)

The users password hash is derived via [PBKDF2][pbkdf2] using the password salt as IV. the first 256 bit of this hash is stored in the server data storage and used for user authentication.

Since the `MasterKey` is not allowed to be stored in plaintext in the server data storage we need to encrypt it. Therefore, the users password is again used to derive a key via PBKDF2 using the `MasterKeySalt` as IV. The resulting key is used to encrypt the plaintext `MasterKey`, the the encrypted MaterKey is put into the server data storage. The password-derived key __is not stored__.

To retrieve the plaintext `MasterKey` at any later time, the users password and the `MasterKeySalt` are required as input to the PBKDF2 function. This is only possible during the OAuth token exchange phase (which only occurs once for a device), as the password is not transmitted on further client requests.

After the initialization, the server data storage does not hold enough information to retrieve the plaintext `MasterKey`, as the users password is not stored.

### OAuth token exchange

Clients use OAuth for authentication. The `access token` is the only credential sent to the server on the token exchange has finished. Token exchange only happens once, which is usually at the time the device is setup for synchronization. For this reason, the access token has to carry important crypto information that is required to start the decryption process.

During the OAuth token exchange, the user sends his password to the server. At this point, using the password, we decrypt the `MasterKey` and encrypt it with a newly generated random 256 bit key called the `Token key` using the `MasterKeySalt` as IV. The thus newly encrypted MaterKey form the `access_token` that is sent back to the client. The token key is unique and newly generated for every issued access token.

For further authorization requests, the user sends the access token to the server. Since we are not allowed to store the access token (because that would allow us to decrypt the master key without user interaction), we only store the first 192 bits of the access token (Note that due to the padding added by PBKDF2 the access token is 920 bits long). Authorization can then be done by comparing the first 192 bits of the access token, which is enough for authentication but too few to decrypt the `MasterKey`.

### Note encryption

For every note, a new random 256 bit `per-note key` is generated that is used to encrypt/decrypt the note body. Since we can't simply store that key in the server data store (again), this key is encrypted using the `MasterKey` and stored together with the note. If a user wishes to publicly share a note, the encrypted per-note key is decrypted and embedded into the sharing url. This of course destroys all encryption security (which does not matter as the note is public).

Note: The access token is used to decrypt the MasterKey, which is then used to decrypt the per-note Key, which in turn is used to decrypt the note body.

### Possible attack vectors

Since we only implement server-side encryption, there are still some scenarios that could be used to hijack a user's notes:

  * If an attacker can get control of the server and modify the code or decrypt the SSL stream (with the access to the SSL certificate private key) the password and/or the access tokens can be retrieved, and together with the server data  store be used to decrypt all keys and notes
  * The password is the key to everything. If the user gives away his password (i.e. through social engineering), an attacker can setup a new device and download all notes in plaintext.

Warning: Even with server-side encryption available, the party hosting the Server / Rainy instance can still access your notes by hijacking the keys at the point you send them to the server. You have to trust the server administrator. The encryption only protects from unwanted data leakage, like i.e. SQL dumping attacks.

  [pbkdf2]: http://en.wikipedia.org/wiki/PBKDF2
