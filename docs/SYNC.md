Syncing documentation for Tomboy with remote webservers
=================


About
-----
This document describes the syncing protocoll used by Tomboy in recent versions
and how a remote cloud note storage (like Snowy, Rainy, Ubuntu One) should respond
and interact.

This document describe the Tomboy REST API Version 1.0 only, described at
<https://live.gnome.org/Tomboy/Synchronization/REST/1.0>

Throughout the text, I will refer to the *client* as a means of an instance of
the Note-taking client like Tomboy, or alternative, compatible implementations
like Tomdroid. The *server* is the part of cloud storage responsible for storing
the Notes.

A *note repository* represents all notes and notebooks of a given user. The client
usually only manages one repository, whilst the server has multiple repositories
that it servers to a set of users.

Syncing
-------

All syncing decision is currently done by the client. That means that the client
decides 

* whether a note should updated (retransmitted with updated content) *TO* the server
* whether a note should be updated locally, so downloading *FROM* the server 
  replacing the local note instance 
* whether a note gets deleted (locally and/or remotely on the server)

The server is solely responsible for executing the clients requests (retrieve,
update, delete note), and is  providing secure storage of the notes.

### Syncing data formats

The client as well as the server associate a single global counter variable (of
type signed integer) for each note repository. In Tomboy this is currently the
`last-sync-rev` value stored in the manifest.xml file.

Additionally to the global `last-sync-rev`, the client and the server store a
revision counter (of type signed integer) for *each note in the repository*. In
Tomboy, this per-note counter is currently saved in manifest.xml in the
`latest-revision` field.

* * *
ATTENTION: Due to poor naming convention n the REST/JSON format descritpion, the
revisions fields in the serialized JSON format can easily be confused: Tomboy's
global note repository counter `last-sync-rev` is serialized as
`latest-sync-revision`, while Tomboy's per-note counter `latest-revision` is
serialized as `last-sync-revision` in the per-note JSON.

In the further text, we will refer to the REST/JSON namings of those fields,
*not*  the names of the values used by Tomboy's internal manifest.xml
representation!
* * *

It is noteworthy, that the various date values associated for each note
(create-date, last-change-date, etc.) are not used at all for synchronisation.

### Syncing relationship

A client is associated to exactly one server. That means, that every client at any
given time can at max have a single in-sync note repository instance. To identify
this relationship, the client and the server share a `current-sync-guid`. Whenever
the client adds or switches to a server, the client

* deletes his local `current-sync-guid` and retrieves a new one from the new server 
* resets the global `last-sync-rev` counter as well as the `latest-revision`
  counter for each note by setting those values to -1
* uploads all notes to the server to create a first copy of the local note
  repository

If the client decides to switch back to a server used before the current one, it
MUST NOT reuse the previously used guid for that server, but start over resetting
as described above and perform a full upload of all notes to the server.

### Syncing protocoll

The REST API and datatypes are described in the REST API 1.0 documentation, see
the introduction text in this document. Hence we focus on the logic in the syncing
process. We assume the OAuth process has already taken place and the client starts
to sync the notes.

1) Client asks for any changed notes

At the very beginning, the client sends a HTTP GET request to the notes url, which
is most likely /api/1.0/<username>/notes

Along with the first request, the client sends his global revision counter
`latest-sync-revision` in the JSON *as well as query parameter `since`* in the
query url. (NOTE: Specifying the `since` is redundant but required in the original
REST API; this might change in future API versions)

The client MAY use the `include_notes` parameters to get the full note body in
this request.

2) Server reponds with any changed notes

The server now checks the `since` parameter and responds with a list of notes for
which `last-sync-revision` is *GREATER THAN* the revision specified in the
`since`/`latest-sync-revision` parameter. The server also sends his own value of
the server-side global revision counter `latest-sync-revision` (which might only
be equal (which means both sides are in sync) or higher (which means the client is
 out-of-sync) than the one sent by the client) in the response.

3) Client retrieves any updated notes from the server

If the list of notes retrieved is non-empty, there a notes on the server which are
newer (have a higher `last-sync-revision` on the remote note than the local note).
If the client had specified the `include_notes=true` parameter on the first
request, it can directly use the notes data retrieved to update its local notes.
Else, it must now repeat the first request using the `include_notes=true`
parameter (recommended), or retrieve each note one-by-one through the
/api/1.0/notes/<id> call (not recommended as this produces lots of HTTP requests
and thus overhead).

TODO: Conflict resolution if the local note has changed, but was not synced??

4) The clients sends all notes updates to the server

???



3) Clients



