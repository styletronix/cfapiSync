# cfapiSync
Working c# Example implementig a Cloud Sync Engine on Windows 10 / 11 based on the  cloud files API.

This is very early alpha... And also my very first project written entirely in c#.

It uses the content of folder "d:\temp" as the server source and creates a OneDrive Style sync folder for the current user.

Bi-directional synchronization is supported as long as the Server is able to notify the client of file changes. The ServerProvider is implemented as an Interface to allow easy adaption to other cloud sources.

There are lot things to do, but at least for lokal folders it seems to be fully functional.
