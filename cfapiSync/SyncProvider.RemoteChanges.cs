using Styletronix.CloudSyncProvider;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Vanara.Extensions;
using Vanara.PInvoke;
using Windows.Storage.Provider;
using static Styletronix.CloudFilterApi;
using static Styletronix.CloudFilterApi.SafeHandlers;
using static Vanara.PInvoke.CldApi;

public partial class SyncProvider : IDisposable
{
    private readonly ActionBlock<FileChangedEventArgs> RemoteChangesQueue;

    private async void ServerProvider_FileChanged(object sender, FileChangedEventArgs e)
    {
        Styletronix.Debug.WriteLine("ServerProvider_FileChanged: (Delay 2000ms) " + e.Placeholder?.RelativeFileName, System.Diagnostics.TraceLevel.Verbose);
        await Task.Delay(2000);
        RemoteChangesQueue.Post(e);
    }

    private async Task ProcessRemoteFileChanged(FileChangedEventArgs e)
    {
        Styletronix.Debug.WriteLine("ProcessRemoteFileChanged: " + e.ChangeType.ToString() + " " + e.Placeholder?.RelativeFileName, System.Diagnostics.TraceLevel.Verbose);

        // TODO: Make use of Placeholder data submitted by ServerProvider.
        try
        {
            if (e.ResyncSubDirectories == true)
            {
                Styletronix.Debug.WriteLine("Full Sync requested by ServerProvider.", System.Diagnostics.TraceLevel.Warning);
                await SyncDataAsync(SyncMode.Full, e.Placeholder.RelativeFileName);
            }
            else
            {
                var localFullPath = GetLocalFullPath(e.Placeholder.RelativeFileName);

                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Deleted:
                        if (e.Placeholder.FileAttributes.HasFlag(FileAttributes.Directory))
                        {
                            await SyncDataAsync(SyncMode.Full, e.Placeholder.RelativeFileName);
                        }
                        else
                        {
                            AddFileToChangeQueue(localFullPath,false);
                        }
                        break;

                    case WatcherChangeTypes.Renamed:
                        var localOldFullPath = GetLocalFullPath(e.OldRelativePath);

                        if (Directory.Exists(localOldFullPath))
                        {
                            Directory.Move(localOldFullPath, localFullPath);
                        }
                        else if (File.Exists(localOldFullPath))
                        {
                            File.Move(localOldFullPath, localFullPath);
                        }
                        else
                        {
                            AddFileToChangeQueue(localFullPath, false);
                        }
                        break;

                    case WatcherChangeTypes.Created:
                        AddFileToChangeQueue(localFullPath, false);
                        break;

                    case WatcherChangeTypes.Changed:
                        AddFileToChangeQueue(localFullPath, false);
                        break;

                    default:
                        AddFileToChangeQueue(GetLocalFullPath(e.Placeholder.RelativeFileName), false);
                        break;

                }
            }
        }
        catch (Exception ex)
        {
            Styletronix.Debug.WriteLine("ProcessRemoteFileChanged Error: " + e.Placeholder?.RelativeFileName + "  " + ex.Message, System.Diagnostics.TraceLevel.Error);
        }
    }

    private void ServerProvider_ServerProviderStateChanged(object sender, ServerProviderStateChangedEventArgs e)
    {
        Styletronix.Debug.WriteLine("ServerProviderStateChanged: " + e.Status.ToString(), System.Diagnostics.TraceLevel.Verbose);
    }
}