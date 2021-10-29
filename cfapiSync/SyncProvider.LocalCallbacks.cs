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

/// <summary>
/// Handle local file Requests
/// </summary>
public partial class SyncProvider
{
    private CF_CALLBACK_REGISTRATION[] _callbackMappings;
    private FileSystemWatcher watcher;
    private readonly System.Collections.Concurrent.BlockingCollection<SyncProviderUtils.DataActions> FetchDataQueue;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, SyncProviderUtils.DataActions> FetchDataRunningQueue;
    private readonly Task FetchDataWorkerThread;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, CancellationTokenSource> FetchPlaceholdersCancellationTokens = new();
    private readonly ActionBlock<DeleteAction> DeleteQueue;
    private readonly FileFetchHelper2 fileFetchHelper = new();


    public void FETCH_PLACEHOLDERS(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        CF_OPERATION_INFO opInfo = CreateOPERATION_INFO(CallbackInfo, CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_PLACEHOLDERS);
        
        CancellationTokenSource ctx = new();
        FetchPlaceholdersCancellationTokens.TryAdd(CallbackInfo.NormalizedPath, ctx);

        FETCH_PLACEHOLDERS_Internal(GetRelativePath(CallbackInfo), opInfo, CallbackParameters.FetchPlaceholders.Pattern, ctx.Token);
    }
    public void CANCEL_FETCH_PLACEHOLDERS(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Styletronix.Debug.WriteLine("Cancel Fetch Placeholders " + CallbackInfo.NormalizedPath, System.Diagnostics.TraceLevel.Info);

        if (FetchPlaceholdersCancellationTokens.TryRemove(CallbackInfo.NormalizedPath, out CancellationTokenSource ctx))
        {
            ctx.Cancel();
            Styletronix.Debug.WriteLine("Fetch Placeholder Cancelled" + CallbackInfo.NormalizedPath, System.Diagnostics.TraceLevel.Verbose);
        }
    }
    public void FETCH_DATA(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Styletronix.Debug.WriteLine(@"FETCH_DATA: Priority " + CallbackInfo.PriorityHint +
            @" / R " + CallbackParameters.FetchData.RequiredFileOffset + @" - " + CallbackParameters.FetchData.RequiredLength +
            @" / O " + CallbackParameters.FetchData.OptionalFileOffset + @" - " + CallbackParameters.FetchData.OptionalLength +
            @" / " + CallbackInfo.NormalizedPath, System.Diagnostics.TraceLevel.Info);

        long length = CallbackParameters.FetchData.RequiredLength;
        long offset = CallbackParameters.FetchData.RequiredFileOffset;

        if ((offset + length) == CallbackParameters.FetchData.OptionalFileOffset)
        {
            if (length < chunkSize)
            {
                length = Math.Min(chunkSize, CallbackParameters.FetchData.OptionalLength + length);
            }
        }

        SyncProviderUtils.DataActions data = new()
        {
            FileOffset = offset,
            Length = length,
            NormalizedPath = CallbackInfo.NormalizedPath,
            PriorityHint = CallbackInfo.PriorityHint,
            TransferKey = CallbackInfo.TransferKey,
            Id = CallbackInfo.NormalizedPath + "!" + CallbackParameters.FetchData.RequiredFileOffset + "!" + CallbackParameters.FetchData.RequiredLength
        };

        fileFetchHelper.Add(data);
    }
    public void CANCEL_FETCH_DATA(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Styletronix.Debug.WriteLine(@"CANCEL_FETCH_DATA: " +
            @" / " + CallbackParameters.Cancel.FetchData.FileOffset + @" - " + CallbackParameters.Cancel.FetchData.Length +
            @" / " + CallbackInfo.NormalizedPath, System.Diagnostics.TraceLevel.Verbose);

        fileFetchHelper.Cancel(new SyncProviderUtils.DataActions
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
        //Styletronix.Debug.WriteLine("NOTIFY_FILE_CLOSE_COMPLETION: " + CallbackInfo.NormalizedPath);
    }
    public void NOTIFY_DELETE(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
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
        Styletronix.Debug.WriteLine("NOTIFY_RENAME: " + CallbackInfo.NormalizedPath + " -> " + CallbackParameters.Rename.TargetPath, System.Diagnostics.TraceLevel.Info);

        if (CallbackParameters.Rename.TargetPath.StartsWith(@"\$Recycle.Bin\"))
        {

        }
        CF_OPERATION_INFO opInfo = CreateOPERATION_INFO(CallbackInfo, CF_OPERATION_TYPE.CF_OPERATION_TYPE_ACK_RENAME);

        NOTIFY_RENAME_Internal(GetRelativePath(CallbackInfo), GetRelativePath(CallbackParameters.Rename),
            CallbackParameters.Rename.Flags.HasFlag(CF_CALLBACK_RENAME_FLAGS.CF_CALLBACK_RENAME_FLAG_IS_DIRECTORY), opInfo);

    }
    public void NOTIFY_RENAME_COMPLETION(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Styletronix.Debug.WriteLine("NOTIFY_RENAME_COMPLETION: " + CallbackParameters.RenameCompletion.SourcePath + " -> " + CallbackInfo.NormalizedPath, System.Diagnostics.TraceLevel.Verbose);
    }




    public async void FETCH_PLACEHOLDERS_Internal(string relativePath, CF_OPERATION_INFO opInfo, string pattern, CancellationToken cancellationToken)
    {

        Styletronix.Debug.WriteLine("Fetch Placeholder: " + relativePath, System.Diagnostics.TraceLevel.Info);

        using SafePlaceHolderList infos = new();
        List<Placeholder> placeholders = new();
        NtStatus completionStatus = NtStatus.STATUS_SUCCESS;

        // Get Filelist from Server
        using (IFileListAsync fileList = SyncContext.ServerProvider.GetNewFileList())
        {
            var result = await fileList.OpenAsync(relativePath, cancellationToken);
            if (!result.Succeeded)
            {
                completionStatus = result.Status;
                goto skip;
            }

            var getNextResult = await fileList.GetNextAsync();
            while (getNextResult.Succeeded)
            {
                var relativeFileName = relativePath + "\\" + getNextResult.Placeholder.RelativeFileName;

                if (!IsExcludedFile(relativeFileName) && !getNextResult.Placeholder.FileAttributes.HasFlag(FileAttributes.System))
                {
                    placeholders.Add(getNextResult.Placeholder);
                    infos.Add(Styletronix.CloudFilterApi.CreatePlaceholderInfo(getNextResult.Placeholder,  Guid.NewGuid().ToString()));
                }

                if (cancellationToken.IsCancellationRequested) break;

                getNextResult = await fileList.GetNextAsync();
            };

            var closeResult = await fileList.CloseAsync();
            completionStatus = closeResult.Status;
        }

        if (cancellationToken.IsCancellationRequested) return;



        skip:
        if (completionStatus == NtStatus.STATUS_NOT_A_CLOUD_FILE)
            completionStatus = NtStatus.STATUS_SUCCESS;

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

        if (completionStatus != NtStatus.STATUS_SUCCESS || !executeResult.Succeeded) return;

        // Validate local Placeholders. CfExecute only adds missing entries, but does not check existing data.
        foreach (Placeholder item in placeholders)
        {
            try
            {
                if (item.ETag != new Placeholder(SyncContext.LocalRootFolder + "\\" + item.RelativeFileName).ETag)
                {
                    await ChangedDataQueueBlock.SendAsync(SyncContext.LocalRootFolder + "\\" + item.RelativeFileName);
                }
            }
            catch (Exception ex)
            {
                Styletronix.Debug.WriteLine(ex.ToString(), System.Diagnostics.TraceLevel.Error);
            }
        }

        foreach (string item in Directory.GetFileSystemEntries(SyncContext.LocalRootFolder + "\\" + relativePath))
        {
            if (!(from a in placeholders where a.RelativeFileName.Equals(GetRelativePath(item), StringComparison.CurrentCultureIgnoreCase) select a).Any())
            {
                await ChangedDataQueueBlock.SendAsync(item);
            }
        }

        SetInSyncState(SyncContext.LocalRootFolder + "\\" + relativePath, CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC, true);
    }

    public bool IsExcludedFile(string relativeOrFullPath)
    {
        if (relativeOrFullPath.Contains(@"$Recycle.Bin")) 
            return true;

        var fileName = Path.GetFileName(relativeOrFullPath);
        if (fileExclusions.Contains(fileName, StringComparer.CurrentCultureIgnoreCase))
            return true;

        return false;
    }


    public async void NOTIFY_RENAME_Internal(string RelativeFileName, string RelativeFileNameDestination, bool isDirectory, CF_OPERATION_INFO opInfo)
    {
        NTStatus status;

        if (!RelativeFileNameDestination.StartsWith(@"\$Recycle.Bin\", StringComparison.CurrentCultureIgnoreCase))
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
        NTStatus status;
        string fullPath = SyncContext.LocalRootFolder + "\\" + dat.RelativePath;
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
        if (pl.PlaceholderInfoBasic.PinState == CF_PIN_STATE.CF_PIN_STATE_EXCLUDED)
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
        status = (int)result.Status;


    skip:
        CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(new CF_OPERATION_PARAMETERS.ACKDELETE
        {
            Flags = CF_OPERATION_ACK_DELETE_FLAGS.CF_OPERATION_ACK_DELETE_FLAG_NONE,
            CompletionStatus = status
        });

        Styletronix.Debug.LogResponse(CfExecute(dat.OpInfo, ref opParams));
    }

    public HRESULT ReportProviderProgress(CF_TRANSFER_KEY transferKey, long total, long completed)
    {
        HRESULT ret = CfReportProviderProgress(this.SyncContext.ConnectionKey, transferKey, total, completed);
        Styletronix.Debug.LogResponse(ret);
        return ret;
    }


    private CF_OPERATION_INFO CreateOPERATION_INFO(in CF_CALLBACK_INFO CallbackInfo, CF_OPERATION_TYPE OperationType)
    {
        CF_OPERATION_INFO opInfo = new()
        {
            Type = OperationType,
            ConnectionKey = CallbackInfo.ConnectionKey,
            TransferKey = CallbackInfo.TransferKey,
            CorrelationVector = CallbackInfo.CorrelationVector,
            RequestKey = CallbackInfo.RequestKey
        };

        opInfo.StructSize = (uint)Marshal.SizeOf(opInfo);
        return opInfo;
    }

    private Task FetchDataWorker()
    {
        return Task.Factory.StartNew(async () =>
        {
            while (!fileFetchHelper.CancellationToken.IsCancellationRequested)
            {
                SyncProviderUtils.FetchRange item = await fileFetchHelper.WaitTakeNextAsync().ConfigureAwait(false);
                if (fileFetchHelper.CancellationToken.IsCancellationRequested) { break; }
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
        }, fileFetchHelper.CancellationToken, TaskCreationOptions.LongRunning |
        TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Default).Unwrap();
    }

    private async Task FetchDataAsync(SyncProviderUtils.FetchRange data)
    {
        string relativePath = data.NormalizedPath.Remove(0, SyncContext.LocalRootFolderNormalized.Length).TrimStart(char.Parse("\\"));
        string targetFullPath = Path.Combine(SyncContext.LocalRootFolder, relativePath);

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

            if (relativePath.Contains(@"$Recycle.bin"))
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
                fileFetchHelper.Cancel(data.NormalizedPath);
                return;
            }


            Placeholder localSimplePlaceholder = null;
            if (File.Exists(targetFullPath))
                localSimplePlaceholder = new(targetFullPath);

            using (IReadFileAsync fetchFile = SyncContext.ServerProvider.GetNewReadFile())
            {
                ReadFileOpenResult openAsyncResult = await fetchFile.OpenAsync(new OpenAsyncParams()
                {
                    RelativeFileName = relativePath,
                    CancellationToken = ctx,
                    ETag = localSimplePlaceholder?.ETag
                });

                CompletionStatus = new NTStatus((uint)openAsyncResult.Status);
                using ExtendedPlaceholderState localPlaceholder = new(targetFullPath);

                // Compare ETag to verify Sync of cloud and local file
                if (CompletionStatus == NTStatus.STATUS_SUCCESS)
                    if (openAsyncResult.Placeholder?.ETag != localPlaceholder.ETag)
                    {
                        Styletronix.Debug.WriteLine("ETag Validation FAILED: " + relativePath, System.Diagnostics.TraceLevel.Info);
                        CompletionStatus = new NTStatus((uint)Styletronix.CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_NOT_IN_SYNC);
                        openAsyncResult.Message = Styletronix.CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_NOT_IN_SYNC.ToString();
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

                    fileFetchHelper.Cancel(data.NormalizedPath);

                    localPlaceholder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC);
                    return;
                }

                byte[] stackBuffer = new byte[stackSize];
                byte[] buffer = new byte[chunkSize];

                long minRangeStart = long.MaxValue;
                long totalRead = 0;

                while (data != null)
                {
                    minRangeStart = Math.Min(minRangeStart, data.RangeStart);
                    long currentRangeStart = data.RangeStart;
                    long currentRangeEnd = data.RangeEnd;

                    long currentOffset = currentRangeStart;
                    long totalLength = currentRangeEnd - currentRangeStart;

                    int readLength = (int)Math.Min(currentRangeEnd - currentOffset, chunkSize);

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

                            fileFetchHelper.Cancel(data.NormalizedPath);
                            return;
                        }
                        int dataRead = readResult.BytesRead;

                        if (data.RangeEnd == 0 || data.RangeEnd < currentOffset || data.RangeStart > currentOffset) { continue; }

                        totalRead += dataRead;
                        this.ReportProviderProgress(data.TransferKey, currentRangeEnd - minRangeStart, totalRead);

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

                        fileFetchHelper.RemoveRange(data.NormalizedPath, currentRangeStart, currentRangeStart + dataRead);
                    }

                    data = fileFetchHelper.TakeNext(data.NormalizedPath);
                }

                await fetchFile.CloseAsync();
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
            fileFetchHelper.Cancel(data.NormalizedPath);
        }
    }

    private class DeleteAction
    {
        public CF_OPERATION_INFO OpInfo;
        public string RelativePath;
        public bool IsDirectory;
    }

    private class FileFetchHelper2 : IDisposable
    {
        private readonly object lockObject = new();
        private readonly List<SyncProviderUtils.FetchRange> filesToProcess = new();
        private readonly AutoResetEventAsync autoResetEventAsync = new();
        private readonly CancellationTokenSource cancellationToken = new();

        public FileFetchHelper2()
        {
            //cancellationToken.Token.Register(() => _waitForNewItem.Close());
        }

        public CancellationToken CancellationToken => cancellationToken.Token;
        public async Task<SyncProviderUtils.FetchRange> WaitTakeNextAsync()
        {
            SyncProviderUtils.FetchRange x = TakeNext();

            if (x == null)
            {
                //   _waitForNewItem.WaitOne();
                await autoResetEventAsync.WaitAsync(CancellationToken).ConfigureAwait(false);
                lock (lockObject)
                {
                    //_waitForNewItem.Reset();
                    SyncProviderUtils.FetchRange t = TakeNext();
                    return t;
                }
            }
            else
            {
                return x;
            }
        }
        public SyncProviderUtils.FetchRange TakeNext()
        {
            SyncProviderUtils.FetchRange ret = null;

            lock (lockObject)
            {
                ret = filesToProcess.OrderByDescending(a => a.PriorityHint)
                    .OrderBy(a => a.RangeStart).FirstOrDefault();

            }

            return ret;
        }
        public SyncProviderUtils.FetchRange TakeNext(string normalizedPath)
        {
            SyncProviderUtils.FetchRange ret = null;

            lock (lockObject)
            {
                ret = filesToProcess.Where(a => a.NormalizedPath == normalizedPath)
                    .OrderBy(a => a.RangeStart).FirstOrDefault();
            }

            return ret;
        }


        public void Add(SyncProviderUtils.DataActions data)
        {
            lock (lockObject)
            {
                filesToProcess.Add(new SyncProviderUtils.FetchRange(data));
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

                foreach (SyncProviderUtils.FetchRange item2 in filesToProcess.Where(a => a.NormalizedPath == normalizedPath).OrderBy(a => a.RangeStart))
                {
                    SyncProviderUtils.FetchRange item3 = filesToProcess.Where(a => a.NormalizedPath == normalizedPath && a.RangeStart <= item2.RangeEnd && a.RangeEnd >= item2.RangeStart && a != item2).OrderBy(a => a.RangeStart).FirstOrDefault();
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
        public void Cancel(SyncProviderUtils.DataActions data)
        {
            List<SyncProviderUtils.FetchRange> removeItems = new();
            List<SyncProviderUtils.FetchRange> addItems = new();

            lock (lockObject)
            {
                long rangeStart = data.FileOffset;
                long rangeEnd = data.FileOffset + data.Length;

                foreach (SyncProviderUtils.FetchRange item in filesToProcess.Where(a => a.NormalizedPath == data.NormalizedPath && a.RangeStart <= rangeEnd && a.RangeEnd >= rangeStart).OrderBy(a => a.RangeStart))
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
                        SyncProviderUtils.FetchRange newItem = new()
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

                foreach (SyncProviderUtils.FetchRange item in removeItems)
                {
                    filesToProcess.Remove(item);
                }
                foreach (SyncProviderUtils.FetchRange item in addItems)
                {
                    filesToProcess.Add(item);
                }

                Combine(data.NormalizedPath);
            }
        }
        public void RemoveRange(string normalizedPath, long rangeStart, long rangeEnd)
        {
            Cancel(new SyncProviderUtils.DataActions()
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

        // // TODO: Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
        // ~FileFetchHelper()
        // {
        //     // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }


}