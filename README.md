# cfapiSync
Working c# Example implementig a Cloud Sync Engine on Windows 10 / 11 based on the  cloud files API.

This is very early alpha... And also my very first project written entirely in c#.

It uses the content of folder "d:\temp" as the server source and creates a OneDrive Style sync folder for the current user.

Bi-directional synchronization is supported as long as the Server is able to notify the client of file changes. The ServerProvider is implemented as an Interface to allow easy adaption to other cloud sources.

There are lot things to do, but at least for lokal folders it seems to be fully functional with sometimes an "uupppsss...."

### Supported Sources:
- Local directory
- UNC Network Path which is supported by FileSystemWatcher.

#### Planed:
- WebDav
- Amazon S3

### TODO:
- Failure Handling / Retry.
- Reconnect after Server disconnect.
- Delay after online file changed.
- Revalidate "excluded" status after file move.
- Prevent endless Update if ETag is not reliable.
- Save local / remote Folder
- Add WebDav support
- Revalidate "Delete" Message from ServerProvider.
- Delay "DELETE" Message from ServerProvider.
- ServerProvider reports "DELETED" for localy moved files.
- Add Thumbnail Provider for remote files.
- Global Progressbar within UI.
- FullSync needs to validate pinned files / subfolders.
- FullSync needs to validate local available files in all local available folders.
- FullSync needs to enable "OnDemandPopulation" for all folders which are localy cached.  
- Validate if local "MOVE" triggrs a "DELETE / CREATE" Message from Server.
- .....
