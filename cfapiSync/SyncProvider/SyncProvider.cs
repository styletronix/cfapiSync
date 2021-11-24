using Styletronix.CloudSyncProvider;
using System;
using System.Collections.Concurrent;
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
    private bool _isConnected;

    private CancellationTokenSource ChangedDataCancellationTokenSource;
    private readonly CancellationTokenSource GlobalShutDownTokenSource = new();
    private readonly SyncContext SyncContext;

    private readonly int chunkSize = 1024 * 1024 * 2; // 2MB chunkSize for File Download / Upload
    private readonly int optionalChunkSizeFaktor = 2; // If optional Offset is supplied, Prefetch x times of chunkSize
    private readonly int optionalChunkSize; // optionalChunkSize = chunkSize * optionalChunkSizeFaktor
    private readonly int stackSize = 1024 * 512; // Buffer size for P/Invoke Call to CFExecute max 1 MB
    private readonly TimeSpan FailedQueueTimerInterval = TimeSpan.FromSeconds(30); // Retry interval for failed files.
    private readonly TimeSpan LocalSyncTimerInterval = TimeSpan.FromMinutes(30); // Interval for local file change checks if FileWatcher missed changes.
    private readonly string[] fileExclusions = new string[] { @".*\\Thumbs\.db", @".*\\Desktop\.ini", @".*\.tmp", @".*Recycle\.Bin.*", @".*\~.*" }; // Files which are not synced. (RegEx)

    private bool disposedValue;
    private CancellationToken GlobalShutDownToken => GlobalShutDownTokenSource.Token;
    private readonly Timer FailedQueueTimer;
    private Timer LocalSyncTimer;

    public event EventHandler<int> QueuedItemsCountChanged;
    public event EventHandler<int> FailedDataQueueChanged;

    public SyncProvider(SyncProviderParameters parameter)
    {
        SyncContext = new()
        {
            LocalRootFolder = parameter.LocalDataPath,
            LocalRootFolderNormalized = parameter.LocalDataPath.Remove(0, 2),
            ServerProvider = parameter.ServerProvider,
            SyncProviderParameter = parameter,
            SyncProvider = this
        };
        SyncContext.ServerProvider.SyncContext = SyncContext;

        optionalChunkSize = optionalChunkSizeFaktor * chunkSize;

        RemoteChangesQueue = new(ProcessRemoteFileChanged);
        FetchDataRunningQueue = new();
        FetchDataWorkerThread = FetchDataWorker();

        DeleteQueue = new(NOTIFY_DELETE_Action);

        SyncContext.ServerProvider.ServerProviderStateChanged += ServerProvider_ServerProviderStateChanged;
        SyncContext.ServerProvider.FileChanged += ServerProvider_FileChanged;

        FailedQueueTimer = new(FailedQueueTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        LocalSyncTimer = new(LocalSyncTimerCallback, null, Timeout.Infinite, Timeout.Infinite);

        SyncActionBlock = new(SyncAction, new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 8,
            CancellationToken = this.GlobalShutDownToken
        });

        ChangedDataQueue = new(ChangedDataAction, new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 8,
            CancellationToken = this.GlobalShutDownToken
        });
    }
    private Task ChangedDataAction(ProcessChangedDataArgs data)
    {
        return ProcessChangedDataAsync2(
            data.FullPath,
            data.LocalPlaceHolder,
            data.RemotePlaceholder,
            data.SyncMode,
            GlobalShutDownToken);
    }
    private async void FailedQueueTimerCallback(object state)
    {
        if (SyncContext.ServerProvider.Status != ServerProviderStatus.Connected)
            return;

        FailedQueueTimer.Change(Timeout.Infinite, Timeout.Infinite);
        try
        {
            var items = FailedDataQueue.AsQueryable();

            foreach (var item in (from a in items where a.Value.NextTry <= DateTime.Now select a))
            {
                if (await ProcessFileChanged(item.Key, SyncMode.Full))
                {
                    FailedDataQueue.TryRemove(item.Key, out _);
                    FailedDataQueueChanged?.Invoke(this, FailedDataQueue.Count());
                }
            }
        }
        finally
        {
            FailedQueueTimer.Change(FailedQueueTimerInterval, FailedQueueTimerInterval);
        }
    }
    private async void LocalSyncTimerCallback(object state)
    {
        if (SyncContext.ServerProvider.Status != ServerProviderStatus.Connected)
            return;

        LocalSyncTimer.Change(Timeout.Infinite, Timeout.Infinite);
        try
        {
            await SyncDataAsync(SyncMode.Local, GlobalShutDownToken);
        }
        catch (Exception ex)
        {
            Styletronix.Debug.LogException(ex);
        }
        finally
        {
            LocalSyncTimer.Change(LocalSyncTimerInterval, LocalSyncTimerInterval);
        }
    }

    public string GetSyncRootID()
    {
        string syncRootID = SyncContext.SyncProviderParameter.ProviderInfo.ProviderId.ToString();
        syncRootID += @"!";
        syncRootID += System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;
        syncRootID += @"!";
        syncRootID += SyncContext.LocalRootFolder.GetHashCode(); // Provider Account -> Used Hash of LocalPath asuming that no Account would be synchronized to the same Folder.
        return syncRootID;
    }
    internal static bool FileOrDirectoryExists(string name)
    {
        return (Directory.Exists(name) || File.Exists(name));
    }
    public async Task Register()
    {
        if (StorageProviderSyncRootManager.IsSupported() == false)
        {
            Styletronix.Debug.WriteLine("OS not supported!", System.Diagnostics.TraceLevel.Error);
            throw new NotSupportedException();
        }

        Windows.Storage.StorageFolder path = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(SyncContext.LocalRootFolder);

        StorageProviderSyncRootInfo SyncRootInfo = new()
        {
            Id = GetSyncRootID(),
            AllowPinning = true,
            DisplayNameResource = SyncContext.SyncProviderParameter.ProviderInfo.ProviderName,
            HardlinkPolicy = StorageProviderHardlinkPolicy.None,
            HydrationPolicy = StorageProviderHydrationPolicy.Partial,

            HydrationPolicyModifier = StorageProviderHydrationPolicyModifier.AutoDehydrationAllowed |
            StorageProviderHydrationPolicyModifier.StreamingAllowed,

            InSyncPolicy = StorageProviderInSyncPolicy.FileLastWriteTime,
            Path = path,
            PopulationPolicy = StorageProviderPopulationPolicy.Full,
            ProtectionMode = StorageProviderProtectionMode.Unknown,
            ProviderId = SyncContext.SyncProviderParameter.ProviderInfo.ProviderId,
            Version = SyncContext.SyncProviderParameter.ProviderInfo.ProviderVersion,
            IconResource = @"C:\WINDOWS\system32\imageres.dll,-1043",
            ShowSiblingsAsGroup = false,
            RecycleBinUri = null,
            Context = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(GetSyncRootID(), Windows.Security.Cryptography.BinaryStringEncoding.Utf8)
        };
        SyncRootInfo.StorageProviderItemPropertyDefinitions.Add(new StorageProviderItemPropertyDefinition() { DisplayNameResource = "Beschreibung", Id = 0 });

        StorageProviderSyncRootManager.Register(SyncRootInfo);

        await Task.Delay(1000);

        #region "Old Implementation"
        //unsafe {
        //fixed (char* ProviderName = this.ProviderName.ToCharArray(), ProviderVersion = this.ProviderVersion.ToCharArray())
        //{
        //    fixed (void* SyncRootIdentity = this.SyncContext.LocalRootFolder)
        //    {
        //        var Registration = new CF_SYNC_REGISTRATION()
        //        {
        //            ProviderId = this.ProviderId,
        //            ProviderName = ProviderName,
        //            ProviderVersion = ProviderVersion,
        //            SyncRootIdentity = SyncRootIdentity,
        //            SyncRootIdentityLength = (uint)(System.Runtime.InteropServices.Marshal.SystemDefaultCharSize * this.SyncContext.LocalRootFolder.Length)
        //        };
        //        Registration.StructSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(Registration);

        //        var Policies = new CF_SYNC_POLICIES
        //        {
        //            HardLink = CF_HARDLINK_POLICY.CF_HARDLINK_POLICY_NONE,
        //            Hydration = new CF_HYDRATION_POLICY()
        //            {
        //                Primary = new CF_HYDRATION_POLICY_PRIMARY_USHORT() { us = (ushort)CF_HYDRATION_POLICY_PRIMARY.CF_HYDRATION_POLICY_PARTIAL },
        //                Modifier = new CF_HYDRATION_POLICY_MODIFIER_USHORT()
        //                {
        //                    us = (ushort)(
        //                CF_HYDRATION_POLICY_MODIFIER.CF_HYDRATION_POLICY_MODIFIER_AUTO_DEHYDRATION_ALLOWED |
        //                CF_HYDRATION_POLICY_MODIFIER.CF_HYDRATION_POLICY_MODIFIER_STREAMING_ALLOWED)
        //                }
        //            },
        //            InSync = CF_INSYNC_POLICY.CF_INSYNC_POLICY_TRACK_ALL,
        //            Population = new CF_POPULATION_POLICY
        //            {
        //                Primary = new CF_POPULATION_POLICY_PRIMARY_USHORT() { us = (ushort)CF_POPULATION_POLICY_PRIMARY.CF_POPULATION_POLICY_PARTIAL },
        //                Modifier = new CF_POPULATION_POLICY_MODIFIER_USHORT() { us = (ushort)CF_POPULATION_POLICY_MODIFIER.CF_POPULATION_POLICY_MODIFIER_NONE }
        //            }
        //        };
        //        Policies.StructSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(Policies);

        //        var ret = PInvoke.CfRegisterSyncRoot(this.SyncContext.LocalRootFolder, Registration, Policies, CF_REGISTER_FLAGS.CF_REGISTER_FLAG_UPDATE);
        //        ret.ThrowOnFailure();
        //    }
        //}
        //}

        // Not required anymore.... Will automatically added  during StorageProviderSyncRootManager.Register
        // Add to Explorer
        //try
        //{
        //    Microsoft.Win32.RegistryKey CLSIDkey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID", true).CreateSubKey(this.SyncContext.SyncProviderParameter.ProviderInfo.CLSID, true);
        //    CLSIDkey.SetValue("", this.SyncContext.SyncProviderParameter.ProviderInfo.ProviderName, Microsoft.Win32.RegistryValueKind.String);
        //    CLSIDkey.SetValue(@"System.IsPinnedToNameSpaceTree", 1, Microsoft.Win32.RegistryValueKind.DWord);

        //    CLSIDkey.CreateSubKey("DefaultIcon", true).SetValue("", @"C:\WINDOWS\system32\imageres.dll,-1043", Microsoft.Win32.RegistryValueKind.ExpandString);
        //    CLSIDkey.CreateSubKey("InProcServer32", true).SetValue("", @"C:\WINDOWS\system32\shell32.dll", Microsoft.Win32.RegistryValueKind.ExpandString);

        //    Microsoft.Win32.RegistryKey InstanceKey = CLSIDkey.CreateSubKey("Instance", true);
        //    InstanceKey.SetValue("CLSID", @"{0E5AAE11-A475-4c5b-AB00-C66DE400274E} ", Microsoft.Win32.RegistryValueKind.String);

        //    Microsoft.Win32.RegistryKey InitPropertyBagKey = InstanceKey.CreateSubKey(@"InitPropertyBag", true);
        //    InitPropertyBagKey.SetValue("TargetFolderPath", this.SyncContext.LocalRootFolder, Microsoft.Win32.RegistryValueKind.ExpandString);
        //    InitPropertyBagKey.SetValue("Attributes", 17, Microsoft.Win32.RegistryValueKind.DWord);

        //    Microsoft.Win32.RegistryKey ShellFolderKey = CLSIDkey.CreateSubKey(@"ShellFolder", true);
        //    ShellFolderKey.SetValue("Attributes", unchecked((int)0xF080004D), Microsoft.Win32.RegistryValueKind.DWord);
        //    ShellFolderKey.SetValue("FolderValueFlags", 40, Microsoft.Win32.RegistryValueKind.DWord);

        //    Microsoft.Win32.RegistryKey NameSpacekey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace", true).CreateSubKey(this.SyncContext.SyncProviderParameter.ProviderInfo.CLSID, true);
        //    NameSpacekey.SetValue("", this.SyncContext.SyncProviderParameter.ProviderInfo.ProviderName, Microsoft.Win32.RegistryValueKind.String);

        //    Microsoft.Win32.RegistryKey NewStartPanelkey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", true);
        //    NewStartPanelkey.SetValue(this.SyncContext.SyncProviderParameter.ProviderInfo.CLSID, 1, Microsoft.Win32.RegistryValueKind.DWord);
        //}
        //catch (Exception ex)
        //{
        //    Styletronix.Debug.WriteLine(ex.Message);
        //}
        #endregion
    }
    public string GetSyncRootManagerRegistryKeyHKLM()
    {
        return @"Software\Microsoft\Windows\CurrentVersion\Explorer\SyncRootManager\" + GetSyncRootID();
    }
    public string GetNamespaceCLSID()
    {
        return (string)Microsoft.Win32.Registry.LocalMachine.OpenSubKey(GetSyncRootManagerRegistryKeyHKLM(), false).GetValue("NamespaceCLSID");
    }
    public string GetSyncRootManagerNameSpaceRegistryKeyHKCU()
    {
        return @"Software\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\" + GetNamespaceCLSID();
    }

    public async Task Unregister()
    {
        await Stop();

        MaintenanceInProgress = true;
        Styletronix.Debug.WriteLine("Unregister", System.Diagnostics.TraceLevel.Info);

        try
        {
            StorageProviderSyncRootManager.Unregister(GetSyncRootID());
            Styletronix.Debug.WriteLine("Unregister completed", System.Diagnostics.TraceLevel.Info);
        }
        catch (Exception ex)
        {
            Styletronix.Debug.WriteLine(ex.Message, System.Diagnostics.TraceLevel.Error);
        }

        // Old Implementation
        //try
        //{
        //    Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID", true).DeleteSubKeyTree(this.SyncContext.SyncProviderParameter.ProviderInfo.CLSID, false);
        //    Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace", true).DeleteSubKeyTree(this.SyncContext.SyncProviderParameter.ProviderInfo.CLSID, false);
        //    Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", true).DeleteSubKey(this.SyncContext.SyncProviderParameter.ProviderInfo.CLSID, false);
        //}
        //catch (Exception ex)
        //{
        //    Styletronix.Debug.WriteLine(ex.Message);
        //}
    }

    private bool SyncInProgress;
    public Task SyncDataAsync(SyncMode syncMode)
    {
        return SyncDataAsync(syncMode, "", GlobalShutDownToken);
    }
    public Task SyncDataAsync(SyncMode syncMode, string relativePath)
    {
        return SyncDataAsync(syncMode, relativePath, GlobalShutDownToken);
    }
    public Task SyncDataAsync(SyncMode syncMode, CancellationToken ctx)
    {
        return SyncDataAsync(syncMode, "", ctx);
    }
    //public async Task SyncDataAsync(SyncMode syncMode, string relativePath, CancellationToken ctx)
    //{
    //    switch (syncMode)
    //    {
    //        case SyncMode.Local:
    //            CfUpdateSyncProviderStatus(SyncContext.ConnectionKey, CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_SYNC_INCREMENTAL);
    //            break;

    //        case SyncMode.Full:
    //            CfUpdateSyncProviderStatus(SyncContext.ConnectionKey, CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_SYNC_FULL);
    //            break;
    //    }

    //    try
    //    {
    //        await Task.Factory.StartNew(async () =>
    //                {
    //                    if (relativePath.Length > 0) relativePath = "\\" + relativePath;

    //                    await FindLocalChangedDataRecursive(SyncContext.LocalRootFolder + relativePath, ctx, syncMode).ConfigureAwait(false);
    //                }, ctx, TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Default);
    //    }
    //    finally
    //    {
    //        CfUpdateSyncProviderStatus(SyncContext.ConnectionKey, CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_IDLE);
    //    }
    //}


    private Task SyncAction(SyncDataParam data)
    {
        return SyncDataAsyncRecursive(data.Folder, data.Ctx, data.SyncMode);
    }

    public async Task SyncDataAsync(SyncMode syncMode, string relativePath, CancellationToken ctx)
    {
        if(SyncInProgress) return;

        if (MaintenanceInProgress) return;

        switch (syncMode)
        {
            case SyncMode.Local:
                CfUpdateSyncProviderStatus(SyncContext.ConnectionKey, CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_SYNC_INCREMENTAL);
                break;

            case SyncMode.Full:
                CfUpdateSyncProviderStatus(SyncContext.ConnectionKey, CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_SYNC_FULL);
                break;
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            LocalChangedDataQueue.Reset();
            if (syncMode == SyncMode.Full)
                RemoteChangedDataQueue.Reset();
        }

        try
        {
            SyncInProgress = true;
            await Task.Factory.StartNew(async () =>
            {
                if (relativePath.Length > 0)
                {
                    relativePath = "\\" + relativePath;
                }
                try
                {
                    await SyncDataAsyncRecursive(SyncContext.LocalRootFolder + relativePath, ctx, syncMode).ConfigureAwait(false);
                }
                catch (Exception ex)
                {

                }


            }, ctx, TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Default);
        }
        finally
        {
            SyncInProgress = false;
            CfUpdateSyncProviderStatus(SyncContext.ConnectionKey, CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_IDLE);
        }
    }

    internal async Task<GenericResult<List<Placeholder>>> GetServerFileListAsync(string relativePath, CancellationToken cancellationToken)
    {
        Styletronix.Debug.WriteLine("GetServerFileListAsync: " + relativePath, System.Diagnostics.TraceLevel.Verbose);

        GenericResult<List<Placeholder>> completionStatus = new();
        completionStatus.Data = new List<Placeholder>();

        using IFileListAsync fileList = SyncContext.ServerProvider.GetNewFileList();
        GenericResult result = await fileList.OpenAsync(relativePath, cancellationToken);
        if (!result.Succeeded)
        {
            completionStatus.Status = result.Status;
            return completionStatus;
        }

        GetNextResult getNextResult = await fileList.GetNextAsync();
        while (getNextResult.Succeeded && !cancellationToken.IsCancellationRequested)
        {
            string relativeFileName = relativePath + "\\" + getNextResult.Placeholder.RelativeFileName;

            if (!IsExcludedFile(relativeFileName) && !getNextResult.Placeholder.FileAttributes.HasFlag(FileAttributes.System))
            {
                completionStatus.Data.Add(getNextResult.Placeholder);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            getNextResult = await fileList.GetNextAsync();
        };

        GenericResult closeResult = await fileList.CloseAsync();
        completionStatus.Status = closeResult.Status;

        cancellationToken.ThrowIfCancellationRequested();

        Styletronix.Debug.WriteLine("GetServerFileListAsync Completed: " + relativePath, System.Diagnostics.TraceLevel.Verbose);
        return completionStatus;
    }
    internal List<Placeholder> GetLocalFileList(string absolutePath, CancellationToken cancellationToken)
    {
        List<Placeholder> localPlaceholders = new();
        DirectoryInfo directory = new(absolutePath);

        foreach (FileSystemInfo fileSystemInfo in directory.EnumerateFileSystemInfos())
        {
            cancellationToken.ThrowIfCancellationRequested();
            localPlaceholders.Add(new Placeholder(fileSystemInfo));
        }

        return localPlaceholders;
    }

    private ActionBlock<SyncDataParam> SyncActionBlock;
    public class SyncDataParam
    {
        public string Folder;
        public SyncMode SyncMode;
        public CancellationToken Ctx;
    }


    private async Task<bool> SyncDataAsyncRecursive(string folder, CancellationToken ctx, SyncMode syncMode)
    {
        Styletronix.Debug.WriteLine("SyncDataAsyncRecursive SyncMode " + syncMode.ToString() + " : " + folder, System.Diagnostics.TraceLevel.Info);

        string relativeFolder = GetRelativePath(folder);
        bool anyFileHydrated = false;
        List<Placeholder> remotePlaceholderes;

        using ExtendedPlaceholderState localFolderPlaceholder = new(folder);
        bool isExcludedFile = IsExcludedFile(folder, localFolderPlaceholder.Attributes);

        // Get Filelist from Server on FullSync
        if (syncMode >= SyncMode.Full && !isExcludedFile)
        {
            GenericResult<List<Placeholder>> getServerFileListResult = await GetServerFileListAsync(relativeFolder, ctx);
            if (getServerFileListResult.Status != NtStatus.STATUS_NOT_A_CLOUD_FILE)
            {
                getServerFileListResult.ThrowOnFailure();
            }

            remotePlaceholderes = getServerFileListResult.Data;
        }
        else
        {
            remotePlaceholderes = new();
        }

        if (isExcludedFile)
        {
            localFolderPlaceholder.ConvertToPlaceholder(true);
            localFolderPlaceholder.SetPinState(CF_PIN_STATE.CF_PIN_STATE_EXCLUDED);
            localFolderPlaceholder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC);
        }

        using Kernel32.SafeSearchHandle findHandle = Kernel32.FindFirstFile(@"\\?\" + folder + @"\*", out WIN32_FIND_DATA findData);
        bool fileFound = (findHandle.IsInvalid == false);
        using AutoDisposeList<ExtendedPlaceholderState> localPlaceholders = new();

        //List<Task> taskList = new();

        // Check existing local placeholders
        while (fileFound)
        {
            if (findData.cFileName != "." && findData.cFileName != "..")
            {
                string fullFilePath = folder + "\\" + findData.cFileName;

                ExtendedPlaceholderState localPlaceholder = new(findData, folder);
                localPlaceholders.Add(localPlaceholder);

                Placeholder remotePlaceholder = (from a in remotePlaceholderes where string.Equals(a.RelativeFileName, findData.cFileName, StringComparison.CurrentCultureIgnoreCase) select a).FirstOrDefault();

                if (localPlaceholder.IsDirectory)
                {
                    if (localPlaceholder.IsPlaceholder)
                    {
                        if (!localPlaceholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL) ||
                            !localPlaceholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC) ||
                            isExcludedFile ||
                            localPlaceholder.PlaceholderInfoStandard.PinState == CF_PIN_STATE.CF_PIN_STATE_PINNED)
                        {
                            if (syncMode == SyncMode.Full)
                            {
                                await SyncActionBlock.SendAsync(new SyncDataParam
                                {
                                    Ctx = ctx,
                                    Folder = fullFilePath,
                                    SyncMode = syncMode
                                });
                                anyFileHydrated = true;
                            }
                            else
                            {
                                if (await SyncDataAsyncRecursive(fullFilePath, ctx, syncMode))
                                    anyFileHydrated = true;
                            }
                        }
                        else
                        {
                            localPlaceholder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC);
                            // Ignore Directorys which will trigger FETCH_PLACEHOLDER
                        }
                    }
                    else
                    {
                        try
                        {
                            if (syncMode == SyncMode.Full)
                            {
                                await ProcessChangedDataAsync(fullFilePath, localPlaceholder, (DynamicServerPlaceholder)remotePlaceholder, syncMode, ctx);
                            }
                            else
                            {
                                await ProcessChangedDataAsync(fullFilePath, localPlaceholder, new DynamicServerPlaceholder(GetRelativePath(fullFilePath), localPlaceholder.IsDirectory, this.SyncContext), syncMode, ctx);
                            }
                        }
                        catch (Exception)
                        {
                            AddFileToLocalChangeQueue(fullFilePath, true);
                        }

                        if (syncMode == SyncMode.Full)
                        {
                            await SyncActionBlock.SendAsync(new SyncDataParam
                            {
                                Ctx = ctx,
                                Folder = fullFilePath,
                                SyncMode = syncMode
                            });
                            anyFileHydrated = true;
                        }
                        else
                        {
                            if (await SyncDataAsyncRecursive(fullFilePath, ctx, syncMode))
                                anyFileHydrated = true;
                        }
                    }
                }
                else
                {
                    DynamicServerPlaceholder dynPlaceholder;
                    if (syncMode == SyncMode.Full)
                    {
                        dynPlaceholder = (DynamicServerPlaceholder)remotePlaceholder;
                    }
                    else
                    {
                        dynPlaceholder = new DynamicServerPlaceholder(GetRelativePath(fullFilePath), localPlaceholder.IsDirectory, this.SyncContext);
                    }
                    await ProcessChangedDataAsync(fullFilePath, localPlaceholder, dynPlaceholder, syncMode, ctx);
                }

                if (!localPlaceholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC) || !localPlaceholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL) || localPlaceholder.PlaceholderInfoStandard.OnDiskDataSize > 0)
                    anyFileHydrated = true;

            }

            ctx.ThrowIfCancellationRequested();
            fileFound = Kernel32.FindNextFile(findHandle, out findData);
        }

        //await Task.WhenAll(taskList.ToArray());

        foreach (ExtendedPlaceholderState lpl in localPlaceholders)
        {
            if (!lpl.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC) || !lpl.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL) || lpl.PlaceholderInfoStandard.OnDiskDataSize > 0)
            {
                anyFileHydrated = true;
                break;
            }
        }

        // Add missing local Placeholders
        foreach (Placeholder remotePlaceholder in remotePlaceholderes)
        {
            string fullFilePath = folder + "\\" + remotePlaceholder.RelativeFileName;

            if ((from a in localPlaceholders where string.Equals(a.FullPath, fullFilePath, StringComparison.CurrentCultureIgnoreCase) select a).Any() == false)
            {
                CF_PLACEHOLDER_CREATE_INFO[] a = new CF_PLACEHOLDER_CREATE_INFO[1];
                a[0] = CreatePlaceholderInfo(remotePlaceholder, Guid.NewGuid().ToString());
                HRESULT ret = CfCreatePlaceholders(folder, a, 1, CF_CREATE_FLAGS.CF_CREATE_FLAG_NONE, out uint EntriesProcessed);
                Styletronix.Debug.LogResponse(ret);
            }
        }

        if (syncMode == SyncMode.Full)
        {
            localFolderPlaceholder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC);

            if (!anyFileHydrated && !isExcludedFile && localFolderPlaceholder.PlaceholderInfoStandard.PinState != CF_PIN_STATE.CF_PIN_STATE_PINNED)
            {
                localFolderPlaceholder.EnableOnDemandPopulation();
            }
            else
            {
                localFolderPlaceholder.DisableOnDemandPopulation();
            }
        }
        //else
        //{
        //    if (!anyFileHydrated && !isExcludedFile && localFolderPlaceholder.PlaceholderInfoStandard.PinState != CF_PIN_STATE.CF_PIN_STATE_PINNED)
        //        localFolderPlaceholder.EnableOnDemandPopulation();
        //}

        return anyFileHydrated;
    }

    public async Task Start()
    {
        if (Directory.Exists(SyncContext.LocalRootFolder) == false)
        {
            Directory.CreateDirectory(SyncContext.LocalRootFolder);
        }

        Styletronix.Debug.WriteLine("Register", System.Diagnostics.TraceLevel.Info);

        await Register();

        Styletronix.Debug.WriteLine("Connect", System.Diagnostics.TraceLevel.Info);

        _callbackMappings = new CF_CALLBACK_REGISTRATION[]
      {
          new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(FETCH_PLACEHOLDERS),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_FETCH_PLACEHOLDERS
                } ,
           new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(CANCEL_FETCH_PLACEHOLDERS),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_CANCEL_FETCH_PLACEHOLDERS
                } ,
          new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(FETCH_DATA),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_FETCH_DATA
                } ,
          new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(CANCEL_FETCH_DATA),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_CANCEL_FETCH_DATA
                } ,
          new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(NOTIFY_FILE_OPEN_COMPLETION),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_FILE_OPEN_COMPLETION
                } ,
          new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(NOTIFY_FILE_CLOSE_COMPLETION),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_FILE_CLOSE_COMPLETION
                } ,
           new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(NOTIFY_DELETE),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DELETE
                } ,
           new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(NOTIFY_DELETE_COMPLETION),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DELETE_COMPLETION
                } ,
           new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(NOTIFY_RENAME),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_RENAME
                } ,
            new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(NOTIFY_RENAME_COMPLETION),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_RENAME_COMPLETION
                } ,
           CF_CALLBACK_REGISTRATION.CF_CALLBACK_REGISTRATION_END
      };

        SyncContext.ConnectionKey = default;

        HRESULT ret = CfConnectSyncRoot(SyncContext.LocalRootFolder, _callbackMappings,
            IntPtr.Zero,
            CF_CONNECT_FLAGS.CF_CONNECT_FLAG_REQUIRE_PROCESS_INFO |
            CF_CONNECT_FLAGS.CF_CONNECT_FLAG_REQUIRE_FULL_FILE_PATH,
            out SyncContext.ConnectionKey);

        if (ret.Succeeded)
        {
            _isConnected = true;
            Styletronix.Debug.WriteLine("Connected", System.Diagnostics.TraceLevel.Verbose);
        }
        else
        {
            Styletronix.Debug.WriteLine("Connection failed!", System.Diagnostics.TraceLevel.Error);
        }
        ret.ThrowIfFailed();

        InitWatcher();


        Styletronix.Debug.WriteLine("Connect to Server...", System.Diagnostics.TraceLevel.Info);
        GenericResult connectResult = await SyncContext.ServerProvider.Connect();
        Styletronix.Debug.WriteLine("Connect result: " + connectResult.Status.ToString(), System.Diagnostics.TraceLevel.Verbose);

        // FullSync should be initiated by ServerProvider after connection.
        //Styletronix.Debug.WriteLine("Full Sync...", System.Diagnostics.TraceLevel.Info);
        //await SyncDataAsync(SyncMode.Full, GlobalShutDownToken);

        ret = CfUpdateSyncProviderStatus(SyncContext.ConnectionKey, CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_IDLE);
        if (ret.Succeeded == false) { Styletronix.Debug.WriteLine("Fehler bei CfUpdateSyncProviderStatus: " + ret.ToString(), System.Diagnostics.TraceLevel.Warning); }

        Styletronix.Debug.WriteLine("Ready", System.Diagnostics.TraceLevel.Verbose);
    }
    public async Task Stop()
    {
        GlobalShutDownTokenSource.Cancel();

        Styletronix.Debug.WriteLine("Disconnecting....", System.Diagnostics.TraceLevel.Info);
        _ = await SyncContext.ServerProvider.Disconnect();

        if (_isConnected == false) { return; }

        HRESULT ret = CfDisconnectSyncRoot(SyncContext.ConnectionKey);
        if (ret.Succeeded)
        {
            _isConnected = false;
        }
        Styletronix.Debug.WriteLine("DisconnectSyncRoot: " + ret.ToString(), System.Diagnostics.TraceLevel.Verbose);

        CfUpdateSyncProviderStatus(SyncContext.ConnectionKey, CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_TERMINATED);
    }

    public async Task DeleteLocalData()
    {
        await Stop();

        HRESULT ret = CfUnregisterSyncRoot(SyncContext.LocalRootFolder);
        Styletronix.Debug.WriteLine("UnregisterSyncRoot: " + ret.ToString(), System.Diagnostics.TraceLevel.Verbose);

        try
        {
            if (Directory.Exists(SyncContext.LocalRootFolder))
            {
                Directory.Delete(SyncContext.LocalRootFolder, true);
            }
        }
        catch (Exception ex)
        {
            Styletronix.Debug.WriteLine("Delete Root Folder: " + ex.ToString(), System.Diagnostics.TraceLevel.Error);
        }
    }
    //public Task RevertAllPlaceholders()
    //{
    //    //throw new NotImplementedException("This function is currently not working and requires internal modifications!");

    //    return RevertAllPlaceholders(new CancellationToken());
    //}

    private bool MaintenanceInProgress = false;

    //public async Task RevertAllPlaceholders(CancellationToken ctx)
    //{
    //    Styletronix.Debug.WriteLine("RevertAllPlaceholders", System.Diagnostics.TraceLevel.Verbose);
    //    MaintenanceInProgress = true;

    //    Styletronix.Debug.WriteLine("RevertAllPlaceholders: FullSync", System.Diagnostics.TraceLevel.Verbose);

    //    //await SyncDataAsync(SyncMode.Full);

    //    //StopWatcher();

    //    Styletronix.Debug.WriteLine("TODO: RevertAllPlaceholders ASYNC", System.Diagnostics.TraceLevel.Info);

    //    foreach (string item in Directory.EnumerateFileSystemEntries(SyncContext.LocalRootFolder, "*", SearchOption.AllDirectories))
    //    {
    //        using ExtendedPlaceholderState pl = new(item);
    //        if (pl.IsPlaceholder && !pl.IsDirectory)
    //        {
    //            pl.SetPinState(CF_PIN_STATE.CF_PIN_STATE_EXCLUDED);

    //            if (pl.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
    //            {
    //                Styletronix.Debug.WriteLine("DELETE ", System.Diagnostics.TraceLevel.Verbose);
    //                File.Delete(item);
    //            }
    //            else
    //            {
    //                pl.RevertPlaceholder(true).ThrowOnFailure();
    //            }
    //        }

    //        await Unregister();

    //        await Task.CompletedTask;
    //    }
    //}


    public readonly ConcurrentDictionary<string, FailedData> FailedDataQueue = new();

    // TODO: Restructure of Queues to have one queue containing path, sync mode and optional remote placeholder supplied from ServerProvider during change notification.
    // TODO: Add abiltity to reduce queue to folder path if many files inside folder changed.
    private readonly MultiQueue<string> LocalChangedDataQueue = new();
    private readonly MultiQueue<string> RemoteChangedDataQueue = new();

    private Task LocalChangedDataQueueTask;
    private Task RemoteChangedDataQueueTask;

    private void InitWatcher()
    {
        Styletronix.Debug.WriteLine("InitWatcher", System.Diagnostics.TraceLevel.Verbose);

        StopWatcher();

        ChangedDataCancellationTokenSource = new CancellationTokenSource();
        GlobalShutDownToken.Register(() => ChangedDataCancellationTokenSource.Cancel());
        ChangedDataCancellationTokenSource.Token.Register(() => LocalChangedDataQueue.Complete());
        ChangedDataCancellationTokenSource.Token.Register(() => RemoteChangedDataQueue.Complete());

        LocalChangedDataQueueTask = CreateLocalChangedDataQueueTask();
        RemoteChangedDataQueueTask = CreateRemoteChangedDataQueueTask();

        watcher = new FileSystemWatcher
        {
            Path = SyncContext.LocalRootFolder,
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.Attributes | NotifyFilters.LastWrite | NotifyFilters.Size,
            Filter = "*"
        };
        watcher.Error += new ErrorEventHandler(FileSystemWatcher_OnError);
        watcher.Changed += new FileSystemEventHandler(FileSystemWatcher_OnChanged);
        watcher.Created += new FileSystemEventHandler(FileSystemWatcher_OnChanged);
        watcher.EnableRaisingEvents = true;
    }
    private void StopWatcher()
    {
        if (ChangedDataCancellationTokenSource != null)
        {
            ChangedDataCancellationTokenSource.Cancel();
        }

        if (watcher != null)
        {
            Styletronix.Debug.WriteLine("StopWatcher", System.Diagnostics.TraceLevel.Verbose);
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
    }


    private Task CreateLocalChangedDataQueueTask()
    {
        return Task.Factory.StartNew(async () =>
        {
            while (!LocalChangedDataQueue.CancellationToken.IsCancellationRequested)
            {
                string item = await LocalChangedDataQueue.WaitTakeNextAsync().ConfigureAwait(false);

                if (LocalChangedDataQueue.CancellationToken.IsCancellationRequested) { break; }

                if (item != null)
                {
                    DisposableObject<string> itemLock = LocalChangedDataQueue.LockItemDisposable(item);
                    try
                    {
                        await ProcessFileChanged(item, SyncMode.Local);
                    }
                    catch (Exception ex)
                    {
                        Styletronix.Debug.LogException(ex);
                    }
                    finally
                    {
                        // Delay before releasing itemLock
                        _ = Task.Delay(20).ContinueWith(_ => itemLock.Dispose());
                    }
                }
            }
        }, LocalChangedDataQueue.CancellationToken, TaskCreationOptions.LongRunning |
        TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Default).Unwrap();
    }
    private Task CreateRemoteChangedDataQueueTask()
    {
        return Task.Factory.StartNew(async () =>
        {
            while (!RemoteChangedDataQueue.CancellationToken.IsCancellationRequested)
            {
                string item = await RemoteChangedDataQueue.WaitTakeNextAsync().ConfigureAwait(false);

                if (RemoteChangedDataQueue.CancellationToken.IsCancellationRequested) { break; }

                if (item != null)
                {
                    DisposableObject<string> itemLock = RemoteChangedDataQueue.LockItemDisposable(item);
                    try
                    {
                        await ProcessFileChanged(item, SyncMode.Full);
                    }
                    catch (Exception ex)
                    {
                        Styletronix.Debug.LogException(ex);
                    }
                    finally
                    {
                        // Delay before releasing itemLock
                        _ = Task.Delay(20).ContinueWith(_ => itemLock.Dispose());
                    }
                }
            }
        }, RemoteChangedDataQueue.CancellationToken, TaskCreationOptions.LongRunning |
        TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Default).Unwrap();
    }

    private async Task<bool> ProcessFileChanged(string path, SyncMode syncMode)
    {
        try
        {
            QueuedItemsCountChanged?.Invoke(this, LocalChangedDataQueue.Count() + RemoteChangedDataQueue.Count());

            await ProcessChangedDataAsync(path, syncMode, ChangedDataCancellationTokenSource.Token).ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            Styletronix.Debug.LogException(ex);

            FailedData failedData = new()
            {
                LastException = ex,
                LastTry = DateTime.Now,
                NextTry = DateTime.Now.AddSeconds(20),
                RetryCount = 0,
                SyncMode = syncMode
            };

            FailedDataQueue.AddOrUpdate(path, failedData, (key, current) =>
            {
                current.LastTry = failedData.LastTry;
                current.NextTry = failedData.NextTry;
                current.RetryCount += 1;
                current.SyncMode = current.SyncMode > failedData.SyncMode ? current.SyncMode : failedData.SyncMode;
                return current;
            });
            FailedDataQueueChanged?.Invoke(this, FailedDataQueue.Count());

            return false;
        }
    }

    private async Task ProcessChangedDataAsync(string fullPath, SyncMode syncMode, CancellationToken ctx)
    {
        // Ignore deleted Files
        if (!FileOrDirectoryExists(fullPath))
        {
            return;
        }

        using ExtendedPlaceholderState localPlaceHolder = new(fullPath);
        await ProcessChangedDataAsync(fullPath, localPlaceHolder, syncMode, ctx).ConfigureAwait(false);
    }
    private async Task ProcessChangedDataAsync(string fullPath, ExtendedPlaceholderState localPlaceHolder, SyncMode syncMode, CancellationToken ctx)
    {
        await ProcessChangedDataAsync(fullPath, localPlaceHolder, new DynamicServerPlaceholder(GetRelativePath(fullPath), localPlaceHolder.IsDirectory, this.SyncContext), syncMode, ctx);
    }


    private ActionBlock<ProcessChangedDataArgs> ChangedDataQueue;
    public class ProcessChangedDataArgs
    {
        public string FullPath;
        public ExtendedPlaceholderState LocalPlaceHolder;
        public DynamicServerPlaceholder RemotePlaceholder;
        public SyncMode SyncMode;
    }

    private Task<bool> AddChangedDataToQueueAsync(string fullPath, ExtendedPlaceholderState localPlaceHolder, DynamicServerPlaceholder remotePlaceholder, SyncMode syncMode)
    {
        return ChangedDataQueue.SendAsync(new ProcessChangedDataArgs
        {
            SyncMode = syncMode,
            FullPath = fullPath,
            LocalPlaceHolder = localPlaceHolder,
            RemotePlaceholder = remotePlaceholder
        });
    }

    private  Task ProcessChangedDataAsync(string fullPath, ExtendedPlaceholderState localPlaceHolder, DynamicServerPlaceholder remotePlaceholder, SyncMode syncMode, CancellationToken ctx)
    {
        return ChangedDataQueue.SendAsync(new ProcessChangedDataArgs
        {
            SyncMode = syncMode,
            FullPath = fullPath,
            LocalPlaceHolder = localPlaceHolder,
            RemotePlaceholder = remotePlaceholder
        });
    }
    private async Task ProcessChangedDataAsync2(string fullPath, ExtendedPlaceholderState localPlaceHolder, DynamicServerPlaceholder remotePlaceholder, SyncMode syncMode, CancellationToken ctx)
    {
        try
        {
            string relativePath = GetRelativePath(fullPath);
            Styletronix.Debug.WriteLine("ProcessFileChanged: " + relativePath, System.Diagnostics.TraceLevel.Verbose);

            // Convert to placeholder if required
            if (!localPlaceHolder.ConvertToPlaceholder(false))
                throw new Exception("Convert to Placeholder failed");


            // Ignore special files.
            if (IsExcludedFile(fullPath, localPlaceHolder.Attributes))
            {
                localPlaceHolder.SetPinState(CF_PIN_STATE.CF_PIN_STATE_EXCLUDED);
                localPlaceHolder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC);
            }
            else if (localPlaceHolder.PlaceholderInfoStandard.PinState == CF_PIN_STATE.CF_PIN_STATE_EXCLUDED)
            {
                localPlaceHolder.SetPinState(CF_PIN_STATE.CF_PIN_STATE_INHERIT);
            }


            if (localPlaceHolder.PlaceholderInfoStandard.PinState == CF_PIN_STATE.CF_PIN_STATE_EXCLUDED)
                return;

            if (localPlaceHolder.IsDirectory)
            {
                if (syncMode == SyncMode.Full)
                {
                    if ((await remotePlaceholder.GetPlaceholder()) == null)
                    {
                        // Directory does not exist on Server
                        if (localPlaceHolder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC))
                        {
                            // Directory remotely deleted if it was in sync.
                            Styletronix.Debug.WriteLine("TODO: Remove local Directory if empty...", System.Diagnostics.TraceLevel.Warning);
                            //Directory.Delete(localPlaceHolder.FullPath, false);
                            return;
                        }
                        else
                        {
                            // File locally created or modified while deleted on Server
                            Styletronix.Debug.WriteLine("Create Directory on Server: " + relativePath, System.Diagnostics.TraceLevel.Info);

                            CreateFileResult creatResult = await SyncContext.ServerProvider.CreateFileAsync(relativePath, true);
                            creatResult.ThrowOnFailure();

                            localPlaceHolder.UpdatePlaceholder(creatResult.Placeholder,
                                   CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC).ThrowOnFailure();

                            return;
                        }
                    }
                }
            }
            else
            {
                // Compare with remote file if Full Sync
                if (syncMode == SyncMode.Full)
                {
                    if ((await remotePlaceholder.GetPlaceholder()) == null)
                    // New local file or remote deleted File
                    {
                        // File does not exist on Server
                        if (localPlaceHolder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC))
                        {
                            // File remotely deleted if it was in sync.
                            MoveToRecycleBin(localPlaceHolder);
                            return;
                        }
                        else
                        {
                            // File locally created or modified while deleted on Server
                            WriteFileCloseResult uploadFileToServerResult = await UploadFileToServer(fullPath, ctx);
                            uploadFileToServerResult.ThrowOnFailure();
                            return;
                        }
                    }

                    // Validate ETag
                    ValidateETag(localPlaceHolder, (await remotePlaceholder.GetPlaceholder()));
                }


                // local file full populated and out of sync
                if (localPlaceHolder.PlaceholderInfoStandard.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC &&
                    !localPlaceHolder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
                {
                    // Local File changed: Upload to Server
                    if (await remotePlaceholder.GetPlaceholder() == null || (localPlaceHolder.LastWriteTime > (await remotePlaceholder.GetPlaceholder()).LastWriteTime))
                    {
                        await UploadFileToServer(fullPath, ctx);
                        localPlaceHolder.Reload();
                    }
                    else
                    {
                        // Local File requires update...
                        if (localPlaceHolder.PlaceholderInfoStandard.PinState == CF_PIN_STATE.CF_PIN_STATE_PINNED)
                        {
                            HydratePlaceholder(localPlaceHolder, await remotePlaceholder.GetPlaceholder());
                            return;
                        }
                        else
                        {
                            //Backup local file, Dehydrate and update placeholder
                            if (!localPlaceHolder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
                            {
                                await CreatePreviousVersion(fullPath, ctx);
                            }

                            localPlaceHolder.UpdatePlaceholder(await remotePlaceholder.GetPlaceholder(),
                                CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC | CF_UPDATE_FLAGS.CF_UPDATE_FLAG_DEHYDRATE).ThrowOnFailure();

                            return;
                        }
                    }
                }

                // Dehydration requested
                if (localPlaceHolder.PlaceholderInfoStandard.PinState == CF_PIN_STATE.CF_PIN_STATE_UNPINNED)
                {
                    await DehydratePlaceholder(localPlaceHolder, await remotePlaceholder.GetPlaceholder(), ctx);
                    return;
                }

                // Hydration requested
                if (localPlaceHolder.PlaceholderInfoStandard.PinState == CF_PIN_STATE.CF_PIN_STATE_PINNED && localPlaceHolder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
                {
                    HydratePlaceholder(localPlaceHolder, await remotePlaceholder.GetPlaceholder());
                    return;
                }

                // local file not fully populated and out of sync -> Update and Dehydrate
                if (localPlaceHolder.PlaceholderInfoStandard.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC &&
                    localPlaceHolder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
                {
                    localPlaceHolder.UpdatePlaceholder(await remotePlaceholder.GetPlaceholder(),
                        CF_UPDATE_FLAGS.CF_UPDATE_FLAG_DEHYDRATE | CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC).ThrowOnFailure();

                    return;
                }

                // Info if placeholder is still not in sync
                // TODO: Retry at a later time.
                if (localPlaceHolder.PlaceholderInfoStandard.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC)
                {
                    Styletronix.Debug.WriteLine("Not in Sync after processing: " + fullPath, System.Diagnostics.TraceLevel.Warning);

                    unchecked
                    {
                        throw new System.ComponentModel.Win32Exception((int)NtStatus.STATUS_CLOUD_FILE_NOT_IN_SYNC, "Not in Sync after processing: " + fullPath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Styletronix.Debug.WriteLine(ex.Message, System.Diagnostics.TraceLevel.Error);
        }


    }

    private static void ValidateETag(ExtendedPlaceholderState localPlaceHolder, Placeholder remotePlaceholder)
    {
        if (localPlaceHolder?.ETag != remotePlaceholder?.ETag || localPlaceHolder?.PlaceholderInfoStandard.ModifiedDataSize > 0)
        {
            localPlaceHolder?.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC).ThrowOnFailure();
        }
        else
        {
            if (localPlaceHolder?.PlaceholderInfoStandard.ModifiedDataSize == 0)
            {
                localPlaceHolder?.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC).ThrowOnFailure();
            }
        }
    }

    private async Task HydratePlaceholderAsync(ExtendedPlaceholderState localPlaceHolder, Placeholder remotePlaceholder)
    {
        if (localPlaceHolder.PlaceholderInfoStandard.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC)
        {
            // Local File in Sync: Hydrate....
            (await localPlaceHolder.HydratePlaceholderAsync()).ThrowOnFailure();
        }
        else
        {
            bool pinned = (localPlaceHolder.PlaceholderInfoStandard.PinState == CF_PIN_STATE.CF_PIN_STATE_PINNED);
            if (pinned)
            {
                localPlaceHolder.SetPinState(CF_PIN_STATE.CF_PIN_STATE_UNSPECIFIED);
            }

            // Local File not in Sync: Update placeholder, dehydrate, hydrate....
            GenericResult updateResult = localPlaceHolder.UpdatePlaceholder(remotePlaceholder, CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC | CF_UPDATE_FLAGS.CF_UPDATE_FLAG_DEHYDRATE);

            if (pinned)
            {
                localPlaceHolder.SetPinState(CF_PIN_STATE.CF_PIN_STATE_PINNED);
            }

            updateResult.ThrowOnFailure();

            (await localPlaceHolder.HydratePlaceholderAsync()).ThrowOnFailure();
        }
    }
    private void HydratePlaceholder(ExtendedPlaceholderState localPlaceHolder, Placeholder remotePlaceholder)
    {
        if (localPlaceHolder.PlaceholderInfoStandard.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC)
        {
            // Local File in Sync: Hydrate....
            localPlaceHolder.HydratePlaceholder().ThrowOnFailure();
        }
        else
        {
            bool pinned = (localPlaceHolder.PlaceholderInfoStandard.PinState == CF_PIN_STATE.CF_PIN_STATE_PINNED);
            if (pinned)
            {
                localPlaceHolder.SetPinState(CF_PIN_STATE.CF_PIN_STATE_UNSPECIFIED);
            }

            // Local File not in Sync: Update placeholder, dehydrate, hydrate....
            GenericResult updateResult = localPlaceHolder.UpdatePlaceholder(remotePlaceholder, CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC | CF_UPDATE_FLAGS.CF_UPDATE_FLAG_DEHYDRATE);

            if (pinned)
            {
                localPlaceHolder.SetPinState(CF_PIN_STATE.CF_PIN_STATE_PINNED);
            }

            updateResult.ThrowOnFailure();

            localPlaceHolder.HydratePlaceholder().ThrowOnFailure();
        }
    }

    private async Task DehydratePlaceholder(ExtendedPlaceholderState localPlaceHolder, Placeholder remotePlaceholder, CancellationToken ctx)
    {
        if (localPlaceHolder.PlaceholderInfoStandard.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC)
        {
            // Local file in Sync: Dehydrate
            localPlaceHolder.DehydratePlaceholder(false).ThrowOnFailure();
        }
        else
        {
            if (localPlaceHolder.LastWriteTime <= remotePlaceholder.LastWriteTime)
            {
                // Local file older: Dehydrate and update MetaData
                localPlaceHolder.UpdatePlaceholder(remotePlaceholder,
                    CF_UPDATE_FLAGS.CF_UPDATE_FLAG_DEHYDRATE |
                    CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC).ThrowOnFailure();
            }
            else
            {
                // Local file newer than Server: Upload, dehydrate, update MetaData
                if (!localPlaceHolder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
                {
                    // Upload if local file is fully available
                    await UploadFileToServer(localPlaceHolder.FullPath, ctx);
                    localPlaceHolder.Reload();
                }

                localPlaceHolder.UpdatePlaceholder(remotePlaceholder,
                     CF_UPDATE_FLAGS.CF_UPDATE_FLAG_VERIFY_IN_SYNC |
                    CF_UPDATE_FLAGS.CF_UPDATE_FLAG_DEHYDRATE |
                    CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC).ThrowOnFailure();
            }
        }

        localPlaceHolder.SetPinState(CF_PIN_STATE.CF_PIN_STATE_UNSPECIFIED);
        return;
    }

    public Task CreatePreviousVersion(string fullPath, CancellationToken ctx)
    {
        //TODO: Implement CreatePreviousVersion
        Styletronix.Debug.WriteLine("TODO: Implement CreatePreviousVersion: " + fullPath, System.Diagnostics.TraceLevel.Info);

        return Task.CompletedTask;
    }
    public bool IsExcludedFile(string relativeOrFullPath)
    {
        foreach (string match in fileExclusions)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(relativeOrFullPath, match, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return true;
        }

        return false;
    }
    public bool IsExcludedFile(string relativeOrFullPath, FileAttributes attributes)
    {
        if (attributes.HasFlag(FileAttributes.System) || attributes.HasFlag(FileAttributes.Temporary))
            return true;

        return IsExcludedFile(relativeOrFullPath);
    }


    private void MoveToRecycleBin(ExtendedPlaceholderState localPlaceHolder)
    {
        string recyclePath = SyncContext.LocalRootFolder + @"\$Recycle.bin\" + GetRelativePath(localPlaceHolder.FullPath);
        string recycleDirectory = Path.GetDirectoryName(recyclePath);

        if (localPlaceHolder.IsPlaceholder)
        {
            localPlaceHolder.SetPinState(CF_PIN_STATE.CF_PIN_STATE_EXCLUDED);
            localPlaceHolder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC);

            if (localPlaceHolder.IsDirectory)
            {
                // TODO: Delete Directory....
            }
            else
            {
                if (localPlaceHolder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
                {
                    //localPlaceHolder.RevertPlaceholder(true);
                    File.Delete(localPlaceHolder.FullPath);
                }
                else
                {
                    localPlaceHolder.RevertPlaceholder(false).ThrowOnFailure();

                    if (!Directory.Exists(recycleDirectory))
                    {
                        Directory.CreateDirectory(recycleDirectory);
                    }

                    File.Move(localPlaceHolder.FullPath, recyclePath);
                }
            }
        }
    }
    private async Task<WriteFileCloseResult> UploadFileToServer(string fullPath, CancellationToken ctx)
    {
        Styletronix.Debug.WriteLine("Upload File: " + fullPath, System.Diagnostics.TraceLevel.Info);

        string relativePath = GetRelativePath(fullPath);
        int currentChunkSize = GetChunkSize();

        IWriteFileAsync writeFileAsync = SyncContext.ServerProvider.GetNewWriteFile();
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable ignored1 = writeFileAsync.ConfigureAwait(false);

        using FileStream fStream = new(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        // Set to "NOT IN SYNC" to retry uploads if failed
        //SetInSyncState(fStream.SafeFileHandle, CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC);

        Placeholder localPlaceHolder = new(fullPath);

        long currentOffset = 0;
        byte[] buffer = new byte[currentChunkSize];

        using SafeTransferKey TransferKey = new(fStream.SafeFileHandle);

        WriteFileOpenResult openResult = await writeFileAsync.OpenAsync(new OpenAsyncParams()
        {
            RelativeFileName = relativePath,
            FileInfo = localPlaceHolder,
            CancellationToken = ctx,
            mode = UploadMode.FullFile,
            ETag = localPlaceHolder.ETag
        });
        openResult.ThrowOnFailure();

        int readBytes = await fStream.ReadAsync(buffer, 0, currentChunkSize);
        while (readBytes > 0)
        {
            ctx.ThrowIfCancellationRequested();

            ReportProviderProgress(TransferKey, localPlaceHolder.FileSize, currentOffset + readBytes, relativePath);

            WriteFileWriteResult writeResult = await writeFileAsync.WriteAsync(buffer, 0, currentOffset, readBytes);
            writeResult.ThrowOnFailure();

            currentOffset += readBytes;
            readBytes = await fStream.ReadAsync(buffer, 0, currentChunkSize);
        };

        WriteFileCloseResult closeResult = await writeFileAsync.CloseAsync(ctx.IsCancellationRequested == false);
        closeResult.ThrowOnFailure();

        ctx.ThrowIfCancellationRequested();

        // Update local LastWriteTime. Use Server Time to ensure consistent change time.
        if (closeResult.Placeholder != null)
        {
            if (localPlaceHolder.LastWriteTime != closeResult.Placeholder.LastWriteTime)
            {
                Windows.Win32.PInvoke.SetFileTime(fStream.SafeFileHandle, null, null, closeResult.Placeholder.LastWriteTime.ToFileTimeStruct());
            }
        }

        SetInSyncState(fStream.SafeFileHandle, CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC);
        fStream.Close();

        Styletronix.Debug.WriteLine("Upload File Completed: " + fullPath, System.Diagnostics.TraceLevel.Verbose);

        return closeResult;
    }


    //private async Task FindLocalChangedDataRecursive(string folder, CancellationToken ctx, SyncMode syncMode)
    //{
    //    bool fileFound;

    //    if (folder.IndexOf(@"$Recycle.bin" ,StringComparison.CurrentCultureIgnoreCase )>=0)
    //        return;

    //    Styletronix.Debug.WriteLine("FindLocalChangedData: " + folder, System.Diagnostics.TraceLevel.Verbose);

    //    if (syncMode != SyncMode.Local)
    //    {
    //        using ExtendedPlaceholderState pl = new(folder);
    //        pl.EnableOnDemandPopulation();
    //    }

    //    using Kernel32.SafeSearchHandle findHandle = Kernel32.FindFirstFile(@"\\?\" + folder + @"\*", out WIN32_FIND_DATA findData);
    //    fileFound = (findHandle.IsInvalid == false);

    //    while (fileFound)
    //    {
    //        if (findData.cFileName != "." && findData.cFileName != ".." && findData.cFileName != @"$Recycle.bin")
    //        {
    //            using ExtendedPlaceholderState localPlaceholder = new(findData, folder);

    //            if (localPlaceholder.IsDirectory)
    //            {
    //                if (localPlaceholder.IsPlaceholder)
    //                {
    //                    if (!localPlaceholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL) ||
    //                        !localPlaceholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC))
    //                    {
    //                        await FindLocalChangedDataRecursive(folder + "\\" + findData.cFileName, ctx, syncMode);
    //                    }
    //                    else
    //                    {

    //                    }
    //                }
    //                else
    //                {
    //                    AddFileToChangeQueue(folder + "\\" + findData.cFileName);
    //                    await FindLocalChangedDataRecursive(folder + "\\" + findData.cFileName, ctx, syncMode);
    //                }
    //            }
    //            else
    //            {
    //                if (syncMode.HasFlag(SyncMode.Local))
    //                {
    //                    if (!localPlaceholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC))
    //                    {
    //                        AddFileToChangeQueue(folder + "\\" + findData.cFileName);
    //                    }
    //                }
    //                else
    //                {
    //                    AddFileToChangeQueue(folder + "\\" + findData.cFileName);
    //                }
    //            }
    //        }

    //        if (ctx.IsCancellationRequested) { return; }
    //        fileFound = Kernel32.FindNextFile(findHandle, out findData);
    //    }
    //}



    internal string GetRelativePath(string fullPath)
    {
        if (fullPath.Equals(SyncContext.LocalRootFolder, StringComparison.CurrentCultureIgnoreCase)) { return ""; }

        if (fullPath.StartsWith(SyncContext.LocalRootFolder, StringComparison.CurrentCultureIgnoreCase))
        {
            return fullPath.Remove(0, SyncContext.LocalRootFolder.Length + 1);
        }
        else
        {
            return fullPath;
            throw new NotSupportedException("Pad nicht unterstützt: " + fullPath);
        }
    }
    internal string GetRelativePath(in CF_CALLBACK_INFO callbackInfo)
    {
        if (callbackInfo.NormalizedPath.StartsWith(SyncContext.LocalRootFolderNormalized, StringComparison.CurrentCultureIgnoreCase))
        {
            string relativePath = callbackInfo.NormalizedPath.Remove(0, SyncContext.LocalRootFolderNormalized.Length);
            return relativePath.TrimStart(char.Parse("\\"));
        }
        return callbackInfo.NormalizedPath;
    }
    internal string GetRelativePath(in CF_CALLBACK_PARAMETERS.RENAME callbackInfo)
    {
        if (callbackInfo.TargetPath.StartsWith(SyncContext.LocalRootFolderNormalized, StringComparison.CurrentCultureIgnoreCase))
        {
            string relativePath = callbackInfo.TargetPath.Remove(0, SyncContext.LocalRootFolderNormalized.Length);
            return relativePath.TrimStart(char.Parse("\\"));
        }

        return callbackInfo.TargetPath;
    }
    internal string GetLocalFullPath(in CF_CALLBACK_INFO callbackInfo)
    {
        string relativePath = GetRelativePath(callbackInfo);
        return Path.Combine(SyncContext.LocalRootFolder, relativePath);
    }
    internal string GetLocalFullPath(string relativePath)
    {
        return Path.Combine(SyncContext.LocalRootFolder, relativePath);
    }
    internal int GetChunkSize()
    {
        int currentChunkSize = Math.Min(chunkSize, SyncContext.ServerProvider.PreferredServerProviderSettings.MaxChunkSize);
        currentChunkSize = Math.Max(currentChunkSize, SyncContext.ServerProvider.PreferredServerProviderSettings.MinChunkSize);
        return currentChunkSize;
    }


    public void ReportProviderProgress(CF_TRANSFER_KEY transferKey, long total, long completed, string relativePath)
    {
        // Report progress to System
        HRESULT ret = CfReportProviderProgress(SyncContext.ConnectionKey, transferKey, total, completed);
        Styletronix.Debug.LogResponse(ret);


        // Report progress to components
        try
        {
            FileProgressEvent?.Invoke(this, new FileProgressEventArgs(relativePath, completed, total));
        }
        catch (Exception ex)
        {
            Styletronix.Debug.LogException(ex);
        }
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

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                fileRangeManager?.Dispose();
                LocalChangedDataQueue?.Dispose();
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