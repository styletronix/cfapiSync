# cfapiSync
Working c# Example implementig a Cloud Sync Engine on Windows 10 / 11 based on the  cloud files API.

This is very early alpha... And also my very first project written entirely in c#.

It uses the content of folder "d:\temp" as the server source and creates a OneDrive Style sync folder for the current user.

Bi-directional synchronization is supported as long as the Server is able to notify the client of file changes. The ServerProvider is implemented as an Interface to allow easy adaption to other cloud sources.

There are lot things to do, but at least for lokal folders it seems to be fully functional with sometimes an "uupppsss...."


## Short Explanation of the Components
### SyncProvider
The SyncProvider is the core function which handles all synchronization logic and monitoring of the local files.

### ServerProvider
The ServerProvider uses the IServerProvider Interface to communicate with the SyncProvider. The ServerProvider is ued to connect to the cloud. For each cloud type, a new ServerProvider is required.

#### Available ServerProviders
- Local and UNC Path's supported by FileSystemWatcher.

#### Planned ServerProviders
- Amazon S3
- WebDav


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
- FullSync needs to validate pinned files / subfolders.
- FullSync needs to validate local available files in all local available folders.
- FullSync needs to enable "OnDemandPopulation" for all folders which are localy cached.  
- Validate if local "MOVE" triggrs a "DELETE / CREATE" Message from Server.
- .....


### Notes on FileSystemWatcher
To enable realtime Monitoring of changed files on Server. The ServerProvider for local and UNC Paths is using FileSystemWatcher.
UNC Paths, hostet on Windows can be realtime monitored with FileSystemWatcher. But there are a few exceptions.
If the UNC Path is a DFS-Path, the realtime Monitoring won't work. In this case, set the UNC Path not to DFS-Path, but to the direct Server path.
In other projects, I used functions to get the Server path, based on the DFS-Root to set Monitoring to that location. But this is not available in the current impementation of the ServerProvider.