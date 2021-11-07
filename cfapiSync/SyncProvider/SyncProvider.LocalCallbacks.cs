using Styletronix.CloudSyncProvider;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Vanara.PInvoke;
using static Styletronix.CloudFilterApi;
using static Styletronix.CloudFilterApi.SafeHandlers;
using static Vanara.PInvoke.CldApi;

public partial class SyncProvider
{
    private CF_CALLBACK_REGISTRATION[] _callbackMappings;
    private FileSystemWatcher watcher;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, DataActions> FetchDataRunningQueue;
    private readonly Task FetchDataWorkerThread;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, CancellationTokenSource> FetchPlaceholdersCancellationTokens = new();
    private readonly ActionBlock<DeleteAction> DeleteQueue;
    private readonly FileRangeManager fileRangeManager = new();
    public event EventHandler<FileProgressEventArgs> FileProgressEvent;

    private readonly string[] ExcludedProcessesForFetchPlaceholders = new string[] {
        @".*\\SearchProtocolHost\.exe.*", // This process tries to index folders which are just a few seconds before marked as "ENABLE_ON_DEMAND_POPULATION" which results in unwanted repopulation.
        @".*\\svchost\.exe.*StorSvc" // This process cleans old data. Fetching of placeholders is not required for this process
    };

    public void FETCH_PLACEHOLDERS(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        var opInfo = CreateOPERATION_INFO(CallbackInfo, CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_PLACEHOLDERS);

        bool cancelFetch = SyncContext.ServerProvider.Status != ServerProviderStatus.Connected;

        if (!cancelFetch)
        {
            // Get Process info to
            CF_PROCESS_INFO processInfo = Marshal.PtrToStructure<CF_PROCESS_INFO>(CallbackInfo.ProcessInfo);
            foreach (string process in ExcludedProcessesForFetchPlaceholders)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(processInfo.CommandLine, process))
                {
                    //Styletronix.Debug.WriteLine("FETCH_PLACEHOLDERS Triggered by excluded App: " + processInfo.ImagePath, System.Diagnostics.TraceLevel.Info);
                    cancelFetch = true;
                    break;
                }
            }
        }

        if (cancelFetch)
        {
            CF_OPERATION_PARAMETERS.TRANSFERPLACEHOLDERS TpParam = new()
            {
                PlaceholderArray = IntPtr.Zero,
                Flags = CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_NONE,
                PlaceholderCount = 0,
                PlaceholderTotalCount = 0,
                CompletionStatus = new NTStatus((uint)NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE)
            };
            CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(TpParam);
            HRESULT executeResult = CfExecute(opInfo, ref opParams);


            Styletronix.Debug.LogResponse(executeResult);
            return;
        }


        string relativePath = GetRelativePath(CallbackInfo);
        string fullPath = GetLocalFullPath(CallbackInfo);

        CancellationTokenSource ctx = new();

        FetchPlaceholdersCancellationTokens.AddOrUpdate(relativePath, ctx, (k, v) =>
        {
            v?.Cancel();
            return ctx;
        });

        FETCH_PLACEHOLDERS_Internal(relativePath, opInfo, CallbackParameters.FetchPlaceholders.Pattern, ctx.Token);
    }
    public void CANCEL_FETCH_PLACEHOLDERS(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Styletronix.Debug.WriteLine("Cancel Fetch Placeholders " + CallbackInfo.NormalizedPath, System.Diagnostics.TraceLevel.Info);

        if (FetchPlaceholdersCancellationTokens.TryRemove(GetRelativePath(CallbackInfo), out CancellationTokenSource ctx))
        {
            ctx.Cancel();
            Styletronix.Debug.WriteLine("Fetch Placeholder Cancelled" + CallbackInfo.NormalizedPath, System.Diagnostics.TraceLevel.Verbose);
        }
    }
    public void FETCH_DATA(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        bool cancelFetch = SyncContext.ServerProvider.Status != ServerProviderStatus.Connected;

        if (cancelFetch)
        {
            Styletronix.Debug.WriteLine(@"FETCH_DATA Cancelling due to disconnected Server Provider", System.Diagnostics.TraceLevel.Info);

            var opInfo = CreateOPERATION_INFO(CallbackInfo, CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_DATA);
            CF_OPERATION_PARAMETERS.TRANSFERDATA TpParam = new()
            {
                Length = CallbackParameters.FetchData.RequiredLength,
                Offset = CallbackParameters.FetchData.RequiredFileOffset,
                Buffer = IntPtr.Zero,
                Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE,
                CompletionStatus = new NTStatus((uint)NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE)
            };
            CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(TpParam);
            Styletronix.Debug.LogResponse(CfExecute(opInfo, ref opParams));
            return;
        }


        Styletronix.Debug.WriteLine(@"FETCH_DATA: Priority " + CallbackInfo.PriorityHint +
            @" / R " + CallbackParameters.FetchData.RequiredFileOffset + @" - " + CallbackParameters.FetchData.RequiredLength +
            @" / O " + CallbackParameters.FetchData.OptionalFileOffset + @" - " + CallbackParameters.FetchData.OptionalLength +
            @" / " + CallbackInfo.NormalizedPath, System.Diagnostics.TraceLevel.Info);


        long length = CallbackParameters.FetchData.RequiredLength;
        long offset = CallbackParameters.FetchData.RequiredFileOffset;
        int currentChunkSize = GetChunkSize();

        if ((offset + length) == CallbackParameters.FetchData.OptionalFileOffset)
        {
            if (length < currentChunkSize)
            {
                length = Math.Min(currentChunkSize, CallbackParameters.FetchData.OptionalLength + length);
            }
        }

        DataActions data = new()
        {
            FileOffset = offset,
            Length = length,
            NormalizedPath = CallbackInfo.NormalizedPath,
            PriorityHint = CallbackInfo.PriorityHint,
            TransferKey = CallbackInfo.TransferKey,
            Id = CallbackInfo.NormalizedPath + "!" + CallbackParameters.FetchData.RequiredFileOffset + "!" + CallbackParameters.FetchData.RequiredLength
        };

        fileRangeManager.Add(data);
    }
    public void CANCEL_FETCH_DATA(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Styletronix.Debug.WriteLine(@"CANCEL_FETCH_DATA: " +
            @" / " + CallbackParameters.Cancel.FetchData.FileOffset + @" - " + CallbackParameters.Cancel.FetchData.Length +
            @" / " + CallbackInfo.NormalizedPath, System.Diagnostics.TraceLevel.Verbose);

        fileRangeManager.Cancel(new DataActions
        {
            FileOffset = CallbackParameters.Cancel.FetchData.FileOffset,
            Length = CallbackParameters.Cancel.FetchData.Length,
            NormalizedPath = CallbackInfo.NormalizedPath,
            TransferKey = CallbackInfo.TransferKey,
            Id = CallbackInfo.NormalizedPath + "!" + CallbackParameters.Cancel.FetchData.FileOffset + "!" + CallbackParameters.Cancel.FetchData.Length
        });
    }
    public void NOTIFY_FILE_OPEN_COMPLETION(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        //Styletronix.Debug.WriteLine("NOTIFY_FILE_OPEN_COMPLETION: " + CallbackInfo.NormalizedPath);
    }
    public void NOTIFY_FILE_CLOSE_COMPLETION(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Styletronix.Debug.WriteLine("NOTIFY_FILE_CLOSE_COMPLETION: " + CallbackInfo.NormalizedPath);
    }
    public void NOTIFY_DELETE(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        if (MaintenanceInProgress) return;

        DeleteQueue.Post(new DeleteAction()
        {
            OpInfo = CreateOPERATION_INFO(CallbackInfo, CF_OPERATION_TYPE.CF_OPERATION_TYPE_ACK_DELETE),
            IsDirectory = CallbackParameters.Delete.Flags.HasFlag(CF_CALLBACK_DELETE_FLAGS.CF_CALLBACK_DELETE_FLAG_IS_DIRECTORY),
            RelativePath = GetRelativePath(CallbackInfo)
        });
    }
    public void NOTIFY_DELETE_COMPLETION(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        // Styletronix.Debug.WriteLine("NOTIFY_DELETE_COMPLETION: " + CallbackInfo.NormalizedPath);
    }
    public void NOTIFY_RENAME(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        if (MaintenanceInProgress)
        {
            return;
        }

        Styletronix.Debug.WriteLine("NOTIFY_RENAME: " + CallbackInfo.NormalizedPath + " -> " + CallbackParameters.Rename.TargetPath, System.Diagnostics.TraceLevel.Info);

        CF_OPERATION_INFO opInfo = CreateOPERATION_INFO(CallbackInfo, CF_OPERATION_TYPE.CF_OPERATION_TYPE_ACK_RENAME);

        NOTIFY_RENAME_Internal(GetRelativePath(CallbackInfo), GetRelativePath(CallbackParameters.Rename),
            CallbackParameters.Rename.Flags.HasFlag(CF_CALLBACK_RENAME_FLAGS.CF_CALLBACK_RENAME_FLAG_IS_DIRECTORY), opInfo);

    }
    public void NOTIFY_RENAME_COMPLETION(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Styletronix.Debug.WriteLine("NOTIFY_RENAME_COMPLETION: " + CallbackParameters.RenameCompletion.SourcePath + " -> " + CallbackInfo.NormalizedPath, System.Diagnostics.TraceLevel.Verbose);
    }
    private async void FileSystemWatcher_OnError(object sender, ErrorEventArgs e)
    {
        Styletronix.Debug.WriteLine("FileSystemWatcher Error: " + e.GetException().Message, System.Diagnostics.TraceLevel.Warning);

        await Task.Delay(2000, GlobalShutDownToken).ConfigureAwait(false);
        _ = SyncDataAsync(SyncMode.Local, GlobalShutDownToken).ConfigureAwait(false);
    }
    private void FileSystemWatcher_OnChanged(object sender, FileSystemEventArgs e)
    {
        if (IsExcludedFile(e.FullPath))
            return;

        if (SyncContext.ServerProvider.PreferredServerProviderSettings.PreferFullDirSync)
        {
            if(SyncContext.ServerProvider.Status == ServerProviderStatus.Connected)
                LocalSyncTimer.Change(TimeSpan.FromSeconds(5), LocalSyncTimerInterval);
        }
        else
        {
            AddFileToChangeQueue(e.FullPath, false);
        }
    }


    private void AddFileToChangeQueue(string fullPath, bool ignoreLock)
    {
        if (MaintenanceInProgress)
            return;

        ChangedDataQueue.TryAdd(fullPath, ignoreLock);
    }

    public async void FETCH_PLACEHOLDERS_Internal(string relativePath, CF_OPERATION_INFO opInfo, string pattern, CancellationToken cancellationToken)
    {
        Styletronix.Debug.WriteLine("Fetch Placeholder: " + relativePath, System.Diagnostics.TraceLevel.Info);

        string fullPath = GetLocalFullPath(relativePath);

        //TODO: Transfer Placeholders in Chunks for large directories.
        //Question: What is considere a large directory? > 1000 entries? Let ServerProvider decide
        // - Get partial file list of large Directory.
        // - Add to partial and full list.
        // - Send partial list to  CfExecute.
        // - Clear partial list.
        // - Get next partial file list
        // - Repeat until completed.
        // - Compare full list after last CFExecute.
        // - Use pattern

        try
        {
            using SafePlaceHolderList infos = new();
            NtStatus completionStatus = NtStatus.STATUS_SUCCESS;

            // Get Filelist from Server
            GenericResult<List<Placeholder>> getServerFileListResult = await GetServerFileListAsync(relativePath, cancellationToken);
            if (!getServerFileListResult.Succeeded)
            {
                completionStatus = getServerFileListResult.Status;
            }
            else
            {
                // Create CreatePlaceholderInfo for each Cloud File
                foreach (Placeholder placeholder in getServerFileListResult.Data)
                {
                    infos.Add(Styletronix.CloudFilterApi.CreatePlaceholderInfo(placeholder, Guid.NewGuid().ToString()));
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                };
            }


            // Directorys which do not exist on Server should not throw any exception.
            if (completionStatus == NtStatus.STATUS_NOT_A_CLOUD_FILE)
            {

                completionStatus = NtStatus.STATUS_SUCCESS;
            }


            using DisposableObject<string> lockItem = ChangedDataQueue.LockItem(fullPath);



            uint total = (uint)infos.Count;
            CF_OPERATION_PARAMETERS.TRANSFERPLACEHOLDERS TpParam = new()
            {
                PlaceholderArray = infos,
                Flags = CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_DISABLE_ON_DEMAND_POPULATION,
                PlaceholderCount = total,
                PlaceholderTotalCount = total,
                CompletionStatus = new NTStatus((uint)completionStatus)
            };
            CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(TpParam);
            HRESULT executeResult = CfExecute(opInfo, ref opParams);
            Styletronix.Debug.LogResponse(executeResult);

            FetchPlaceholdersCancellationTokens.TryRemove(relativePath, out CancellationTokenSource _);

            if (completionStatus != NtStatus.STATUS_SUCCESS || !executeResult.Succeeded)
            {
                return;
            }


            // Validate local Placeholders. CfExecute only adds missing entries, but does not check existing data.

            // Get Local FileList
            List<Placeholder> localPlaceholders = GetLocalFileList(SyncContext.LocalRootFolder + "\\" + relativePath, cancellationToken);

            foreach (Placeholder remotePlaceholder in getServerFileListResult.Data)
            {
                Placeholder localPlaceholder = (from a in localPlaceholders where string.Equals(a.RelativeFileName, remotePlaceholder.RelativeFileName, StringComparison.CurrentCultureIgnoreCase) select a).FirstOrDefault();

                if (remotePlaceholder.FileAttributes.HasFlag(FileAttributes.Directory) == false && remotePlaceholder.ETag != localPlaceholder?.ETag)
                {
                    AddFileToChangeQueue(GetLocalFullPath(remotePlaceholder.RelativeFileName), true);
                }
            }

            foreach (Placeholder item in localPlaceholders)
            {
                if (!(from a in getServerFileListResult.Data where string.Equals(a.RelativeFileName, item.RelativeFileName, StringComparison.CurrentCultureIgnoreCase) select a).Any())
                {
                    AddFileToChangeQueue(GetLocalFullPath(item.RelativeFileName), true);
                }
            }

            //SetInSyncState(SyncContext.LocalRootFolder + "\\" + relativePath, CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC, true);
        }
        finally
        {
            //suspendLocalFileChangeHandling.EndSuspension();
        }
    }


    public async void NOTIFY_RENAME_Internal(string RelativeFileName, string RelativeFileNameDestination, bool isDirectory, CF_OPERATION_INFO opInfo)
    {
        if (MaintenanceInProgress) return;

        using var lockItem = this.ChangedDataQueue.LockItem(GetLocalFullPath(RelativeFileName));

        NTStatus status;

        if (!IsExcludedFile(RelativeFileName) && !IsExcludedFile(RelativeFileNameDestination))
        {
            MoveFileResult result = await SyncContext.ServerProvider.MoveFileAsync(RelativeFileName, RelativeFileNameDestination, isDirectory);
            if (result.Succeeded)
            {
                status = NTStatus.STATUS_SUCCESS;
            }
            else
            {
                status = NTStatus.STATUS_ACCESS_DENIED;
            }
        }
        else
        {
            status = NTStatus.STATUS_SUCCESS;
        }


        CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(new CF_OPERATION_PARAMETERS.ACKRENAME
        {
            Flags = CF_OPERATION_ACK_RENAME_FLAGS.CF_OPERATION_ACK_RENAME_FLAG_NONE,
            CompletionStatus = status
        });

        Styletronix.Debug.LogResponse(CfExecute(opInfo, ref opParams));
    }
    private async Task NOTIFY_DELETE_Action(DeleteAction dat)
    {
        if (MaintenanceInProgress) return;

        NTStatus status;

        if (SyncContext.ServerProvider.Status != ServerProviderStatus.Connected)
        {
            CF_OPERATION_PARAMETERS opParams1 = CF_OPERATION_PARAMETERS.Create(new CF_OPERATION_PARAMETERS.ACKDELETE
            {
                Flags = CF_OPERATION_ACK_DELETE_FLAGS.CF_OPERATION_ACK_DELETE_FLAG_NONE,
                CompletionStatus = new NTStatus((uint)NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE)
            });
            Styletronix.Debug.LogResponse(CfExecute(dat.OpInfo, ref opParams1));
            return;
        }

        string fullPath = SyncContext.LocalRootFolder + "\\" + dat.RelativePath;
        using var lockFile = this.ChangedDataQueue.LockItem(fullPath);

        if (IsExcludedFile(dat.RelativePath))
        {
            status = NTStatus.STATUS_SUCCESS;
            goto skip;
        }

        if (!(dat.IsDirectory ? Directory.Exists(fullPath) : File.Exists(fullPath)))
        {
            status = NTStatus.STATUS_SUCCESS;
            goto skip;
        }

        ExtendedPlaceholderState pl = new(fullPath);
        if (pl.PlaceholderInfoStandard.PinState == CF_PIN_STATE.CF_PIN_STATE_EXCLUDED)
        {
            status = NTStatus.STATUS_SUCCESS;
            goto skip;
        }

        DeleteFileResult result = await SyncContext.ServerProvider.DeleteFileAsync(dat.RelativePath, dat.IsDirectory);
        if (result.Succeeded)
        {
            Styletronix.Debug.WriteLine("Deleted on Server: " + dat.RelativePath, System.Diagnostics.TraceLevel.Verbose);
        }
        else
        {
            Styletronix.Debug.WriteLine("Delete on Server FAILED " + result.Status.ToString() + ": " + dat.RelativePath, System.Diagnostics.TraceLevel.Warning); ;
        }
        status = new NTStatus((uint)result.Status);


    skip:
        CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(new CF_OPERATION_PARAMETERS.ACKDELETE
        {
            Flags = CF_OPERATION_ACK_DELETE_FLAGS.CF_OPERATION_ACK_DELETE_FLAG_NONE,
            CompletionStatus = status
        });

        Styletronix.Debug.LogResponse(CfExecute(dat.OpInfo, ref opParams));
    }


    private Task FetchDataWorker()
    {
        return Task.Factory.StartNew(async () =>
        {
            while (!fileRangeManager.CancellationToken.IsCancellationRequested)
            {
                FetchRange item = await fileRangeManager.WaitTakeNextAsync().ConfigureAwait(false);
                if (fileRangeManager.CancellationToken.IsCancellationRequested) { break; }
                if (item != null)
                {
                    try
                    {
                        await FetchDataAsync(item).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Styletronix.Debug.WriteLine(ex.ToString(), System.Diagnostics.TraceLevel.Error);
                    }
                }
            }
        }, fileRangeManager.CancellationToken, TaskCreationOptions.LongRunning |
        TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Default).Unwrap();
    }

    private async Task FetchDataAsync(FetchRange data)
    {
        string relativePath = data.NormalizedPath.Remove(0, SyncContext.LocalRootFolderNormalized.Length).TrimStart(char.Parse("\\"));
        string targetFullPath = Path.Combine(SyncContext.LocalRootFolder, relativePath);
        int currentChunkSize = GetChunkSize();
        NTStatus CompletionStatus = NTStatus.STATUS_SUCCESS;

        Styletronix.Debug.WriteLine("Fetch DataRange " + data.RangeStart + @" - " + data.RangeEnd + @" / " + relativePath, System.Diagnostics.TraceLevel.Info);

        try
        {
            CancellationToken ctx = new CancellationTokenSource().Token; // data.CancellationTokenSource.Token;
            if (ctx.IsCancellationRequested) { return; }

            CF_OPERATION_INFO opInfo = new()
            {
                Type = CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_DATA,
                ConnectionKey = SyncContext.ConnectionKey,
                TransferKey = data.TransferKey,
                RequestKey = new CF_REQUEST_KEY()
            };
            opInfo.StructSize = (uint)Marshal.SizeOf(opInfo);

            if (IsExcludedFile(targetFullPath))
            {
                CF_OPERATION_PARAMETERS.TRANSFERDATA TpParam = new()
                {
                    Length = 1, // Length has to be greater than 0 even if transfer failed or CfExecute fails....
                    Offset = data.RangeStart,
                    Buffer = IntPtr.Zero,
                    Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE,
                    CompletionStatus = new NTStatus((uint)NtStatus.STATUS_NOT_A_CLOUD_FILE)
                };
                CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(TpParam);
                Styletronix.Debug.LogResponse(CfExecute(opInfo, ref opParams));
                fileRangeManager.Cancel(data.NormalizedPath);
                return;
            }


            //Placeholder localSimplePlaceholder = null;
            ExtendedPlaceholderState localPlaceholder = null;
            try
            {
                localPlaceholder = new(targetFullPath);

                using IReadFileAsync fetchFile = SyncContext.ServerProvider.GetNewReadFile();
                ReadFileOpenResult openAsyncResult = await fetchFile.OpenAsync(new OpenAsyncParams()
                {
                    RelativeFileName = relativePath,
                    CancellationToken = ctx,
                    ETag = localPlaceholder?.ETag
                });

                CompletionStatus = new NTStatus((uint)openAsyncResult.Status);
                //using ExtendedPlaceholderState localPlaceholder = new(targetFullPath);

                // Compare ETag to verify Sync of cloud and local file
                if (CompletionStatus == NTStatus.STATUS_SUCCESS)
                {
                    if (openAsyncResult.Placeholder?.ETag != localPlaceholder.ETag)
                    {
                        Styletronix.Debug.WriteLine("ETag Validation FAILED: " + relativePath, System.Diagnostics.TraceLevel.Info);
                        CompletionStatus = new NTStatus((uint)Styletronix.CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_NOT_IN_SYNC);
                        openAsyncResult.Message = Styletronix.CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_NOT_IN_SYNC.ToString();
                    }
                }

                if (CompletionStatus != NTStatus.STATUS_SUCCESS)
                {
                    Styletronix.Debug.WriteLine("Warning: " + openAsyncResult.Message, System.Diagnostics.TraceLevel.Info);

                    CF_OPERATION_PARAMETERS.TRANSFERDATA TpParam = new()
                    {
                        Length = 1, // Length has to be greater than 0 even if transfer failed....
                        Offset = data.RangeStart,
                        Buffer = IntPtr.Zero,
                        Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE,
                        CompletionStatus = CompletionStatus
                    };
                    CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(TpParam);
                    Styletronix.Debug.LogResponse(CfExecute(opInfo, ref opParams));

                    fileRangeManager.Cancel(data.NormalizedPath);

                    localPlaceholder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC);
                    return;
                }

                byte[] stackBuffer = new byte[stackSize];
                byte[] buffer = new byte[currentChunkSize];

                long minRangeStart = long.MaxValue;
                long totalRead = 0;

                while (data != null)
                {
                    minRangeStart = Math.Min(minRangeStart, data.RangeStart);
                    long currentRangeStart = data.RangeStart;
                    long currentRangeEnd = data.RangeEnd;

                    long currentOffset = currentRangeStart;
                    long totalLength = currentRangeEnd - currentRangeStart;

                    int readLength = (int)Math.Min(currentRangeEnd - currentOffset, currentChunkSize);

                    if (readLength > 0 && ctx.IsCancellationRequested == false)
                    {
                        ReadFileReadResult readResult = await fetchFile.ReadAsync(buffer, 0, currentOffset, readLength);
                        if (!readResult.Succeeded)
                        {
                            Styletronix.Debug.WriteLine("Error: " + readResult.Message, System.Diagnostics.TraceLevel.Error);

                            CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(new CF_OPERATION_PARAMETERS.TRANSFERDATA
                            {
                                Length = 1, // Length has to be greater than 0 even if transfer failed....
                                Offset = data.RangeStart,
                                Buffer = IntPtr.Zero,
                                Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE,
                                CompletionStatus = new NTStatus((uint)readResult.Status)
                            });
                            Styletronix.Debug.LogResponse(CfExecute(opInfo, ref opParams));

                            fileRangeManager.Cancel(data.NormalizedPath);
                            return;
                        }
                        int dataRead = readResult.BytesRead;

                        if (data.RangeEnd == 0 || data.RangeEnd < currentOffset || data.RangeStart > currentOffset) { continue; }

                        totalRead += dataRead;
                        ReportProviderProgress(data.TransferKey, currentRangeEnd - minRangeStart, totalRead, relativePath);

                        if (dataRead < readLength && CompletionStatus == NTStatus.STATUS_SUCCESS)
                        {
                            CompletionStatus = NTStatus.STATUS_END_OF_FILE;
                        }

                        unsafe
                        {
                            fixed (byte* StackBuffer = stackBuffer)
                            {
                                int stackTransfered = 0;
                                while (stackTransfered < dataRead)
                                {
                                    if (ctx.IsCancellationRequested) { return; }

                                    int realStackSize = Math.Min(stackSize, dataRead - stackTransfered);

                                    Marshal.Copy(buffer, stackTransfered, (IntPtr)StackBuffer, realStackSize);

                                    CF_OPERATION_PARAMETERS.TRANSFERDATA TpParam = new()
                                    {
                                        Length = realStackSize,
                                        Offset = currentOffset + stackTransfered,
                                        Buffer = (IntPtr)StackBuffer,
                                        Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE,
                                        CompletionStatus = CompletionStatus
                                    };
                                    CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(TpParam);

                                    HRESULT ret = CfExecute(opInfo, ref opParams);
                                    if (ret.Succeeded == false)
                                    {
                                        Styletronix.Debug.WriteLine(ret.ToString(), System.Diagnostics.TraceLevel.Error);
                                    }
                                    //ret.ThrowIfFailed();

                                    stackTransfered += realStackSize;
                                }
                            }
                        }

                        fileRangeManager.RemoveRange(data.NormalizedPath, currentRangeStart, currentRangeStart + dataRead);
                    }

                    data = fileRangeManager.TakeNext(data.NormalizedPath);
                }

                await fetchFile.CloseAsync();
            }
            finally
            {
                localPlaceholder?.Dispose();
            }


            if (ctx.IsCancellationRequested)
            {
                Styletronix.Debug.WriteLine("FETCH_DATA CANCELED", System.Diagnostics.TraceLevel.Info);
            }
            else
            {
                Styletronix.Debug.WriteLine("FETCH_DATA Completed", System.Diagnostics.TraceLevel.Verbose);
            }
        }
        catch (Exception ex)
        {
            Styletronix.Debug.WriteLine("FETCH_DATA FAILED " + ex.ToString(), System.Diagnostics.TraceLevel.Error);
            fileRangeManager.Cancel(data.NormalizedPath);
        }
    }

    private class DeleteAction
    {
        public CF_OPERATION_INFO OpInfo;
        public string RelativePath;
        public bool IsDirectory;
    }

    private class FileRangeManager : IDisposable
    {
        private readonly object lockObject = new();
        private readonly List<FetchRange> filesToProcess = new();
        private readonly AutoResetEventAsync autoResetEventAsync = new();
        private readonly CancellationTokenSource cancellationToken = new();

        public CancellationToken CancellationToken => cancellationToken.Token;
        public async Task<FetchRange> WaitTakeNextAsync()
        {
            FetchRange x = TakeNext();

            if (x == null)
            {
                await autoResetEventAsync.WaitAsync(CancellationToken).ConfigureAwait(false);
                lock (lockObject)
                {
                    FetchRange t = TakeNext();
                    return t;
                }
            }
            else
            {
                return x;
            }
        }
        public FetchRange TakeNext()
        {
            FetchRange ret = null;

            lock (lockObject)
            {
                ret = filesToProcess.OrderByDescending(a => a.PriorityHint)
                    .OrderBy(a => a.RangeStart).FirstOrDefault();

            }

            return ret;
        }
        public FetchRange TakeNext(string normalizedPath)
        {
            FetchRange ret = null;

            lock (lockObject)
            {
                ret = filesToProcess.Where(a => a.NormalizedPath == normalizedPath)
                    .OrderBy(a => a.RangeStart).FirstOrDefault();
            }

            return ret;
        }

        public void Add(DataActions data)
        {
            lock (lockObject)
            {
                filesToProcess.Add(new FetchRange(data));
                Combine(data.NormalizedPath);
            }

            autoResetEventAsync.Set();
        }
        private void Combine(string normalizedPath)
        {
            bool exitLoop = false;
            while (!exitLoop)
            {
                exitLoop = true;

                foreach (FetchRange item2 in filesToProcess.Where(a => a.NormalizedPath == normalizedPath).OrderBy(a => a.RangeStart))
                {
                    FetchRange item3 = filesToProcess.Where(a => a.NormalizedPath == normalizedPath && a.RangeStart <= item2.RangeEnd && a.RangeEnd >= item2.RangeStart && a != item2).OrderBy(a => a.RangeStart).FirstOrDefault();
                    if (item3 != null)
                    {
                        item2.RangeStart = Math.Min(item2.RangeStart, item3.RangeStart);
                        item2.RangeEnd = Math.Min(item2.RangeEnd, item3.RangeEnd);
                        filesToProcess.Remove(item3);

                        exitLoop = false;
                        break;
                    }
                }
            }
        }
        public void Cancel(DataActions data)
        {
            List<FetchRange> removeItems = new();
            List<FetchRange> addItems = new();

            lock (lockObject)
            {
                long rangeStart = data.FileOffset;
                long rangeEnd = data.FileOffset + data.Length;

                foreach (FetchRange item in filesToProcess.Where(a => a.NormalizedPath == data.NormalizedPath && a.RangeStart <= rangeEnd && a.RangeEnd >= rangeStart).OrderBy(a => a.RangeStart))
                {
                    if (item.RangeStart >= rangeStart && item.RangeEnd <= rangeEnd)
                    {
                        item.RangeStart = 0;
                        item.RangeEnd = 0;
                        removeItems.Add(item);
                        continue;
                    }

                    if (item.RangeStart >= rangeStart && item.RangeStart < rangeEnd)
                    {
                        item.RangeStart = rangeEnd;
                    }

                    if (item.RangeEnd < rangeEnd && item.RangeEnd >= rangeStart)
                    {
                        item.RangeEnd = rangeStart;
                    }

                    if (item.RangeStart < rangeStart && item.RangeEnd > rangeEnd)
                    {
                        FetchRange newItem = new()
                        {
                            NormalizedPath = item.NormalizedPath,
                            PriorityHint = item.PriorityHint,
                            TransferKey = item.TransferKey,
                            RangeStart = rangeEnd + 1,
                            RangeEnd = item.RangeEnd
                        };

                        item.RangeEnd = rangeStart;

                        addItems.Add(newItem);
                    }

                    if (item.RangeEnd <= item.RangeStart)
                    {
                        removeItems.Add(item);
                    }
                    if (item.RangeStart < 0)
                    {
                        throw new ArgumentOutOfRangeException("RangeStart < 0");
                    }
                }

                foreach (FetchRange item in removeItems)
                {
                    filesToProcess.Remove(item);
                }
                foreach (FetchRange item in addItems)
                {
                    filesToProcess.Add(item);
                }

                Combine(data.NormalizedPath);
            }
        }
        public void RemoveRange(string normalizedPath, long rangeStart, long rangeEnd)
        {
            Cancel(new DataActions()
            {
                NormalizedPath = normalizedPath,
                FileOffset = rangeStart,
                Length = rangeEnd - rangeStart
            });
        }
        public void Cancel(string normalizedPath)
        {
            RemoveRange(normalizedPath, 0, long.MaxValue);
        }



        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cancellationToken.Cancel();
                }

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Used to Queue local file changes and delay processing start.
    /// </summary>
    /// <typeparam name="t"></typeparam>
    private class MultiQueue<t> : IDisposable
    {
        private readonly object lockObject = new();
        private readonly List<t> itemsToProcess = new();
        private readonly AutoResetEventAsync autoResetEventAsync = new();
        private readonly CancellationTokenSource CancellationTokenSource = new();
        private readonly System.Collections.Concurrent.ConcurrentDictionary<t, int> LockTable = new();
        private readonly Timer AddTimer;

        public CancellationToken CancellationToken => CancellationTokenSource.Token;
        public int RestartDelay = 1000;
        public int WaitForAddingCompleted = 4000;

        public MultiQueue()
        {
            AddTimer = new(AddTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        public Task<t> WaitTakeNextAsync()
        {
            lock (lockObject)
            {
                if (TryTakeNext(out t data))
                {
                    return Task.FromResult(data);
                }
                else
                {
                    return Task.Run(async () =>
                   {
                       await autoResetEventAsync.WaitAsync(CancellationToken).ConfigureAwait(false);
                       if (RestartDelay > 0)
                           await Task.Delay(RestartDelay);

                       return await WaitTakeNextAsync();
                   }, CancellationToken);
                }
            }
        }
        internal bool TryTakeNext(out t data)
        {
            if (itemsToProcess.Any())
            {
                data = itemsToProcess[0];
                itemsToProcess.RemoveAt(0);
                return true;
            }
            else
            {
                data = default;
                return false;
            }
        }
        /// <summary>
        /// Locks item for subsequent adds... While the LockItem is not Disposed, the list does not add the item to list if it is requested by Add or TryAdd
        /// </summary>
        /// <param name="item"></param>
        /// <returns>IDisposable Item which holds the Lock for the item and releases the lock after disposable</returns>
        public DisposableObject<t> LockItem(t item)
        {
            LockTable.AddOrUpdate(item, 1, (k, v) => v + 1);

            return new DisposableObject<t>(data =>
            {
                LockTable.AddOrUpdate(item, 0, (k, v) => v - 1);
            }, item);
        }
        public bool IsItemLocked(t item)
        {
            LockTable.TryGetValue(item, out int value);

            return value != 0;
        }


        public void Add(t data, bool ignoreLock)
        {
            TryAdd(data, ignoreLock);
        }
        public bool TryAdd(t data, bool ignoreLock)
        {
            lock (lockObject)
            {
                if (!itemsToProcess.Contains(data))
                {
                    if (!ignoreLock && IsItemLocked(data))
                        return false;

                    itemsToProcess.Add(data);

                    AddTimer.Change(WaitForAddingCompleted, Timeout.Infinite);

                    return true;
                }
            }

            return false;
        }
        public void Cancel(t data)
        {
            lock (lockObject)
            {
                itemsToProcess.Remove(data);
            }
        }
        public void Complete()
        {
            CancellationTokenSource.Cancel();
        }
        public int Count()
        {
            lock (lockObject)
            {
                return itemsToProcess.Count;
            }
        }

        private void AddTimerCallback(object stateInfo)
        {
            autoResetEventAsync.Set();
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    CancellationTokenSource.Cancel();
                }

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public class DisposableObject<T> : IDisposable
    {
        private bool disposedValue;
        private readonly Action<T> disposeAction;
        private readonly T value;

        public DisposableObject(Action<T> disposeAction, T value)
        {
            this.value = value;
            this.disposeAction = disposeAction;
        }
        public T Value => value;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    disposeAction?.Invoke(value);
                }

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}