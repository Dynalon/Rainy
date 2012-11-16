Syncing documentation for Tomboy with remote webservers
=================

Note: This document is only intended for developers.

About
-----
This document describes the syncing protocoll used by Tomboy in recent versions and how a remote cloud note storage (like Snowy, Rainy, Ubuntu One) should respond and interact.

This document also describes the Tomboy REST API Version 1.0 only, described at <https://live.gnome.org/Tomboy/Synchronization/REST/1.0>

Throughout the text, I will refer to

- the *client* as a means of an instance of the Note-taking client like Tomboy, or alternative, compatible implementations like Tomdroid.
- The *server* is the part of cloud storage responsible for storing the Notes
- a *participant* is one of the participating syncing partners, either server or client
- A *note repository* represents all notes and notebooks of a given user. The client usually only manages one repository, whilst the server has only a single one (i.e. filesystem or ssh sync server) or multiple repositories (i.e. Snowy, Rainy, Ubuntu One) that it servers to a set of users.
- *syncing* as an abbreviation to the note synchronization process between a client and a server

Data structures
---------------

Every participant has its own, implementation specific represantation of a notes repository. When it comes to syncing, some extra data needs to be retrieved or stored on the client as well as on the server:

### Client side:

- Date and revision of the last sync
The client keeps a timestamp of the exakt time a sync successfully completed. The sync server **always** sends the global revision, which the client then stores into its own configuration file. A client **never** calculates a new global revision on its own.

- Server ID
The server carries a unique ID that each client that sync's against the server stores locally.

- Note deletions
Whenever a client deletes a note, that note must be removed upon the next sync from the server. Therefore, the client keeps a list of notes that were deleted since the last sync.
    - Tomboy keeps a simple list `NotesDeleted` in its manifest.xml file, which holds all GUIDs of notes that were deleted. After successfull syncing, this list is emptied.
    - Tomdroid keeps deleted notes, but does add the `system:deleted` tag. Those notes are hidden in the GUI to hide them from the user, and are only deleted after the next successfull sync. Compared to the Tomboy approach, this is suboptimal as the notes waste bandwidth, and might never be deleted if no sync will be performed.

- Notes changed since the last sync
The client has to keep track of what notes changed since the last successfull sync, because it has to tell the server what notes changed. There are multiple ways to do this:
    - Keep a list of *tainted* notes, or store a *tained* flag along with each note. Whenever the user makes changes to a note, the tainted flag gets set. Upon successfull sync, all tainted flags on all notes are reset.
    - Tomboy's (and tomboy-library's) approach: Keep track of the MetadataChangeDate. Tomboy stores a `DateTime` value on each note, that is updated to the current UTC time whenever a note is modified (besides changing the notes content, this can also be adding tags). Upon sync, Tomboy compares the last sync date with the MetadataChangeDate. If the MetadataChangeDate is higher than the last sync date, the note is selected as *modified* and send to the server for updating.
    This approach may fail if the local clock does *time warps*. If a user changes his clock, adds or removes days/hours, the comparison of the last sync date with the MetadataChangeDate might return wrong results. Keeping track of trainted notes is advised.

Server side (all per note repository):

- Date of the last sync
Like the client, the server also stores the date of the last sync. This is a single date value that is updated whenever a client successfully sync. The server *MUST NOT* use multiple sync dates (i.e. one for each client), but only maintain a single value. The value thus represents the timestamp that a client (no matter what client) last successfully synced.

- Latest sync revision
The server stores a single global revision number, that tells the global revision of the whole note repository.

- Note revisions
In contrast to the client, the server **MUST** maintain a list of ALL notes and their repsective revisions. A note revision on the server is **always** less or equal to the global latest sync revision that the server stores.

- Note deletions
In contrast to the client, the server **SHOULD NOT** store a list of notes deleted. This wouldn't make much sense anyways, as the server never deletes a note. Instead, user does on the client, and the client then deletes the note on the server on the next sync.

- Server ID
The server stores a unique ID that is unique for every note repository. This server id is sent to the client upon every sync.


Tomboy's data structures for sync information
---------------------------------------------

Tomboy does store all the above-mentioned data into a seperate file that is kept apart from the local note storage. This file is called `manifest.xml` and is usually located at `~/.config/tomboy/manifest.xml`. Note that this file represents the data structure of a *client* - since Tomboy itself is usually the client in a syncing transaction. The tomboy-library uses the same manifest.xml format.

Here is an example of a manifest.xml that holds all the required information for syncing:

	<?xml version="1.0" encoding="utf-8"?>
	<manifest xmlns="http://beatniksoftware.com/tomboy">
	  <last-sync-date>2012-11-14T15:39:46.8882480+01:00</last-sync-date>
	  <last-sync-rev>3</last-sync-rev>
	  <server-id>7755694e-c590-4cc9-9192-8c1d74a543b9</server-id>
	  <note-revisions>
	    <note guid="81487bb5-781f-4caa-92fb-11fdc08c2bd0" latest-revision="3" />
	  </note-revisions>
	  <note-deletions>
	    <note guid="ed6d0a10-a9cb-45bd-8647-2a80d0756de1" title="TODO" />
	  </note-deletions>
	</manifest>

Note: From what I found when looking to the tomboy sourcecode, the note-revisions field is ignored and just a relict of previous versions. There is **no need** to store note revisions on the client!

Syncing
-------

All syncing decision is currently done by the client. That means that the client decides

- whether a note should updated (retransmitted with updated content) *TO* the server
- whether a note should be updated locally, so downloading *FROM* the server 
  replacing the local note instance 
- whether a note gets deleted (locally and/or remotely on the server)

The server is solely responsible for executing the clients requests (retrieve, update, delete note), and is  providing secure storage of the notes.

It is noteworthy, that the various date values associated for each note (create-date, last-metadata-change-date, etc.) should not be used at all for synchronisation. It is ok to compare local timestamps with each other (as Tomboy does for selecting changed notes), but *under no circumstances* is it allowed to compare a server-generated timestamp with a client-generated timestamp. The reason is simple: Client and Server clocks may be out-of-sync and have completely different times set.

### Syncing relationship

A client is associated to exactly one server. That means, that every client at any given time can at max have a single in-sync note repository instance. To identify this relationship, the client and the server share a `current-sync-guid` (`server-id` in Tomboy's manifest.xml). Whenever the client switches to a server, the client must reset all syncing information and start with a fresh, clean sync. Resetting the client usually means:

* deleting the local `current-sync-guid` (or `server-id`) and update with the new id sent by the new server 
* reset the global `last-sync-rev` counter to -1, as well deleting the `last-sync-date` on the client
* reset any history of note deletions or tainting
* uploads all notes to the server to create a first copy of the local note
  repository

If the client decides to switch back to a server used before the current one, it MUST NOT reuse the previously used guid for that server, but start over resetting as described above and perform a full upload of all notes to the server.

Tomboy performs a reset basically by deleting it's manifest.xml file and starting with an empty one, that is filed with the values from the new sync server.

### Abstract Syncing algorithm

Here is some pseudocode of how a client and a server may be synced:

```
TODO
```

### Syncing protocoll

The REST API and datatypes are described in the REST API 1.0 documentation, see the introduction text in this document. Hence we focus on the logic in the syncing process. We assume the OAuth process has already taken place and the client starts to sync the notes.

1) Client asks for any changed notes

At the very beginning, the client sends a HTTP GET request to the notes url, which is most likely /api/1.0/<username>/notes

Along with the first request, the client sends his global revision counter `latest-sync-revision` in the JSON *as well as query parameter `since`* in the query url. (NOTE: Specifying the `since` parameter is encouraged to transmit as few as possible notes to save bandwidthAPI versions)

The client MAY use the `include_notes` parameters to get the full note body in this request. When using the `since` parameter, client SHOULD set the `include_notes` parameter to `true`, since the note's body has to be transfered at some point in the syncing process.

**NOTE**: The `since` parameter is somewhat redundant, as the `latest-sync-revision` is send by the client and usually should equal to the value in `since`. For historic reasons, `since` has precedence  over `latest-sync-revision`. Nonetheless, the fields should be equal with every client request anyways.

2) Server reponds with any changed notes

The server now checks the `since` parameter and responds with a list of notes for which `last-sync-revision` is *GREATER THAN* the revision specified in the `since` / `latest-sync-revision` parameter. The server also sends his own value of the server-side global revision counter `latest-sync-revision` (which might only be equal (which means both sides are in sync) or higher (which means the client is out-of-sync) than the one sent by the client) in the response.

3) Client retrieves any updated notes from the server

If the list of notes retrieved is non-empty, there a notes on the server which are newer (have a higher `last-sync-revision` on the remote note than the local note). If the client had specified the `include_notes=true` parameter on the first request, it can directly use the notes data retrieved to update its local notes. Else, it must now repeat the first request using the `include_notes=true` parameter, or retrieve each note one-by-one through the /api/1.0/notes/<id> call (not recommended as this produces lots of HTTP requests and thus overhead). The best way is to set `?since=rev&include_notes=true` on the very first request, since all transfered notes are newer and therefore the body is required anyways.

4) The clients sends all notes updates to the server

???



3) Clients



