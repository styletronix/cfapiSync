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

    private void ServerProvider_FileChanged(object sender, FileChangedEventArgs e)
    {
        //Styletronix.Debug.WriteLine("ServerProviderFileChanged: " + e.Placeholder?.RelativeFileName, System.Diagnostics.TraceLevel.Verbose);
        RemoteChangesQueue.Post(e);
    }

    private async Task ProcessRemoteFileChanged(FileChangedEventArgs e)
    {
        Styletronix.Debug.WriteLine("ServerProviderFileChanged: " + e.ChangeType.ToString() + " " + e.Placeholder?.RelativeFileName, System.Diagnostics.TraceLevel.Verbose);
        // TODO: Make use of Placeholder data submitted by ServerProvider.
        try
        {
            if (e.ResyncSubDirectories == true)
            {
                await SyncDataAsync(SyncMode.Full, e.Placeholder.RelativeFileName);
            }
            else
            {
                var localFullPath = GetLocalFullPath(e.Placeholder.RelativeFileName);

                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Deleted:
                        await ChangedDataQueueBlock.SendAsync(localFullPath);

                        break;
                    case WatcherChangeTypes.Renamed:
                        var localOldFullPath = GetLocalFullPath(e.OldRelativePath);
                        //var localPlaceholder = new ExtendedPlaceholderState(localOldFullPath);
                        //var inSync = (localPlaceholder.PlaceholderInfoBasic.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC && localPlaceholder.ETag == e.Placeholder.ETag);


                    
                        if (Directory.Exists(localOldFullPath))
                        {
                            Directory.Move(localOldFullPath, localFullPath);
                            //if (inSync) 
                            //    localPlaceholder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC);
                        }
                        else if (File.Exists(localOldFullPath))
                        {
                            File.Move(localOldFullPath, localFullPath);
                            //if (inSync) 
                            //    localPlaceholder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC);
                        }
                        else
                        {
                            await ChangedDataQueueBlock.SendAsync(localFullPath);
                        }

                        break;
                    case WatcherChangeTypes.Created:
                        await ChangedDataQueueBlock.SendAsync(localFullPath);

                        break;
                    case WatcherChangeTypes.Changed:
                        await ChangedDataQueueBlock.SendAsync(localFullPath);

                        break;
                    default:
                        await ChangedDataQueueBlock.SendAsync(GetLocalFullPath(e.Placeholder.RelativeFileName));

                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Styletronix.Debug.WriteLine("ServerProviderFileChanged Error: " + e.Placeholder?.RelativeFileName + "  " + ex.Message, System.Diagnostics.TraceLevel.Error);
        }
    }

    private void ServerProvider_ServerProviderStateChanged(object sender, ServerProviderStateChangedEventArgs e)
    {
        Styletronix.Debug.WriteLine("ServerProviderStateChanged: " + e.Status.ToString(), System.Diagnostics.TraceLevel.Verbose);
    }
}