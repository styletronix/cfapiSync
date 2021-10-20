using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.PInvoke;
using System.Linq;
using System.IO;
using Styletronix.CloudSyncProvider;
using System.Threading.Tasks;
using System.Threading;
using static Styletronix.CloudFilterApi.SafeHandlers;
using static Styletronix.CloudFilterApi;
using static Vanara.PInvoke.CldApi;
using Windows.Storage.Provider;
using System.Threading.Tasks.Dataflow;

public class SyncProviderUtils
{
    //public static Ole32.PROPERTYKEY PKEY_StorageProviderTransferProgress => new Ole32.PROPERTYKEY(new Guid("{e77e90df-6271-4f5b-834f-2dd1f245dda4}"), 4);

    public static void ApplyTransferStateToFile(in CF_CONNECTION_KEY connectionKey, in CF_TRANSFER_KEY transferKey, long total, long completed)
    {
        Styletronix.Debug.LogResponse(CfReportProviderProgress(connectionKey, transferKey, total, completed));
    }


    public class DataActions
    {
        public long FileOffset;
        public long Length;
        public string NormalizedPath;
        public CF_TRANSFER_KEY TransferKey;
        public CF_REQUEST_KEY RequestKey;
        public byte PriorityHint;
        public CancellationTokenSource CancellationTokenSource;
        public Guid guid = Guid.NewGuid();

        public bool isCompleted;

        public string Id;
        //public CldApi.CF_CALLBACK_INFO CallbackInfo;
        //public CldApi.CF_CALLBACK_PARAMETERS CallbackParameters;
    }
    public class FetchRange
    {
        public FetchRange() { }
        public FetchRange(DataActions data)
        {
            this.NormalizedPath = data.NormalizedPath;
            this.PriorityHint = data.PriorityHint;
            this.RangeStart = data.FileOffset;
            this.RangeEnd = data.FileOffset + data.Length;
            this.TransferKey = data.TransferKey;
        }

        public long RangeStart;
        public long RangeEnd;
        public string NormalizedPath;
        public CF_TRANSFER_KEY TransferKey;
        public byte PriorityHint;
    }
    public class SyncContext
    {
        /// <summary>
        /// Absolute Path to the local Root Folder where the cached files are stored.
        /// </summary>
        public string LocalRootFolder;

        public string LocalRootFolderNormalized;

        public CF_CONNECTION_KEY ConnectionKey;

        public IServerFileProvider ServerProvider;
        public SyncProviderParameters SyncProviderParameter;
    }


    class SafeAllocCoTaskMem : IDisposable
    {
        private IntPtr _pointer;

        public SafeAllocCoTaskMem(int size)
        {
            this._pointer = Marshal.AllocCoTaskMem(size);
        }
        public static implicit operator IntPtr(SafeAllocCoTaskMem instance)
        {
            return instance._pointer;
        }

        #region "Dispose"

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                }

                Marshal.FreeCoTaskMem(this._pointer);
                disposedValue = true;
            }
        }

        ~SafeAllocCoTaskMem()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}

public class SyncProvider : IDisposable
{
    private CF_CALLBACK_REGISTRATION[] _callbackMappings;
    private bool _isConnected;
    private FileSystemWatcher watcher;

    private CancellationTokenSource ChangedDataCancellationTokenSource;
    private readonly SyncProviderUtils.SyncContext SyncContext;

    private readonly FileFetchHelper2 fileFetchHelper = new();


    public class FileFetchHelper2 : IDisposable
    {
        private readonly object lockObject = new();
        private readonly List<SyncProviderUtils.FetchRange> filesToProcess = new();
        //private readonly AutoResetEvent _waitForNewItem = new(false);
        private readonly AutoResetEventAsync autoResetEventAsync = new AutoResetEventAsync();
        private readonly CancellationTokenSource cancellationToken = new();

        public FileFetchHelper2()
        {
            //cancellationToken.Token.Register(() => _waitForNewItem.Close());
        }

        //public AutoResetEvent Waiter => this._waitForNewItem;
        public CancellationToken CancellationToken { get { return cancellationToken.Token; } }
        public async Task<SyncProviderUtils.FetchRange> WaitTakeNextAsync()
        {
            var x = this.TakeNext();

            if (x == null)
            {
                //   _waitForNewItem.WaitOne();
                await autoResetEventAsync.WaitAsync(this.CancellationToken);
                lock (lockObject)
                {
                    //_waitForNewItem.Reset();
                    var t = TakeNext();
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
                ret = this.filesToProcess.OrderByDescending(a => a.PriorityHint)
                    .OrderBy(a => a.RangeStart).FirstOrDefault();

            }

            return ret;
        }
        public SyncProviderUtils.FetchRange TakeNext(string normalizedPath)
        {
            SyncProviderUtils.FetchRange ret = null;

            lock (lockObject)
            {
                ret = this.filesToProcess.Where(a => a.NormalizedPath == normalizedPath)
                    .OrderBy(a => a.RangeStart).FirstOrDefault();
            }

            return ret;
        }


        public void Add(SyncProviderUtils.DataActions data)
        {
            var rangeStart = data.FileOffset;
            var rangeEnd = data.FileOffset + data.Length;
            List<SyncProviderUtils.FetchRange> removeItems = new();
            List<SyncProviderUtils.FetchRange> addItems = new();

            lock (lockObject)
            {
                this.filesToProcess.Add(new SyncProviderUtils.FetchRange(data));
                //var item = this.filesToProcess.Where(a => a.NormalizedPath == data.NormalizedPath && a.RangeStart <= rangeEnd && a.RangeEnd >= rangeStart).OrderBy(a => a.RangeStart).FirstOrDefault();
                //if (item != null)
                //{
                //    item.RangeStart = Math.Min(item.RangeStart, rangeStart);
                //    item.RangeEnd = Math.Min(item.RangeEnd, rangeEnd);
                //}
                //else
                //{

                //}

                this.Combine(data.NormalizedPath);
            }


            this.autoResetEventAsync.Set();
            //this._waitForNewItem.Set();
        }
        private void Combine(string normalizedPath)
        {
            bool exitLoop = false;
            while (!exitLoop)
            {
                exitLoop = true;

                foreach (var item2 in this.filesToProcess.Where(a => a.NormalizedPath == normalizedPath).OrderBy(a => a.RangeStart))
                {
                    var item3 = this.filesToProcess.Where(a => a.NormalizedPath == normalizedPath && a.RangeStart <= item2.RangeEnd && a.RangeEnd >= item2.RangeStart && a != item2).OrderBy(a => a.RangeStart).FirstOrDefault();
                    if (item3 != null)
                    {
                        item2.RangeStart = Math.Min(item2.RangeStart, item3.RangeStart);
                        item2.RangeEnd = Math.Min(item2.RangeEnd, item3.RangeEnd);
                        this.filesToProcess.Remove(item3);

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
                var rangeStart = data.FileOffset;
                var rangeEnd = data.FileOffset + data.Length;

                foreach (var item in this.filesToProcess.Where(a => a.NormalizedPath == data.NormalizedPath && a.RangeStart <= rangeEnd && a.RangeEnd >= rangeStart).OrderBy(a => a.RangeStart))
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

                foreach (var item in removeItems)
                {
                    this.filesToProcess.Remove(item);
                }
                foreach (var item in addItems)
                {
                    this.filesToProcess.Add(item);
                }

                this.Combine(data.NormalizedPath);
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
                    this.cancellationToken.Cancel();
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


    public SyncProvider(SyncProviderParameters parameter)
    {
        this.SyncContext = new SyncProviderUtils.SyncContext()
        {
            LocalRootFolder = parameter.LocalDataPath,
            LocalRootFolderNormalized = parameter.LocalDataPath.Remove(0, 2),
            ServerProvider = parameter.ServerProvider,
            SyncProviderParameter = parameter
        };
        this.SyncContext.ServerProvider.SyncContext = this.SyncContext;

        this.FetchDataQueue = new System.Collections.Concurrent.BlockingCollection<SyncProviderUtils.DataActions>();
        this.FetchDataRunningQueue = new System.Collections.Concurrent.ConcurrentDictionary<Guid, SyncProviderUtils.DataActions>();
        this.FetchDataWorkerThread = FetchDataWorker();

        this.DeleteQueue = new(NOTIFY_DELETE_Action);
    }

    public string GetSyncRootID()
    {
        string syncRootID = this.SyncContext.SyncProviderParameter.ProviderInfo.ProviderId.ToString();
        syncRootID += @"!";
        syncRootID += System.Security.Principal.WindowsIdentity.GetCurrent().User.Value; // System.DirectoryServices.AccountManagement.UserPrincipal.Current.Sid.Value;
        syncRootID += @"!";
        syncRootID += this.SyncContext.LocalRootFolder.GetHashCode();  // Provider Account -> Used Hash of LocalPath asuming that no Account would be synchronized to the same Folder.
        return syncRootID;
    }

    public async Task Register()
    {
        if (StorageProviderSyncRootManager.IsSupported() == false)
        {
            Styletronix.Debug.WriteLine("OS not supported!");
            throw new NotSupportedException();
        }

        var path = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(this.SyncContext.LocalRootFolder);

        var SyncRootInfo = new StorageProviderSyncRootInfo
        {
            Id = GetSyncRootID(),
            AllowPinning = true,
            DisplayNameResource = this.SyncContext.SyncProviderParameter.ProviderInfo.ProviderName,
            HardlinkPolicy = StorageProviderHardlinkPolicy.None,
            HydrationPolicy = StorageProviderHydrationPolicy.Partial,

            HydrationPolicyModifier = StorageProviderHydrationPolicyModifier.AutoDehydrationAllowed |
            StorageProviderHydrationPolicyModifier.StreamingAllowed |
            StorageProviderHydrationPolicyModifier.AllowFullRestartHydration,

            InSyncPolicy = StorageProviderInSyncPolicy.FileLastWriteTime,
            Path = path,
            PopulationPolicy = StorageProviderPopulationPolicy.Full,
            ProtectionMode = StorageProviderProtectionMode.Personal,
            ProviderId = this.SyncContext.SyncProviderParameter.ProviderInfo.ProviderId,
            Version = this.SyncContext.SyncProviderParameter.ProviderInfo.ProviderVersion,
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

    public void Unregister()
    {
        Stop();

        Styletronix.Debug.WriteLine("Unregister");

        try
        {
            StorageProviderSyncRootManager.Unregister(this.GetSyncRootID());
        }
        catch (Exception ex)
        {
            Styletronix.Debug.WriteLine(ex.Message);
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
    public async Task SyncDataAsync(SyncMode syncMode, CancellationToken ctx)
    {
        switch (syncMode)
        {
            case SyncMode.Local:
                CfUpdateSyncProviderStatus(this.SyncContext.ConnectionKey, CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_SYNC_INCREMENTAL);
                break;
            case SyncMode.Full:
                CfUpdateSyncProviderStatus(this.SyncContext.ConnectionKey, CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_SYNC_FULL);
                break;
        }

        try
        {
            await Task.Run(async () =>
                    {
                        await FindLocalChangedDataRecursive(this.SyncContext.LocalRootFolder, ctx, syncMode);
                    }, ctx);
        }
        finally
        {
            CfUpdateSyncProviderStatus(this.SyncContext.ConnectionKey, CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_IDLE);
        }
    }
    public async Task Start(CancellationToken ctx)
    {
        if (Directory.Exists(this.SyncContext.LocalRootFolder) == false)
        {
            Directory.CreateDirectory(this.SyncContext.LocalRootFolder);
        }

        Styletronix.Debug.WriteLine("Register");

        await Register();

        Styletronix.Debug.WriteLine("Connect");

        this._callbackMappings = new CF_CALLBACK_REGISTRATION[]
      {
          new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(this.FETCH_PLACEHOLDERS),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_FETCH_PLACEHOLDERS
                } ,
           new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(this.CANCEL_FETCH_PLACEHOLDERS),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_CANCEL_FETCH_PLACEHOLDERS
                } ,
          new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(this.FETCH_DATA),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_FETCH_DATA
                } ,
          new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(this.CANCEL_FETCH_DATA),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_CANCEL_FETCH_DATA
                } ,
          new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(this.NOTIFY_FILE_OPEN_COMPLETION),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_FILE_OPEN_COMPLETION
                } ,
          new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(this.NOTIFY_FILE_CLOSE_COMPLETION),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_FILE_CLOSE_COMPLETION
                } ,
           new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(this.NOTIFY_DELETE),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DELETE
                } ,
           new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(this.NOTIFY_DELETE_COMPLETION),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DELETE_COMPLETION
                } ,
           new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(this.NOTIFY_RENAME),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_RENAME
                } ,
            new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(this.NOTIFY_RENAME_COMPLETION),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_RENAME_COMPLETION
                } ,
             new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(this.NOTIFY_DEHYDRATE),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DEHYDRATE
                } ,
             new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(this.NOTIFY_DEHYDRATE_COMPLETION),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DEHYDRATE_COMPLETION
                } ,
             new CF_CALLBACK_REGISTRATION {
                    Callback = new CF_CALLBACK(this.VALIDATE_DATA),
                    Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_VALIDATE_DATA
                } ,
           CF_CALLBACK_REGISTRATION.CF_CALLBACK_REGISTRATION_END
      };

        this.SyncContext.ConnectionKey = default;

        HRESULT ret = CfConnectSyncRoot(this.SyncContext.LocalRootFolder, _callbackMappings,
            IntPtr.Zero,
            CF_CONNECT_FLAGS.CF_CONNECT_FLAG_REQUIRE_PROCESS_INFO |
            CF_CONNECT_FLAGS.CF_CONNECT_FLAG_REQUIRE_FULL_FILE_PATH,
            out this.SyncContext.ConnectionKey);

        if (ret.Succeeded)
        {
            _isConnected = true;
            Styletronix.Debug.WriteLine("Connected");
        }
        else
        {
            Styletronix.Debug.WriteLine("Connection failed!");
        }
        ret.ThrowIfFailed();


        InitWatcher();

        Styletronix.Debug.WriteLine("Full Sync...");
        await SyncDataAsync(SyncMode.Full, ctx);

        ret = CfUpdateSyncProviderStatus(this.SyncContext.ConnectionKey, CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_IDLE);
        if (ret.Succeeded == false) { Styletronix.Debug.WriteLine("Fehler bei CfUpdateSyncProviderStatus: " + ret.ToString()); }

        Styletronix.Debug.WriteLine("Ready");
    }
    public void Stop()
    {
        if (this._isConnected == false) { return; }

        HRESULT ret = CfDisconnectSyncRoot(this.SyncContext.ConnectionKey);
        if (ret.Succeeded)
        {
            this._isConnected = false;
        }
        Styletronix.Debug.WriteLine("DisconnectSyncRoot: " + ret.ToString());

        CfUpdateSyncProviderStatus(this.SyncContext.ConnectionKey, CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_TERMINATED);
    }

    public void DeleteLocalData()
    {
        this.Stop();

        HRESULT ret = CfUnregisterSyncRoot(this.SyncContext.LocalRootFolder);
        Styletronix.Debug.WriteLine("UnregisterSyncRoot: " + ret.ToString());

        try
        {
            if (Directory.Exists(this.SyncContext.LocalRootFolder))
            {
                Directory.Delete(this.SyncContext.LocalRootFolder, true);
            }
        }
        catch (Exception ex)
        {
            Styletronix.Debug.WriteLine("Delete Root Folder: " + ex.ToString());
        }
    }
    public async Task RevertAllPlaceholders(CancellationToken ctx)
    {
        Styletronix.Debug.WriteLine("RevertAllPlaceholders");
        this.StopWatcher();

        Styletronix.Debug.WriteLine("TODO: RevertAllPlaceholders ASYNC");
        foreach (var item in Directory.EnumerateFileSystemEntries(this.SyncContext.LocalRootFolder, "*", SearchOption.AllDirectories))
        {
            bool succeeded = false;
            var pl = new ExtendedPlaceholderState(item);
            if (pl.isPlaceholder)
            {
                if (pl.HydratePlaceholder().Succeeded)
                {
                    pl.SetPinState(CF_PIN_STATE.CF_PIN_STATE_PINNED);
                    if (pl.RevertPlaceholder(false))
                    {
                        Styletronix.Debug.WriteLine("RevertPlaceholder OK: " + item);
                    }
                }

                if (succeeded == false)
                {
                    Styletronix.Debug.WriteLine("RevertPlaceholder FAILED: " + item);
                }
            }
        }

        this.Unregister();

        await Task.CompletedTask;
    }



    #region "Monitor and handle local file changes"
    private ActionBlock<string> ChangedDataQueueBlock;

    private void InitWatcher()
    {
        Styletronix.Debug.WriteLine("InitWatcher");

        StopWatcher();

        this.ChangedDataCancellationTokenSource = new CancellationTokenSource();
        this.ChangedDataQueueBlock = new(ProcessFileChanged);
        this.ChangedDataCancellationTokenSource.Token.Register(() => this.ChangedDataQueueBlock.Complete());


        watcher = new FileSystemWatcher
        {
            Path = this.SyncContext.LocalRootFolder,
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
        if (watcher != null)
        {
            Styletronix.Debug.WriteLine("StopWatcher");
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        if (this.ChangedDataCancellationTokenSource != null)
        {
            this.ChangedDataCancellationTokenSource.Cancel();
        }
    }

    private void FileSystemWatcher_OnError(object sender, ErrorEventArgs e)
    {
        //TODO: Local Sync
        Styletronix.Debug.WriteLine("FileSystemWatcher_OnError");
        Styletronix.Debug.WriteLine(e.GetException().Message);
    }
    private void FileSystemWatcher_OnChanged(object sender, FileSystemEventArgs e)
    {
        //Debug.WriteLine("FileSystemWatcher_OnChanged: " + e.FullPath);
        this.ChangedDataQueueBlock.Post(e.FullPath);
        //this.ChangedDataQueue.Add(e);
    }

    private async Task ProcessFileChanged(string path)
    {
        try
        {
            await ProcessChangedDataAsync(path, this.ChangedDataCancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Styletronix.Debug.WriteLine("TODO: Exception Handling required: " + path + " " + ex.Message);
        }
    }

    private async Task ProcessChangedDataAsync(string fullPath, CancellationToken ctx)
    {
        // Ignore all Files in $Recycle.bin
        if (fullPath.Contains(@"$Recycle.bin"))
            return;

        using ExtendedPlaceholderState localPlaceHolder = new(fullPath);

        var relativePath = GetRelativePath(fullPath);
        string fileIdString = relativePath;
        if (localPlaceHolder.isDirectory) { fileIdString += "\\"; }


        // Convert to placeholder if required
        if (!localPlaceHolder.ConvertToPlaceholder(fileIdString)) { throw new Exception("Convert to Placeholder failed"); };

        // Get ServerInfo
        var getFileResult = await this.SyncContext.ServerProvider.GetFileInfo(GetRelativePath(fullPath), localPlaceHolder.isDirectory);

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


        if (localPlaceHolder.isDirectory)
        {
            if (getFileResult.Placeholder == null || getFileResult.Status == NtStatus.STATUS_NOT_A_CLOUD_FILE)
            {
                // Directory does not exist on Server
                if (localPlaceHolder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC))
                {
                    // Directory remotely deleted if it was in sync.
                    Styletronix.Debug.WriteLine("TODO: Remove local Directory if empty...");
                    //Directory.Delete(localPlaceHolder.FullPath, false);
                    return;
                }
                else
                {
                    // File locally created or modified while deleted on Server
                    Styletronix.Debug.WriteLine("Create Directory on Server: " + relativePath);
                    (await this.SyncContext.ServerProvider.CreateFileAsync(relativePath, true)).ThrowOnFailure(); ;
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
                    var uploadFileToServerResult = await UploadFileToServer(fullPath, ctx);
                    uploadFileToServerResult.ThrowOnFailure();
                    return;
                    //if (uploadFileToServerResult.Placeholder == null) { return; }

                    //getFileResult.Placeholder = uploadFileToServerResult.Placeholder;
                    //localPlaceHolder.Reload();
                }
            }

            // Validate ETag
            if (localPlaceHolder.ETag != getFileResult.Placeholder.ETag)
            {
                localPlaceHolder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC).ThrowOnFailure();
            }


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
                        // TODO: Rehydrate local file

                        localPlaceHolder.SetPinState(CF_PIN_STATE.CF_PIN_STATE_UNSPECIFIED);
                        //localPlaceHolder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC)

                        localPlaceHolder.UpdatePlaceholder(getFileResult.Placeholder, fileIdString,
                            CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC | CF_UPDATE_FLAGS.CF_UPDATE_FLAG_DEHYDRATE);

                        localPlaceHolder.SetPinState(CF_PIN_STATE.CF_PIN_STATE_PINNED);

                        //localPlaceHolder.HydratePlaceholder().ThrowOnFailure();


                        return;
                    }
                    else
                    {
                        //Backup local file, Dehydrate and update placeholder
                        // TODO: Backup....
                        localPlaceHolder.UpdatePlaceholder(getFileResult.Placeholder, fileIdString,
                            CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC | CF_UPDATE_FLAGS.CF_UPDATE_FLAG_DEHYDRATE).ThrowOnFailure();
                        return;
                    }
                }
            }

            // Dehydration requested
            if (localPlaceHolder.PlaceholderInfoBasic.PinState == CF_PIN_STATE.CF_PIN_STATE_UNPINNED)
            {
                if (localPlaceHolder.PlaceholderInfoBasic.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC)
                {
                    // Local file in Sync: Dehydrate
                    localPlaceHolder.DehydratePlaceholder(false).ThrowOnFailure();
                }
                else
                {
                    if (localPlaceHolder.LastWriteTime <= getFileResult.Placeholder.LastWriteTime)
                    {
                        // Local file older: Dehydrate and update MetaData
                        localPlaceHolder.UpdatePlaceholder(getFileResult.Placeholder, fileIdString,
                            CF_UPDATE_FLAGS.CF_UPDATE_FLAG_DEHYDRATE |
                            CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC).ThrowOnFailure();
                    }
                    else
                    {
                        // Local file newer than Server: Upload, dehydrate, update MetaData
                        if (!localPlaceHolder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
                        {
                            // Upload if local file is fully available
                            await UploadFileToServer(fullPath, ctx);
                            localPlaceHolder.Reload();
                        }


                        localPlaceHolder.UpdatePlaceholder(getFileResult.Placeholder, fileIdString,
                             CF_UPDATE_FLAGS.CF_UPDATE_FLAG_VERIFY_IN_SYNC |
                            CF_UPDATE_FLAGS.CF_UPDATE_FLAG_DEHYDRATE |
                            CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC).ThrowOnFailure();
                    }
                }

                localPlaceHolder.SetPinState(CF_PIN_STATE.CF_PIN_STATE_UNSPECIFIED);
                return;
            }

            // Hydration requested
            if (localPlaceHolder.PlaceholderInfoBasic.PinState == CF_PIN_STATE.CF_PIN_STATE_PINNED && localPlaceHolder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
            {
                if (localPlaceHolder.PlaceholderInfoBasic.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC)
                {
                    // Local File in Sync: Hydrate....
                    localPlaceHolder.HydratePlaceholder().ThrowOnFailure();
                    return;
                }
                else
                {
                    // Local File not in Sync: Update placeholder, dehydrate, hydrate....
                    localPlaceHolder.UpdatePlaceholder(getFileResult.Placeholder, fileIdString, CF_UPDATE_FLAGS.CF_UPDATE_FLAG_CLEAR_IN_SYNC).ThrowOnFailure();

                    localPlaceHolder.HydratePlaceholder().ThrowOnFailure();
                    return;
                }
            }

            // local file not populated and out of sync
            if (localPlaceHolder.PlaceholderInfoBasic.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC &&
                localPlaceHolder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
            {
                // Dehydrate outdated placeholder
                localPlaceHolder.UpdatePlaceholder(getFileResult.Placeholder, fileIdString,
                    CF_UPDATE_FLAGS.CF_UPDATE_FLAG_DEHYDRATE | CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC).ThrowOnFailure();

                return;
            }

            //if (localPlaceHolder.PlaceholderInfoBasic.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC)
            //{
            //    if (localPlaceHolder.PlaceholderInfoBasic.PinState == CF_PIN_STATE.CF_PIN_STATE_PINNED)
            //    {
            //        if (localPlaceHolder.LastWriteTime > getFileResult.Placeholder.LastWriteTime)
            //        {

            //        }
            //    }


            //    if (localPlaceHolder.LastWriteTime < getFileResult.Placeholder.LastWriteTime)
            //    {
            //        // TODO: Backup local file if fully Hydrated. File was changed on Server and Client.
            //        localPlaceHolder.UpdatePlaceholder(getFileResult.Placeholder, fileIdString, CF_UPDATE_FLAGS.CF_UPDATE_FLAG_DEHYDRATE | CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC);
            //    }
            //    else if (localPlaceHolder.LastWriteTime > getFileResult.Placeholder.LastWriteTime)
            //    {
            //        // Local file is newer. Upload to Server
            //        await UploadFileToServer(e.FullPath, ctx);
            //        return;
            //    }
            //    else
            //    {
            //        localPlaceHolder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC);
            //    }
            //}


            //if (localPlaceHolder.PlaceholderInfoBasic.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC)
            //{
            //    if (localPlaceHolder.PlaceholderInfoBasic.PinState == CF_PIN_STATE.CF_PIN_STATE_UNPINNED)
            //    {
            //        if (localPlaceHolder.LastWriteTime <= getFileResult.Placeholder.LastWriteTime)
            //        {
            //            localPlaceHolder.DehydratePlaceholder(true);
            //        }
            //        else if (localPlaceHolder.LastWriteTime > getFileResult.Placeholder.LastWriteTime)
            //        {
            //            await UploadFileToServer(e.FullPath, ctx);

            //            if (ctx.IsCancellationRequested) { return; }

            //            // Refresh State
            //            localPlaceHolder.Reload();
            //            if (localPlaceHolder.PlaceholderInfoBasic.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC)
            //            {
            //                localPlaceHolder.DehydratePlaceholder(true);
            //            }
            //        }
            //    }

            //    if (localPlaceHolder.PlaceholderInfoBasic.PinState == CF_PIN_STATE.CF_PIN_STATE_PINNED && localPlaceHolder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
            //    {
            //        localPlaceHolder.HydratePlaceholder(true);
            //    }
            //}

            if (!localPlaceHolder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC))
            {
                Styletronix.Debug.WriteLine("Not in Sync after processing: " + fullPath);
            }
        }
    }
    #endregion

    private void MoveToRecycleBin(ExtendedPlaceholderState localPlaceHolder)
    {
        string recyclePath = this.SyncContext.LocalRootFolder + @"\$Recycle.bin\" + GetRelativePath(localPlaceHolder.FullPath);
        string recycleDirectory = Path.GetDirectoryName(recyclePath);

        //if (!Directory.Exists(recycleDirectory))
        //{
        //    Directory.CreateDirectory(recycleDirectory);
        //    //var recyclePlaceholder = new ExtendedPlaceholderState(recycleDirectory);
        //    //recyclePlaceholder.ConvertToPlaceholder(@"$Recycle.bin");
        //    //recyclePlaceholder.SetPinState(CF_PIN_STATE.CF_PIN_STATE_EXCLUDED);
        //    //recyclePlaceholder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC);
        //}


        if (localPlaceHolder.isPlaceholder)
        {
            localPlaceHolder.SetPinState(CF_PIN_STATE.CF_PIN_STATE_EXCLUDED);
            localPlaceHolder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC);

            if (localPlaceHolder.isDirectory)
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
                        Directory.CreateDirectory(recycleDirectory);

                    File.Move(localPlaceHolder.FullPath, recyclePath);
                }
            }

        }
    }

    private async Task<WriteFileCloseResult> UploadFileToServer(string fullPath, CancellationToken ctx)
    {
        Styletronix.Debug.WriteLine("Upload File: " + fullPath);

        var relativePath = GetRelativePath(fullPath);

        var writeFileAsync = this.SyncContext.ServerProvider.GetNewWriteFile();
        await using var ignored1 = writeFileAsync.ConfigureAwait(false);

        using FileStream fStream = new(fullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);

        SetInSyncState(fStream.SafeFileHandle, CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC);

        Placeholder localPlaceHolder = new(fullPath);

        long currentOffset = 0;
        var buffer = new byte[this.chunkSize];

        using var TransferKey = new SafeTransferKey(fStream.SafeFileHandle);

        var openResult = await writeFileAsync.OpenAsync(new OpenAsyncParams()
        {
            RelativeFileName = relativePath,
            FileInfo = localPlaceHolder,
            CancellationToken = ctx,
            mode = UploadMode.FullFile,
            ETag = null // TODO: ETAG hinzufügen
        });
        openResult.ThrowOnFailure();

        var readBytes = await fStream.ReadAsync(buffer, 0, this.chunkSize);
        while (readBytes > 0)
        {
            ctx.ThrowIfCancellationRequested();

            SyncProviderUtils.ApplyTransferStateToFile(this.SyncContext.ConnectionKey, TransferKey, localPlaceHolder.FileSize, currentOffset + readBytes);

            var writeResult = await writeFileAsync.WriteAsync(buffer, 0, currentOffset, readBytes);
            writeResult.ThrowOnFailure();

            currentOffset += readBytes;
            readBytes = await fStream.ReadAsync(buffer, 0, this.chunkSize);
        };

        var closeResult = await writeFileAsync.CloseAsync(ctx.IsCancellationRequested == false);
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

        Styletronix.Debug.WriteLine("Upload File Completed: " + fullPath);

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

        Styletronix.Debug.WriteLine("FindLocalChangedData: " + folder);


        using var findHandle = Kernel32.FindFirstFile(@"\\?\" + folder + @"\*", out WIN32_FIND_DATA findData);
        fileFound = (findHandle.IsInvalid == false);

        while (fileFound)
        {
            if (findData.cFileName != "." && findData.cFileName != ".." && findData.cFileName != @"$Recycle.bin")
            {
                using ExtendedPlaceholderState localPlaceholder = new(findData, folder);

                if (localPlaceholder.isDirectory)
                {
                    if (!localPlaceholder.isPlaceholder)
                    {
                        await this.ChangedDataQueueBlock.SendAsync(folder + "\\" + findData.cFileName);
                        await FindLocalChangedDataRecursive(folder + "\\" + findData.cFileName, ctx, syncMode);
                    }
                    else if (!localPlaceholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL) && localPlaceholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC))
                    {
                        await FindLocalChangedDataRecursive(folder + "\\" + findData.cFileName, ctx, syncMode);
                    }

                }
                else
                {
                    if (syncMode.HasFlag(SyncMode.Full))
                    {
                        await this.ChangedDataQueueBlock.SendAsync(folder + "\\" + findData.cFileName);
                    }
                    else
                    {
                        if (!localPlaceholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC))
                        {
                            await this.ChangedDataQueueBlock.SendAsync(folder + "\\" + findData.cFileName);
                        }
                    }
                }
            }

            if (ctx.IsCancellationRequested) { return; }
            fileFound = Kernel32.FindNextFile(findHandle, out findData);
        }

        if (syncMode == SyncMode.Full)
        {
            var pl = new ExtendedPlaceholderState(folder);
            pl.EnableOnDemandPopulation(GetRelativePath(folder) + "\\");
        }
    }




    private int chunkSize = 1024 * 1024 * 2; // 10MB chunkSize for File Download / Upload
    private int stackSize = 1024 * 512; // Buffer size for P/Invoke Call to CFExecute max 1 MB
    private bool disposedValue;

    private System.Collections.Concurrent.BlockingCollection<SyncProviderUtils.DataActions> FetchDataQueue;
    private System.Collections.Concurrent.ConcurrentDictionary<Guid, SyncProviderUtils.DataActions> FetchDataRunningQueue;
    private Task FetchDataWorkerThread;
    private System.Collections.Concurrent.ConcurrentDictionary<string, CancellationTokenSource> FetchPlaceholdersCancellationTokens = new();

    private Task FetchDataWorker()
    {
        return Task.Factory.StartNew(async () =>
        {
            while (!this.fileFetchHelper.CancellationToken.IsCancellationRequested)
            {
                var item = await this.fileFetchHelper.WaitTakeNextAsync();
                if (this.fileFetchHelper.CancellationToken.IsCancellationRequested) { break; }
                if (item != null)
                {
                    try
                    {
                        FetchDataAsync(item).Wait(this.fileFetchHelper.CancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Styletronix.Debug.WriteLine(ex.ToString());
                    }
                }
            }
        }, this.fileFetchHelper.CancellationToken, TaskCreationOptions.LongRunning |
        TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Default);
    }


    private async Task FetchDataAsync(SyncProviderUtils.FetchRange data)
    {
        string relativePath = data.NormalizedPath.Remove(0, this.SyncContext.LocalRootFolderNormalized.Length).TrimStart(char.Parse("\\"));
        string targetFullPath = Path.Combine(this.SyncContext.LocalRootFolder, relativePath);

        NTStatus CompletionStatus = NTStatus.STATUS_SUCCESS;

        Styletronix.Debug.WriteLine("Fetch DataRange " + data.RangeStart + @" - " + data.RangeEnd + @" / " + relativePath);

        try
        {
            var ctx = new CancellationTokenSource().Token; // data.CancellationTokenSource.Token;
            if (ctx.IsCancellationRequested) { return; }

            var opInfo = new CF_OPERATION_INFO()
            {
                Type = CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_DATA,
                ConnectionKey = this.SyncContext.ConnectionKey,
                TransferKey = data.TransferKey,
                RequestKey = new CF_REQUEST_KEY()
            };
            opInfo.StructSize = (uint)Marshal.SizeOf(opInfo);

            if (relativePath.Contains(@"$Recycle.bin"))
            {
                var TpParam = new CF_OPERATION_PARAMETERS.TRANSFERDATA
                {
                    Length = 1, // Length has to be greater than 0 even if transfer failed....
                    Offset = data.RangeStart,
                    Buffer = IntPtr.Zero,
                    Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE,
                    CompletionStatus = new NTStatus((uint)NtStatus.STATUS_NOT_A_CLOUD_FILE)
                };
                var opParams = CF_OPERATION_PARAMETERS.Create(TpParam);
                Styletronix.Debug.LogResponse(CfExecute(opInfo, ref opParams));
                this.fileFetchHelper.Cancel(data.NormalizedPath);
                return;
            }

            using (IReadFileAsync fetchFile = SyncContext.ServerProvider.GetNewReadFile())
            {
                var openAsyncResult = await fetchFile.OpenAsync(new OpenAsyncParams()
                {
                    RelativeFileName = relativePath,
                    CancellationToken = ctx,
                    ETag = null // TODO: ETAG ausfüllen:  ETag = "_" + File.GetLastWriteTimeUtc(fullPath).Ticks + "_" + this.fileStream.Length
                });

                CompletionStatus = new NTStatus((uint)openAsyncResult.Status);
                using ExtendedPlaceholderState localPlaceholder = new(targetFullPath);

                #region "Compare ETag to verify Sync of cloud and local file"
                if (CompletionStatus == NTStatus.STATUS_SUCCESS)
                {
                    if (openAsyncResult.Placeholder?.ETag != localPlaceholder.ETag)
                    {
                        Styletronix.Debug.WriteLine("ETag Validation FAILED: " + relativePath);
                        CompletionStatus = new NTStatus((uint)Styletronix.CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_NOT_IN_SYNC);
                        openAsyncResult.Message = Styletronix.CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_NOT_IN_SYNC.ToString();
                    }
                }
                #endregion

                #region "TODO:  Rehydrate. Current implementation does not work"
                //TODO:  Rehydrate. Current implementation does not work
                //if (CompletionStatus == (uint)Styletronix.CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_NOT_IN_SYNC)
                //{                 
                //    using SafeHandlers.SafeAllocCoTaskMem fileIdPtr = new(relativePath);
                //    using SafeHandlers.SafeAllocCoTaskMem metaPtr = new(Styletronix.CloudFilterApi.CreateFSMetaData(openAsyncResult.Placeholder));

                //    var opInfo2 = new CF_OPERATION_INFO()
                //    {
                //        Type = CF_OPERATION_TYPE.CF_OPERATION_TYPE_RESTART_HYDRATION,
                //        ConnectionKey = this.SyncContext.ConnectionKey,
                //        TransferKey = data.TransferKey,
                //        RequestKey = new CF_REQUEST_KEY()
                //    };
                //    opInfo2.StructSize = (uint)Marshal.SizeOf(opInfo2);
                //    var TpParam = new CF_OPERATION_PARAMETERS.RESTARTHYDRATION
                //    {
                //        FileIdentity = fileIdPtr,
                //        FileIdentityLength = (uint)fileIdPtr.Size,
                //        Flags = CF_OPERATION_RESTART_HYDRATION_FLAGS.CF_OPERATION_RESTART_HYDRATION_FLAG_MARK_IN_SYNC,
                //        FsMetadata = metaPtr
                //    };
                //    var opParams = CF_OPERATION_PARAMETERS.Create(TpParam);
                //    HRESULT result = CfExecute(opInfo2, ref opParams);
                //    if (result.Succeeded) { CompletionStatus = NTStatus.STATUS_SUCCESS; }
                //}
                #endregion

                if (CompletionStatus != NTStatus.STATUS_SUCCESS)
                {
                    Styletronix.Debug.WriteLine("Error: " + openAsyncResult.Message);

                    var TpParam = new CF_OPERATION_PARAMETERS.TRANSFERDATA
                    {
                        Length = 1, // Length has to be greater than 0 even if transfer failed....
                        Offset = data.RangeStart,
                        Buffer = IntPtr.Zero,
                        Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE,
                        CompletionStatus = CompletionStatus
                    };
                    var opParams = CF_OPERATION_PARAMETERS.Create(TpParam);
                    Styletronix.Debug.LogResponse(CfExecute(opInfo, ref opParams));


                    this.fileFetchHelper.Cancel(data.NormalizedPath);

                    localPlaceholder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC);
                    return;
                }

                byte[] stackBuffer = new byte[stackSize];
                byte[] buffer = new byte[this.chunkSize];

                while (data != null)
                {
                    var currentRangeStart = data.RangeStart;
                    var currentRangeEnd = data.RangeEnd;

                    var currentOffset = currentRangeStart;
                    var totalLength = currentRangeEnd - currentRangeStart;

                    int readLength = (int)Math.Min(currentRangeEnd - currentOffset, (long)this.chunkSize);

                    if (readLength > 0 && ctx.IsCancellationRequested == false)
                    {
                        var readResult = await fetchFile.ReadAsync(buffer, 0, currentOffset, readLength);
                        if (!readResult.Succeeded)
                        {
                            Styletronix.Debug.WriteLine("Error: " + readResult.Message);

                            var opParams = CF_OPERATION_PARAMETERS.Create(new CF_OPERATION_PARAMETERS.TRANSFERDATA
                            {
                                Length = 1, // Length has to be greater than 0 even if transfer failed....
                                Offset = data.RangeStart,
                                Buffer = IntPtr.Zero,
                                Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE,
                                CompletionStatus = new NTStatus((uint)readResult.Status)
                            });
                            Styletronix.Debug.LogResponse(CfExecute(opInfo, ref opParams));


                            this.fileFetchHelper.Cancel(data.NormalizedPath);
                            return;
                        }
                        int dataRead = readResult.BytesRead;

                        if (data.RangeEnd == 0 || data.RangeEnd < currentOffset || data.RangeStart > currentOffset) { continue; }

                        SyncProviderUtils.ApplyTransferStateToFile(this.SyncContext.ConnectionKey, data.TransferKey, totalLength, currentOffset + dataRead);

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

                                    var TpParam = new CF_OPERATION_PARAMETERS.TRANSFERDATA
                                    {
                                        Length = realStackSize,
                                        Offset = currentOffset + stackTransfered,
                                        Buffer = (IntPtr)StackBuffer,
                                        Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE,
                                        CompletionStatus = CompletionStatus
                                    };
                                    var opParams = CF_OPERATION_PARAMETERS.Create(TpParam);

                                    var ret = CfExecute(opInfo, ref opParams);
                                    if (ret.Succeeded == false)
                                    {
                                        Styletronix.Debug.WriteLine(ret.ToString());
                                    }
                                    //ret.ThrowIfFailed();

                                    stackTransfered += realStackSize;
                                }
                            }
                        }

                        this.fileFetchHelper.RemoveRange(data.NormalizedPath, currentRangeStart, currentRangeStart + dataRead);
                    }

                    data = this.fileFetchHelper.TakeNext(data.NormalizedPath);
                }

                await fetchFile.CloseAsync();
            }


            if (ctx.IsCancellationRequested)
            {
                Styletronix.Debug.WriteLine("  FETCH_DATA CANCELED");
            }
            else
            {
                Styletronix.Debug.WriteLine("  FETCH_DATA Completed");
            }
        }
        catch (Exception ex)
        {
            Styletronix.Debug.WriteLine("FETCH_DATA FAILED " + ex.ToString());
            this.fileFetchHelper.Cancel(data.NormalizedPath);
        }
    }


    #region "Callback"
    public void FETCH_PLACEHOLDERS(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Styletronix.Debug.WriteLine("FETCH_PLACEHOLDERS");
        Styletronix.Debug.WriteLine(CallbackInfo.NormalizedPath);
        Styletronix.Debug.WriteLine("Priority: " + CallbackInfo.PriorityHint);

        CF_OPERATION_INFO opInfo = CreateOPERATION_INFO(CallbackInfo, CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_PLACEHOLDERS);
        var ctx = new CancellationTokenSource();


        FetchPlaceholdersCancellationTokens.TryAdd(CallbackInfo.NormalizedPath, ctx);

        FETCH_PLACEHOLDERS_Internal(GetRelativePath(CallbackInfo), opInfo, CallbackParameters.FetchPlaceholders.Pattern, ctx.Token);
    }
    public async void FETCH_PLACEHOLDERS_Internal(string relativePath, CF_OPERATION_INFO opInfo, string Pattern, CancellationToken cancellationToken)
    {
        SetInSyncState(this.SyncContext.LocalRootFolder + "\\" + relativePath, CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC, true);

        using SafePlaceHolderList infos = new SafePlaceHolderList();
        List<Placeholder> placeholders = new List<Placeholder>();

        using (IFileListAsync fileList = SyncContext.ServerProvider.GetNewFileList())
        {
            await fileList.OpenAsync(relativePath, cancellationToken);

            while (await fileList.MoveNextAsync())
            {
                string fileIdString = relativePath + "\\" + fileList.Current.RelativeFileName;
                if (fileList.Current.FileAttributes.HasFlag(FileAttributes.Directory)) { fileIdString += "\\"; }

                placeholders.Add(fileList.Current);
                infos.Add(Styletronix.CloudFilterApi.CreatePlaceholderInfo(fileList.Current, fileIdString));

                if (cancellationToken.IsCancellationRequested)
                    break;
            };

            await fileList.CloseAsync();
        }
        // TODO: Blockweise übertragung
        if (cancellationToken.IsCancellationRequested)
            return;

        uint total = (uint)infos.Count;

        CF_OPERATION_PARAMETERS.TRANSFERPLACEHOLDERS TpParam = new CF_OPERATION_PARAMETERS.TRANSFERPLACEHOLDERS
        {
            PlaceholderArray = infos,
            Flags = CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_DISABLE_ON_DEMAND_POPULATION,
            PlaceholderCount = total,
            PlaceholderTotalCount = total,
            CompletionStatus = NTStatus.STATUS_SUCCESS
        };

        CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(TpParam);

        HRESULT ret = CfExecute(opInfo, ref opParams);


        // Validate local Placeholders. CfExecute only adds missing entries, but does not check existing data.
        foreach (var item in placeholders)
        {
            try
            {
                if (item.ETag != new Placeholder(this.SyncContext.LocalRootFolder + "\\" + item.RelativeFileName).ETag)
                {
                    await ChangedDataQueueBlock.SendAsync(this.SyncContext.LocalRootFolder + "\\" + item.RelativeFileName);
                }
            }
            catch (Exception ex)
            {
                Styletronix.Debug.WriteLine(ex.ToString());
            }
        }

        foreach (var item in Directory.GetFileSystemEntries(this.SyncContext.LocalRootFolder + "\\" + relativePath))
        {
            if (!(from a in placeholders where a.RelativeFileName.Equals(GetRelativePath(item), StringComparison.CurrentCultureIgnoreCase) select a).Any())
            {
                await ChangedDataQueueBlock.SendAsync(item);
            }
        }

        SetInSyncState(this.SyncContext.LocalRootFolder + "\\" + relativePath, CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC, true);
    }

    private class SafePlaceHolderList : Vanara.InteropServices.SafeNativeArray<CF_PLACEHOLDER_CREATE_INFO>
    {
        protected override void Dispose(bool disposing)
        {
            if (this.Elements != null)
            {
                foreach (var item in this.Elements)
                {
                    if (item.FileIdentity != IntPtr.Zero) { Marshal.FreeCoTaskMem(item.FileIdentity); }
                }
            }


            base.Dispose(disposing);
        }
    }

    public void CANCEL_FETCH_PLACEHOLDERS(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Styletronix.Debug.WriteLine("CANCEL_FETCH_PLACEHOLDERS " + CallbackInfo.NormalizedPath);

        if (FetchPlaceholdersCancellationTokens.TryRemove(CallbackInfo.NormalizedPath, out CancellationTokenSource ctx))
        {
            ctx.Cancel();
            Styletronix.Debug.WriteLine("FETCH_PLACEHOLDERS Cancelled" + CallbackInfo.NormalizedPath);
        }
    }

    public void FETCH_DATA(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Styletronix.Debug.WriteLine(@"FETCH_DATA: Priority " + CallbackInfo.PriorityHint +
            @" / R " + CallbackParameters.FetchData.RequiredFileOffset + @" - " + CallbackParameters.FetchData.RequiredLength +
            @" / O " + CallbackParameters.FetchData.OptionalFileOffset + @" - " + CallbackParameters.FetchData.OptionalLength +
            @" / " + CallbackInfo.NormalizedPath);

        var length = CallbackParameters.FetchData.RequiredLength;
        var offset = CallbackParameters.FetchData.RequiredFileOffset;

        if ((offset + length) == CallbackParameters.FetchData.OptionalFileOffset)
        {
            //length = Math.Max(length, Math.Min(this.optionalDownloadPrefetchSize, length + CallbackParameters.FetchData.OptionalLength));
            //length =     Math.Max(length, length + CallbackParameters.FetchData.OptionalLength);
            if (length < this.chunkSize)
            {
                length = Math.Min(this.chunkSize, CallbackParameters.FetchData.OptionalLength + length);
            }
        }

        var data = new SyncProviderUtils.DataActions()
        {
            FileOffset = offset,
            Length = length,
            NormalizedPath = CallbackInfo.NormalizedPath,
            PriorityHint = CallbackInfo.PriorityHint,
            TransferKey = CallbackInfo.TransferKey,
            Id = CallbackInfo.NormalizedPath + "!" + CallbackParameters.FetchData.RequiredFileOffset + "!" + CallbackParameters.FetchData.RequiredLength
        };

        this.fileFetchHelper.Add(data);
    }

    public void CANCEL_FETCH_DATA(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Styletronix.Debug.WriteLine(@"CANCEL_FETCH_DATA: " +
            @" / " + CallbackParameters.Cancel.FetchData.FileOffset + @" - " + CallbackParameters.Cancel.FetchData.Length +
            @" / " + CallbackInfo.NormalizedPath);

        this.fileFetchHelper.Cancel(new SyncProviderUtils.DataActions
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


    private readonly ActionBlock<DeleteAction> DeleteQueue;
    private class DeleteAction
    {
        public CF_OPERATION_INFO OpInfo;
        public string RelativePath;
        public bool IsDirectory;
    }

    public void NOTIFY_DELETE(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        //Styletronix.Debug.WriteLine("NOTIFY_DELETE: " + CallbackInfo.NormalizedPath);

        DeleteQueue.Post(new DeleteAction()
        {
            OpInfo = CreateOPERATION_INFO(CallbackInfo, CF_OPERATION_TYPE.CF_OPERATION_TYPE_ACK_DELETE),
            IsDirectory = CallbackParameters.Delete.Flags.HasFlag(CF_CALLBACK_DELETE_FLAGS.CF_CALLBACK_DELETE_FLAG_IS_DIRECTORY),
            RelativePath = GetRelativePath(CallbackInfo)
        });

        //NOTIFY_DELETE_Internal(GetRelativePath(CallbackInfo), isDirectory, opInfo);
    }
    private async Task NOTIFY_DELETE_Action(DeleteAction dat)
    {
        NTStatus status;
        string fullPath = this.SyncContext.LocalRootFolder + "\\" + dat.RelativePath;

        if (!(dat.IsDirectory ? Directory.Exists(fullPath) : File.Exists(fullPath)))
        {
            status = NTStatus.STATUS_SUCCESS;
            goto skip;
        }

        if (fullPath.Contains(@"$Recycle.bin"))
        {
            status = NTStatus.STATUS_SUCCESS;
            goto skip;
        }

        var pl = new ExtendedPlaceholderState(fullPath);
        if (pl.PlaceholderInfoBasic.PinState == CF_PIN_STATE.CF_PIN_STATE_EXCLUDED)
        {
            status = NTStatus.STATUS_SUCCESS;
            goto skip;
        }

        var result = await SyncContext.ServerProvider.DeleteFileAsync(dat.RelativePath, dat.IsDirectory);
        if (result.Succeeded)
        {
            Styletronix.Debug.WriteLine("Deleted: " + dat.RelativePath);
        }
        else
        {
            Styletronix.Debug.WriteLine("Delete FAILED " + result.Status.ToString() + ": " + dat.RelativePath); ;
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



    public void NOTIFY_DELETE_COMPLETION(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        // Styletronix.Debug.WriteLine("NOTIFY_DELETE_COMPLETION: " + CallbackInfo.NormalizedPath);
    }

    public void NOTIFY_RENAME(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Styletronix.Debug.WriteLine("NOTIFY_RENAME: " + CallbackInfo.NormalizedPath + " -> " + CallbackParameters.Rename.TargetPath);

        if (CallbackParameters.Rename.TargetPath.StartsWith(@"\$Recycle.Bin\"))
        {

        }
        CF_OPERATION_INFO opInfo = CreateOPERATION_INFO(CallbackInfo, CF_OPERATION_TYPE.CF_OPERATION_TYPE_ACK_RENAME);

        NOTIFY_RENAME_Internal(GetRelativePath(CallbackInfo), GetRelativePath(CallbackParameters.Rename),
            CallbackParameters.Rename.Flags.HasFlag(CF_CALLBACK_RENAME_FLAGS.CF_CALLBACK_RENAME_FLAG_IS_DIRECTORY), opInfo);

    }
    public async void NOTIFY_RENAME_Internal(string RelativeFileName, string RelativeFileNameDestination, bool isDirectory, CF_OPERATION_INFO opInfo)
    {
        NTStatus status;

        if (!RelativeFileNameDestination.StartsWith(@"\$Recycle.Bin\", StringComparison.CurrentCultureIgnoreCase))
        {
            var result = await SyncContext.ServerProvider.MoveFileAsync(RelativeFileName, RelativeFileNameDestination, isDirectory);
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

        HRESULT ret = CfExecute(opInfo, ref opParams);
        Styletronix.Debug.WriteLine(ret.Succeeded);
    }

    public void NOTIFY_RENAME_COMPLETION(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Styletronix.Debug.WriteLine("NOTIFY_RENAME_COMPLETION: " + CallbackParameters.RenameCompletion.SourcePath + " -> " + CallbackInfo.NormalizedPath);
    }

    public void NOTIFY_DEHYDRATE(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Styletronix.Debug.WriteLine("NOTIFY_DEHYDRATE: " + CallbackInfo.NormalizedPath + " Reason: " + CallbackParameters.Dehydrate.Reason.ToString());

        CF_OPERATION_INFO opInfo = CreateOPERATION_INFO(CallbackInfo, CF_OPERATION_TYPE.CF_OPERATION_TYPE_ACK_DEHYDRATE);

        CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(new CF_OPERATION_PARAMETERS.ACKDEHYDRATE
        {
            Flags = CF_OPERATION_ACK_DEHYDRATE_FLAGS.CF_OPERATION_ACK_DEHYDRATE_FLAG_NONE,
            CompletionStatus = NTStatus.STATUS_SUCCESS,
        });

        //TODO: Check if Dehydrate is allowed.

        HRESULT ret = CfExecute(opInfo, ref opParams);
        Styletronix.Debug.WriteLine(ret.ToString());
    }

    public void NOTIFY_DEHYDRATE_COMPLETION(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Styletronix.Debug.WriteLine("NOTIFY_DEHYDRATE_COMPLETION: " + CallbackInfo.NormalizedPath + " Reason: " + CallbackParameters.DehydrateCompletion.Reason.ToString());
    }

    public void VALIDATE_DATA(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Styletronix.Debug.WriteLine("VALIDATE_DATA: " + CallbackInfo.NormalizedPath);

        CF_OPERATION_INFO opInfo = CreateOPERATION_INFO(CallbackInfo, CF_OPERATION_TYPE.CF_OPERATION_TYPE_ACK_DATA);

        CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(new CF_OPERATION_PARAMETERS.ACKDATA
        {
            Flags = CF_OPERATION_ACK_DATA_FLAGS.CF_OPERATION_ACK_DATA_FLAG_NONE,
            CompletionStatus = NTStatus.STATUS_SUCCESS,
            Length = CallbackParameters.ValidateData.RequiredLength,
            Offset = CallbackParameters.ValidateData.RequiredFileOffset
        });

        //TODO: Check if File on Server is in Sync.


        HRESULT ret = CfExecute(opInfo, ref opParams);
        Styletronix.Debug.WriteLine(ret.ToString());
    }
    #endregion


    private CF_OPERATION_INFO CreateOPERATION_INFO(in CF_CALLBACK_INFO CallbackInfo, CF_OPERATION_TYPE OperationType)
    {
        CF_OPERATION_INFO opInfo = new CF_OPERATION_INFO()
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

    internal string GetRelativePath(string fullPath)
    {
        if (fullPath.Equals(this.SyncContext.LocalRootFolder)){ return ""; }

        if (fullPath.StartsWith(this.SyncContext.LocalRootFolder, StringComparison.CurrentCultureIgnoreCase))
        {
            return fullPath.Remove(0, this.SyncContext.LocalRootFolder.Length + 1);
        }
        else
        {
            throw new NotSupportedException("Pad nicht unterstützt: " + fullPath);
        }
    }
    internal string GetRelativePath(in CF_CALLBACK_INFO callbackInfo)
    {
        if (callbackInfo.NormalizedPath.StartsWith(this.SyncContext.LocalRootFolderNormalized, StringComparison.CurrentCultureIgnoreCase))
        {
            string relativePath = callbackInfo.NormalizedPath.Remove(0, this.SyncContext.LocalRootFolderNormalized.Length);
            return relativePath.TrimStart(char.Parse("\\"));
        }
        return callbackInfo.NormalizedPath;
    }
    internal string GetRelativePath(in CF_CALLBACK_PARAMETERS.RENAME callbackInfo)
    {
        if (callbackInfo.TargetPath.StartsWith(this.SyncContext.LocalRootFolderNormalized, StringComparison.CurrentCultureIgnoreCase))
        {
            string relativePath = callbackInfo.TargetPath.Remove(0, this.SyncContext.LocalRootFolderNormalized.Length);
            return relativePath.TrimStart(char.Parse("\\"));
        }

        return callbackInfo.TargetPath;
    }
    internal string GetLocalFullPath(in CF_CALLBACK_INFO callbackInfo)
    {
        //string fullPath = callbackInfo.NormalizedPath.Remove(0, this.SyncContext.LocalRootFolderNormalized.Length);
        //fullPath = fullPath.TrimStart(char.Parse("\\"));

        var relativePath = GetRelativePath(callbackInfo);
        return Path.Combine(this.SyncContext.LocalRootFolder, relativePath);
    }


    #region "Dispose"

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                this.fileFetchHelper.Dispose();
                //this.FetchDataCancellationTokenSource.Cancel();
                this.FetchDataQueue.CompleteAdding();
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


