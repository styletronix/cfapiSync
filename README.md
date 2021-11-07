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

### Current issues:
Word and Excel drives me crazy. The way files are handled by office apps are absolutely not easy to handle in sync scenarios in realtime. The chance is high, that local changes to a word file triggers a local delete request while word trnsfers the temp file to the actual existing word document. While the delete request is processed on the server, the server may retrigger the delete to all clients which will result in deletion of the word document on all clients.

Maybe i should in general not handle any delete request in realtime. It may be much saver to add all delete rquests to a queue wich is then checked few minutes later by the full sync algorithm for the given directory. Also it does not seem to be relieble to handle each file request in cloud scenario as a single task... In some situations it seems to be more relieable to revalidate the entire directory where changes occured....  Changes to an entire directory triggers one request to the server with response of the folder content while several files are producing multiple request to the server vor a single directory. So whats better? Muliple smal requests or one request with larger amount of data?


### TODO:
- Ignore changes to files on Server if local file is contained in a not populated folder which is not pinned too.

- Prevent endless Update if ETag is not reliable.
- Save local / remote Folder
- Add WebDav support
- Revalidate "Delete" Message from ServerProvider.
- Delay "DELETE" Message from ServerProvider.
- ServerProvider reports "DELETED" for localy moved files.
- Add Thumbnail Provider for remote files.
- Validate if local "MOVE" triggrs a "DELETE / CREATE" Message from Server.
- .....

### Changes:
#### 11-07.2021
- Failure Handling / Retry.
- Validate Hydration of new files inside pinned folder.
- Reconnect after Server disconnect.

#### 11-03-2021
- Delay after online file changed.
- Revalidate "excluded" status after file move.
- FullSync needs to validate pinned files / subfolders.
- FullSync needs to validate local available files in all local available folders.
- FullSync needs to enable "OnDemandPopulation" for all folders which are localy cached.  


### Notes on FileSystemWatcher
To enable realtime Monitoring of changed files on Server. The ServerProvider for local and UNC Paths is using FileSystemWatcher.
UNC Paths, hostet on Windows can be realtime monitored with FileSystemWatcher. But there are a few exceptions.
If the UNC Path is a DFS-Path, the realtime Monitoring won't work. In this case, set the UNC Path not to DFS-Path, but to the direct Server path.
In other projects, I used functions to get the Server path, based on the DFS-Root to set Monitoring to that location. But this is not available in the current impementation of the ServerProvider.


## Workflow of CfapiSync

### First start
- Registering as a Cloud Provider for a given Folder.
- Convertig all local Files to Hydrated Placeholders.
- Do a bidirectional full sync of existing files.
- Monitoring remote folder using FileSystemWatcher.
- Monitoring local folder using FileSystemWatcher and callbacks which are registered during Cloud Provider registration.

### New local files
- New local files are converted to hydrated placeholders.
- Upload to Server if the same file on Server does not exist.
- If same File on Server exists, the newer file will overwrite the older.
- Then local file will be set to State IN SYNC

### Changes to local files
- State changed to PINNED -> File will be Hydrated with content from Server.
- State changed to UNPINNED -> File will synchronized with server, then dehydrated and set to PIN State UNSPECIFIED.
- Content Changed -> File will be synchronized with server, then set to State IN SYNC

### Changes to remote File
#### If local file is PINNED and server file is newer 
- Local file will be set to PIN State UNSEPECIFIED
- Local files meta data will be updated the content gets dehydrated and the file is marked as IN SYNC.
- Local files PIN State will be changed to PINNED.
- The local file will be Rehydrated.

#### If local file is not Pinned but Hydrated
- Local files meta data will be updated the content gets dehydrated and the fileis marked as IN SYNC.
- At that point, the previously offline available file is not available offline any more.

#### If local file is not Pinned and also Dehydrated
- Local files meta data will be updated and set to IN SYNC

#### If local file does not exist
- Local Placeholder will be created, containing meta data only and marked as IN SYNC.
- If new Placeholder is contained in a PINNED Folder, it will be Hydrated.
- If new file is contained in a localy not populated folder, it will be ignored. Update is done when populating the folder.

#### If local Folder does not contain any pinned or hydrated placeholders
- The containing folder will be marked as PARTIALLY ON DISK and IN SYNC
- During full sync, the folder is ignored to prevent syncing of unnecessary folders and files.
- If folder is opened in any application, it will be repopulated with data from Server and placeholders will be synced.
