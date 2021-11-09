using Styletronix.CloudSyncProvider;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public partial class SyncProvider : IDisposable
{
    private readonly ActionBlock<FileChangedEventArgs> RemoteChangesQueue;

    private async void ServerProvider_FileChanged(object sender, FileChangedEventArgs e)
    {
        Styletronix.Debug.WriteLine("ServerProvider_FileChanged: (Delay 2000ms) " + e.Placeholder?.RelativeFileName, System.Diagnostics.TraceLevel.Verbose);
        await Task.Delay(5000);
        RemoteChangesQueue.Post(e);
    }

    private async Task ProcessRemoteFileChanged(FileChangedEventArgs e)
    {
        Styletronix.Debug.WriteLine("ProcessRemoteFileChanged: " + e.ChangeType.ToString() + " " + e.Placeholder?.RelativeFileName, System.Diagnostics.TraceLevel.Verbose);

        // TODO: Make use of Placeholder data submitted by ServerProvider.
        // TODO: Robust file handling.
        try
        {
            if (e.ResyncSubDirectories == true)
            {
                Styletronix.Debug.WriteLine("Full Sync requested by ServerProvider.", System.Diagnostics.TraceLevel.Info);
                await SyncDataAsync(SyncMode.Full, e.Placeholder == null ? "" : e.Placeholder.RelativeFileName);
            }
            else
            {
                string localFullPath = GetLocalFullPath(e.Placeholder.RelativeFileName);

                AddFileToRemoteChangeQueue(localFullPath, false);

                //switch (e.ChangeType)
                //{
                //    case WatcherChangeTypes.Deleted:
                //        if (e.Placeholder.FileAttributes.HasFlag(FileAttributes.Directory))
                //        {
                //            await SyncDataAsync(SyncMode.Full, e.Placeholder.RelativeFileName);
                //        }
                //        else
                //        {
                //            AddFileToLocalChangeQueue(localFullPath, false);
                //        }
                //        break;

                //    case WatcherChangeTypes.Renamed:
                //        string localOldFullPath = GetLocalFullPath(e.OldRelativePath);

                //        if (Directory.Exists(localOldFullPath))
                //        {
                //            Directory.Move(localOldFullPath, localFullPath);
                //        }
                //        else if (File.Exists(localOldFullPath))
                //        {
                //            File.Move(localOldFullPath, localFullPath);
                //        }
                //        else
                //        {
                //            AddFileToLocalChangeQueue(localFullPath, false);
                //        }
                //        break;

                //    case WatcherChangeTypes.Created:
                //        AddFileToLocalChangeQueue(localFullPath, false);
                //        break;

                //    case WatcherChangeTypes.Changed:
                //        AddFileToLocalChangeQueue(localFullPath, false);
                //        break;

                //    default:
                //        AddFileToLocalChangeQueue(GetLocalFullPath(e.Placeholder.RelativeFileName), false);
                //        break;

                //}
            }
        }
        catch (Exception ex)
        {
            Styletronix.Debug.WriteLine("ProcessRemoteFileChanged Error: " + e.Placeholder?.RelativeFileName + "  " + ex.Message, System.Diagnostics.TraceLevel.Error);
        }
    }

    private void ServerProvider_ServerProviderStateChanged(object sender, ServerProviderStateChangedEventArgs e)
    {
        if (e.Status == ServerProviderStatus.Connected)
        {
            LocalSyncTimer.Change( LocalSyncTimerInterval, LocalSyncTimerInterval);
            FailedQueueTimer.Change(FailedQueueTimerInterval, FailedQueueTimerInterval);
        }
        else
        {
            LocalSyncTimer.Change( Timeout.Infinite, Timeout.Infinite);
            FailedQueueTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        Styletronix.Debug.WriteLine("ServerProviderStateChanged: " + e.Status.ToString(), System.Diagnostics.TraceLevel.Verbose);
    }


    private void AddFileToRemoteChangeQueue(string fullPath, bool ignoreLock)
    {
        if (MaintenanceInProgress)
            return;

        if (IsExcludedFile(fullPath))
            return;

        RemoteChangedDataQueue.TryAdd(fullPath, ignoreLock);
    }
}