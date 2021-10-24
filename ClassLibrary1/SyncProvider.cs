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
    private bool _isConnected;

    private CancellationTokenSource ChangedDataCancellationTokenSource;
    private readonly CancellationTokenSource GlobalShutDownTokenSource = new();
    private readonly SyncProviderUtils.SyncContext SyncContext;
    private readonly int chunkSize = 1024 * 1024 * 2; // 10MB chunkSize for File Download / Upload
    private readonly int stackSize = 1024 * 512; // Buffer size for P/Invoke Call to CFExecute max 1 MB
    private bool disposedValue;


    public CancellationToken GlobalShutDownToken => GlobalShutDownTokenSource.Token;


    public SyncProvider(SyncProviderParameters parameter)
    {
        SyncContext = new()
        {
            LocalRootFolder = parameter.LocalDataPath,
            LocalRootFolderNormalized = parameter.LocalDataPath.Remove(0, 2),
            ServerProvider = parameter.ServerProvider,
            SyncProviderParameter = parameter
        };
        SyncContext.ServerProvider.SyncContext = SyncContext;

        RemoteChangesQueue = new(ProcessRemoteFileChanged);
        FetchDataQueue = new();
        FetchDataRunningQueue = new();
        FetchDataWorkerThread = FetchDataWorker();

        DeleteQueue = new(NOTIFY_DELETE_Action);

        SyncContext.ServerProvider.ServerProviderStateChanged += ServerProvider_ServerProviderStateChanged;
        SyncContext.ServerProvider.FileChanged += ServerProvider_FileChanged;
    }





    public string GetSyncRootID()
    {
        string syncRootID = SyncContext.SyncProviderParameter.ProviderInfo.ProviderId.ToString();
        syncRootID += @"!";
        syncRootID += System.Security.Principal.WindowsIdentity.GetCurrent().User.Value; // System.DirectoryServices.AccountManagement.UserPrincipal.Current.Sid.Value;
        syncRootID += @"!";
        syncRootID += SyncContext.LocalRootFolder.GetHashCode();  // Provider Account -> Used Hash of LocalPath asuming that no Account would be synchronized to the same Folder.
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
            StorageProviderHydrationPolicyModifier.StreamingAllowed |
            StorageProviderHydrationPolicyModifier.AllowFullRestartHydration,

            InSyncPolicy = StorageProviderInSyncPolicy.FileLastWriteTime,
            Path = path,
            PopulationPolicy = StorageProviderPopulationPolicy.Full,
            ProtectionMode = StorageProviderProtectionMode.Personal,
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

        Styletronix.Debug.WriteLine("Unregister", System.Diagnostics.TraceLevel.Verbose);

        try
        {
            StorageProviderSyncRootManager.Unregister(GetSyncRootID());
        }
        catch (Exception ex)
        {
            Styletronix.Debug.WriteLine(ex.Message, System.Diagnostics.TraceLevel.Error);
        }


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

    public Task SyncDataAsync(SyncMode syncMode)
    {
        return this.SyncDataAsync(syncMode, "", this.GlobalShutDownToken);
    }
    public Task SyncDataAsync(SyncMode syncMode, string relativePath)
    {
        return this.SyncDataAsync(syncMode, relativePath, this.GlobalShutDownToken);
    }
    public Task SyncDataAsync(SyncMode syncMode, CancellationToken ctx)
    {
        return this.SyncDataAsync(syncMode, "", ctx);
    }
    public async Task SyncDataAsync(SyncMode syncMode, string relativePath, CancellationToken ctx)
    {
        switch (syncMode)
        {
            case SyncMode.Local:
                CfUpdateSyncProviderStatus(SyncContext.ConnectionKey, CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_SYNC_INCREMENTAL);
                break;
            case SyncMode.Full:
                CfUpdateSyncProviderStatus(SyncContext.ConnectionKey, CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_SYNC_FULL);
                break;
        }

        try
        {
            await Task.Run(async () =>
                    {
                        if (relativePath.Length > 0) relativePath = "\\" + relativePath;

                        await FindLocalChangedDataRecursive(SyncContext.LocalRootFolder + relativePath, ctx, syncMode);
                    }, ctx);
        }
        finally
        {
            CfUpdateSyncProviderStatus(SyncContext.ConnectionKey, CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_IDLE);
        }
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
             new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(NOTIFY_DEHYDRATE),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DEHYDRATE
                } ,
             new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(NOTIFY_DEHYDRATE_COMPLETION),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DEHYDRATE_COMPLETION
                } ,
             new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(VALIDATE_DATA),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_VALIDATE_DATA
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
        var connectResult = await this.SyncContext.ServerProvider.Connect();
        Styletronix.Debug.WriteLine("Connect result: " + connectResult.Status.ToString(), System.Diagnostics.TraceLevel.Verbose);


        Styletronix.Debug.WriteLine("Full Sync...", System.Diagnostics.TraceLevel.Info);
        await SyncDataAsync(SyncMode.Full, this.GlobalShutDownToken);

        ret = CfUpdateSyncProviderStatus(SyncContext.ConnectionKey, CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_IDLE);
        if (ret.Succeeded == false) { Styletronix.Debug.WriteLine("Fehler bei CfUpdateSyncProviderStatus: " + ret.ToString(), System.Diagnostics.TraceLevel.Warning); }

        Styletronix.Debug.WriteLine("Ready", System.Diagnostics.TraceLevel.Verbose);
    }
    public async Task Stop()
    {
        this.GlobalShutDownTokenSource.Cancel();

        Styletronix.Debug.WriteLine("Disconnecting....", System.Diagnostics.TraceLevel.Info);
        _ = await this.SyncContext.ServerProvider.Disconnect();

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
    public Task RevertAllPlaceholders()
    {
        return RevertAllPlaceholders(new CancellationToken());
    }
    public async Task RevertAllPlaceholders(CancellationToken ctx)
    {
        Styletronix.Debug.WriteLine("RevertAllPlaceholders", System.Diagnostics.TraceLevel.Verbose);
        StopWatcher();

        Styletronix.Debug.WriteLine("TODO: RevertAllPlaceholders ASYNC", System.Diagnostics.TraceLevel.Info);
        foreach (string item in Directory.EnumerateFileSystemEntries(SyncContext.LocalRootFolder, "*", SearchOption.AllDirectories))
        {
            bool succeeded = false;
            using ExtendedPlaceholderState pl = new(item);
            if (pl.IsPlaceholder)
            {
                if (pl.HydratePlaceholder().Succeeded)
                {
                    pl.SetPinState(CF_PIN_STATE.CF_PIN_STATE_PINNED);
                    if (pl.RevertPlaceholder(false))
                        Styletronix.Debug.WriteLine("RevertPlaceholder OK: " + item, System.Diagnostics.TraceLevel.Verbose);
                }

                if (succeeded == false)
                    Styletronix.Debug.WriteLine("RevertPlaceholder FAILED: " + item, System.Diagnostics.TraceLevel.Warning);
            }
        }

        await Unregister();

        await Task.CompletedTask;
    }



    #region "Monitor and handle local file changes"
    private ActionBlock<string> ChangedDataQueueBlock;

    private void InitWatcher()
    {
        Styletronix.Debug.WriteLine("InitWatcher", System.Diagnostics.TraceLevel.Verbose);

        StopWatcher();

        this.ChangedDataCancellationTokenSource = new CancellationTokenSource();
        this.ChangedDataQueueBlock = new(ProcessFileChanged);
        this.ChangedDataCancellationTokenSource.Token.Register(() => this.ChangedDataQueueBlock.Complete());
        this.GlobalShutDownToken.Register(() => this.ChangedDataCancellationTokenSource.Cancel());


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
        if (this.ChangedDataCancellationTokenSource != null)
            this.ChangedDataCancellationTokenSource.Cancel();

        if (watcher != null)
        {
            Styletronix.Debug.WriteLine("StopWatcher", System.Diagnostics.TraceLevel.Verbose);
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
    }

    private void FileSystemWatcher_OnError(object sender, ErrorEventArgs e)
    {
        Styletronix.Debug.WriteLine("FileSystemWatcher Error: " + e.GetException().Message, System.Diagnostics.TraceLevel.Warning);

        _ = this.SyncDataAsync(SyncMode.Local, this.GlobalShutDownToken);
    }
    private void FileSystemWatcher_OnChanged(object sender, FileSystemEventArgs e)
    {
        ChangedDataQueueBlock.Post(e.FullPath);
    }

    private async Task ProcessFileChanged(string path)
    {
        try
        {
            await ProcessChangedDataAsync(path, ChangedDataCancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Styletronix.Debug.WriteLine("TODO: Exception Handling required: " + path + " " + ex.Message, System.Diagnostics.TraceLevel.Error);
        }
    }

    private async Task ProcessChangedDataAsync(string fullPath, CancellationToken ctx)
    {
        // Ignore deleted Files
        if (!FileOrDirectoryExists(fullPath))
            return;

        var relativePath = GetRelativePath(fullPath);
        using ExtendedPlaceholderState localPlaceHolder = new(fullPath);

        // Convert to placeholder if required
        if (!localPlaceHolder.ConvertToPlaceholder()) { throw new Exception("Convert to Placeholder failed"); };

        // Ignore all Files in $Recycle.bin
        if (fullPath.Contains(@"$Recycle.bin"))
        {
            localPlaceHolder.SetPinState(CF_PIN_STATE.CF_PIN_STATE_EXCLUDED);
        }


        if (localPlaceHolder.PlaceholderInfoBasic.PinState == CF_PIN_STATE.CF_PIN_STATE_EXCLUDED)
            return;


        // Get ServerInfo
        GetFileInfoResult getFileResult = await SyncContext.ServerProvider.GetFileInfo(GetRelativePath(fullPath), localPlaceHolder.IsDirectory);

        // Handle new local file or file on server deleted
        if (getFileResult.Status == NtStatus.STATUS_NOT_A_CLOUD_FILE)
        {
            // File not found on Server.... New local file or File deleted on Server.
            // Do not raise any exception and continue processing
        }
        else
        {
            getFileResult.ThrowOnFailure();
        }


        if (localPlaceHolder.IsDirectory)
        {
            if (getFileResult.Placeholder == null || getFileResult.Status == NtStatus.STATUS_NOT_A_CLOUD_FILE)
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

                    var creatResult = await SyncContext.ServerProvider.CreateFileAsync(relativePath, true);
                    creatResult.ThrowOnFailure();

                    localPlaceHolder.UpdatePlaceholder(creatResult.Placeholder,
                           CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC).ThrowOnFailure();

                    return;
                }
            }
        }
        else
        {
            // New local file or remote deleted File
            if (getFileResult.Placeholder == null || getFileResult.Status == NtStatus.STATUS_NOT_A_CLOUD_FILE)
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
            ValidateETag(localPlaceHolder, getFileResult.Placeholder);


            // local file full populated and out of sync
            if (localPlaceHolder.PlaceholderInfoBasic.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC &&
                !localPlaceHolder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
            {
                // Local File changed: Upload to Server
                if (localPlaceHolder.LastWriteTime > getFileResult.Placeholder.LastWriteTime)
                {
                    await UploadFileToServer(fullPath, ctx);
                    localPlaceHolder.Reload();
                }
                else
                {
                    // Local File requires update...
                    if (localPlaceHolder.PlaceholderInfoBasic.PinState == CF_PIN_STATE.CF_PIN_STATE_PINNED)
                    {
                        HydratePlaceholder(localPlaceHolder, getFileResult.Placeholder);
                        return;
                    }
                    else
                    {
                        //Backup local file, Dehydrate and update placeholder
                        if (!localPlaceHolder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
                        {
                            await CreatePreviousVersion(fullPath, ctx);
                        }

                        localPlaceHolder.UpdatePlaceholder(getFileResult.Placeholder,
                            CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC | CF_UPDATE_FLAGS.CF_UPDATE_FLAG_DEHYDRATE).ThrowOnFailure();

                        return;
                    }
                }
            }

            // Dehydration requested
            if (localPlaceHolder.PlaceholderInfoBasic.PinState == CF_PIN_STATE.CF_PIN_STATE_UNPINNED)
            {
                await DehydratePlaceholder(localPlaceHolder, getFileResult.Placeholder, ctx);
                return;
            }

            // Hydration requested
            if (localPlaceHolder.PlaceholderInfoBasic.PinState == CF_PIN_STATE.CF_PIN_STATE_PINNED && localPlaceHolder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
            {
                HydratePlaceholder(localPlaceHolder, getFileResult.Placeholder);
                return;
            }

            // local file not fully populated and out of sync -> Update and Dehydrate
            if (localPlaceHolder.PlaceholderInfoBasic.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC &&
                localPlaceHolder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
            {
                localPlaceHolder.UpdatePlaceholder(getFileResult.Placeholder,
                    CF_UPDATE_FLAGS.CF_UPDATE_FLAG_DEHYDRATE | CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC).ThrowOnFailure();

                return;
            }

            // Info if placeholder is still not in sync
            // TODO: Retry at a later time.
            if (localPlaceHolder.PlaceholderInfoBasic.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC)
            {
                Styletronix.Debug.WriteLine("Not in Sync after processing: " + fullPath, System.Diagnostics.TraceLevel.Warning);

                unchecked
                {
                    throw new System.ComponentModel.Win32Exception((int)NtStatus.STATUS_CLOUD_FILE_NOT_IN_SYNC, "Not in Sync after processing: " + fullPath);
                }
            }
        }
    }

    private static void ValidateETag(ExtendedPlaceholderState localPlaceHolder, Placeholder remotePlaceholder)
    {
        if (localPlaceHolder.ETag != remotePlaceholder.ETag)
        {
            localPlaceHolder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC).ThrowOnFailure();
        }
        else
        {
            if (localPlaceHolder.PlaceholderInfoStandard.ModifiedDataSize == 0)
                localPlaceHolder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC).ThrowOnFailure();
        }
    }

    private static void HydratePlaceholder(ExtendedPlaceholderState localPlaceHolder, Placeholder remotePlaceholder)
    {
        if (localPlaceHolder.PlaceholderInfoBasic.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC)
        {
            // Local File in Sync: Hydrate....
            localPlaceHolder.HydratePlaceholder().ThrowOnFailure();
        }
        else
        {
            bool pinned = (localPlaceHolder.PlaceholderInfoBasic.PinState == CF_PIN_STATE.CF_PIN_STATE_PINNED);
            if (pinned)
                localPlaceHolder.SetPinState(CF_PIN_STATE.CF_PIN_STATE_UNSPECIFIED);

            // Local File not in Sync: Update placeholder, dehydrate, hydrate....
            var updateResult = localPlaceHolder.UpdatePlaceholder(remotePlaceholder, CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC | CF_UPDATE_FLAGS.CF_UPDATE_FLAG_DEHYDRATE);

            if (pinned)
                localPlaceHolder.SetPinState(CF_PIN_STATE.CF_PIN_STATE_PINNED);

            updateResult.ThrowOnFailure();

            localPlaceHolder.HydratePlaceholder().ThrowOnFailure();
        }
    }

    private async Task DehydratePlaceholder(ExtendedPlaceholderState localPlaceHolder, Placeholder remotePlaceholder, CancellationToken ctx)
    {
        if (localPlaceHolder.PlaceholderInfoBasic.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC)
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
    #endregion

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

        IWriteFileAsync writeFileAsync = SyncContext.ServerProvider.GetNewWriteFile();
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable ignored1 = writeFileAsync.ConfigureAwait(false);

        using FileStream fStream = new(fullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);

        SetInSyncState(fStream.SafeFileHandle, CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC);

        Placeholder localPlaceHolder = new(fullPath);

        long currentOffset = 0;
        byte[] buffer = new byte[chunkSize];

        using SafeTransferKey TransferKey = new(fStream.SafeFileHandle);

        WriteFileOpenResult openResult = await writeFileAsync.OpenAsync(new OpenAsyncParams()
        {
            RelativeFileName = relativePath,
            FileInfo = localPlaceHolder,
            CancellationToken = ctx,
            mode = UploadMode.FullFile,
            ETag = null // TODO: ETAG hinzufügen
        });
        openResult.ThrowOnFailure();

        int readBytes = await fStream.ReadAsync(buffer, 0, chunkSize);
        while (readBytes > 0)
        {
            ctx.ThrowIfCancellationRequested();

            this.ReportProviderProgress(TransferKey, localPlaceHolder.FileSize, currentOffset + readBytes);

            WriteFileWriteResult writeResult = await writeFileAsync.WriteAsync(buffer, 0, currentOffset, readBytes);
            writeResult.ThrowOnFailure();

            currentOffset += readBytes;
            readBytes = await fStream.ReadAsync(buffer, 0, chunkSize);
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

    public enum SyncMode
    {
        Local,
        Full
    }
    private async Task FindLocalChangedDataRecursive(string folder, CancellationToken ctx, SyncMode syncMode)
    {
        bool fileFound;

        if (folder.Contains(@"$Recycle.bin"))
            return;

        Styletronix.Debug.WriteLine("FindLocalChangedData: " + folder, System.Diagnostics.TraceLevel.Verbose);

        if (syncMode != SyncMode.Local)
        {
            using ExtendedPlaceholderState pl = new(folder);
            pl.EnableOnDemandPopulation();
        }

        using Kernel32.SafeSearchHandle findHandle = Kernel32.FindFirstFile(@"\\?\" + folder + @"\*", out WIN32_FIND_DATA findData);
        fileFound = (findHandle.IsInvalid == false);

        while (fileFound)
        {
            if (findData.cFileName != "." && findData.cFileName != ".." && findData.cFileName != @"$Recycle.bin")
            {
                using ExtendedPlaceholderState localPlaceholder = new(findData, folder);

                if (localPlaceholder.IsDirectory)
                {
                    if (localPlaceholder.IsPlaceholder)
                    {
                        if (!localPlaceholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL) ||
                            !localPlaceholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC))
                        {
                            await FindLocalChangedDataRecursive(folder + "\\" + findData.cFileName, ctx, syncMode);
                        }
                        else
                        {

                        }
                    }
                    else
                    {
                        await ChangedDataQueueBlock.SendAsync(folder + "\\" + findData.cFileName);
                        await FindLocalChangedDataRecursive(folder + "\\" + findData.cFileName, ctx, syncMode);
                    }
                }
                else
                {
                    if (syncMode.HasFlag(SyncMode.Local))
                    {
                        if (!localPlaceholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC))
                        {
                            await ChangedDataQueueBlock.SendAsync(folder + "\\" + findData.cFileName);
                        }
                    }
                    else
                    {
                        await ChangedDataQueueBlock.SendAsync(folder + "\\" + findData.cFileName);

                    }
                }
            }

            if (ctx.IsCancellationRequested) { return; }
            fileFound = Kernel32.FindNextFile(findHandle, out findData);
        }
    }







    internal string GetRelativePath(string fullPath)
    {
        if (fullPath.Equals(SyncContext.LocalRootFolder)) { return ""; }

        if (fullPath.StartsWith(SyncContext.LocalRootFolder, StringComparison.CurrentCultureIgnoreCase))
        {
            return fullPath.Remove(0, SyncContext.LocalRootFolder.Length + 1);
        }
        else
        {
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


    #region "Dispose"

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                fileFetchHelper.Dispose();
                //this.FetchDataCancellationTokenSource.Cancel();
                FetchDataQueue.CompleteAdding();
            }

            // TODO: Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer überschreiben
            // TODO: Große Felder auf NULL setzen
            disposedValue = true;
        }
    }

    // // TODO: Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
    // ~FolderProvider()
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
    #endregion
}