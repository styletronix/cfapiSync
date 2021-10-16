using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.PInvoke;
using System.Linq;
using System.IO;
using Styletronix.CloudSyncProvider;
using Windows.Win32.Storage.CloudFilters;

using System.Threading.Tasks;
using System.Threading;
using Windows.Storage.Provider;

public class SyncProviderUtils
{
    public static Ole32.PROPERTYKEY PKEY_StorageProviderTransferProgress => new Ole32.PROPERTYKEY(new Guid("{e77e90df-6271-4f5b-834f-2dd1f245dda4}"), 4);
    public static void AddFileIdentity(ref CldApi.CF_PLACEHOLDER_CREATE_INFO Placeholder, Placeholder placeHolder)
    {
        string FileIdentity = Newtonsoft.Json.JsonConvert.SerializeObject(placeHolder);
        Placeholder.FileIdentity = Marshal.StringToCoTaskMemUni(FileIdentity);
        Placeholder.FileIdentityLength = (uint)(FileIdentity.Length * Marshal.SizeOf(FileIdentity[0]));
    }
    public static Placeholder GetPlaceholderFromFileIdentity(IntPtr FileIdentity, uint FileIdentityLength)
    {
        if (FileIdentity == IntPtr.Zero) { return null; }
        string fileData = Marshal.PtrToStringUni(FileIdentity, (int)FileIdentityLength);

        return Newtonsoft.Json.JsonConvert.DeserializeObject<Placeholder>(fileData);
    }
    //public static Placeholder GetLasktKnownServerPlaceholderFromFileIdentity(string fullPath)
    //{
    //    using (var h = PInvoke.CreateFileW(fullPath,
    //            Windows.Win32.Storage.FileSystem.FILE_ACCESS_FLAGS.FILE_READ_EA |
    //              Windows.Win32.Storage.FileSystem.FILE_ACCESS_FLAGS.FILE_READ_ATTRIBUTES,
    //              Windows.Win32.Storage.FileSystem.FILE_SHARE_MODE.FILE_SHARE_READ |
    //              Windows.Win32.Storage.FileSystem.FILE_SHARE_MODE.FILE_SHARE_WRITE |
    //              Windows.Win32.Storage.FileSystem.FILE_SHARE_MODE.FILE_SHARE_DELETE,
    //              null,
    //              Windows.Win32.Storage.FileSystem.FILE_CREATION_DISPOSITION.OPEN_EXISTING,
    //              Windows.Win32.Storage.FileSystem.FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_EA,
    //              null))
    //    {
    //        return GetLasktKnownServerPlaceholderFromFileIdentity(h);
    //    }
    //}
    //public static Placeholder GetLasktKnownServerPlaceholderFromFileIdentity(HFILE FileHandle)
    //{
    //    int InfoBufferLength = 1024;
    //    using var buffer = new SafeAllocCoTaskMem(InfoBufferLength);

    //    var ret = CldApi.CfGetPlaceholderInfo(FileHandle, CldApi.CF_PLACEHOLDER_INFO_CLASS.CF_PLACEHOLDER_INFO_BASIC, (IntPtr)buffer, (uint)InfoBufferLength, out uint returnedLength);
    //    if (!ret.Succeeded) { return null; }

    //    if (returnedLength > 0)
    //    {
    //        var info = Marshal.PtrToStructure<CldApi.CF_PLACEHOLDER_BASIC_INFO>((IntPtr)buffer);
    //        if (info.FileIdentity.Length == 0) { return null; }
    //        return Newtonsoft.Json.JsonConvert.DeserializeObject<Placeholder>(System.Text.Encoding.Default.GetString(info.FileIdentity));
    //    }
    //    else
    //    {
    //        return null;
    //    }
    //}

    public static void ApplyTransferStateToFile(in CldApi.CF_CALLBACK_INFO callbackInfo, long total, long completed)
    {
        LoggResponse(CldApi.CfReportProviderProgress(callbackInfo.ConnectionKey, callbackInfo.TransferKey, total, completed));

        #region "Update File Download Progress for File Explorer"
        //try
        //{
        //    Vanara.PInvoke.Shell32.SHCreateItemFromParsingName(
        //        fullPath,
        //        null,
        //        typeof(Vanara.PInvoke.Shell32.IShellItem2).GUID,
        //        out object ppv
        //        ).ThrowIfFailed();
        //    var shellItem = (Vanara.PInvoke.Shell32.IShellItem2)ppv;

        //    var propStore = shellItem.GetPropertyStore(
        //        PropSys.GETPROPERTYSTOREFLAGS.GPS_READWRITE | PropSys.GETPROPERTYSTOREFLAGS.GPS_VOLATILEPROPERTIESONLY,
        //        typeof(PropSys.IPropertyStore).GUID
        //        );

        //    var values = new ulong[] { (ulong)completed, (ulong)total };

        //    Ole32.PROPVARIANT transferProgress = new Ole32.PROPVARIANT();
        //    Vanara.PInvoke.PropSys.InitPropVariantFromUInt64Vector(values, (uint)values.Length, transferProgress).ThrowIfFailed();
        //    propStore.SetValue(PKEY_StorageProviderTransferProgress, transferProgress);

        //    Ole32.PROPVARIANT transferStatus = new Ole32.PROPVARIANT();
        //    var status = (completed < total) ? Constants.SYNC_TRANSFER_STATUS.STS_TRANSFERRING : Constants.SYNC_TRANSFER_STATUS.STS_NONE;
        //    Vanara.PInvoke.PropSys.InitPropVariantFromUInt32Vector(new uint[] { (uint)status }, 1, transferStatus);
        //    propStore.SetValue(Ole32.PROPERTYKEY.System.SyncTransferStatus, transferStatus);

        //    // TODO: Werte werden nicht gespeichert. Wieso ???             
        //    propStore.Commit();

        //    unsafe
        //    {
        //        fixed (char* fullPathPtr = fullPath)
        //        {
        //            PInvoke.SHChangeNotify(
        //                             Windows.Win32.UI.Shell.SHCNE_ID.SHCNE_UPDATEITEM,
        //                             Windows.Win32.UI.Shell.SHCNF_FLAGS.SHCNF_PATHW, fullPathPtr
        //                             );
        //        }
        //    }


        //    //winrt::com_ptr<IPropertyStore> propStoreVolatile;
        //    //winrt::check_hresult(
        //    //    shellItem->GetPropertyStore(
        //    //        GETPROPERTYSTOREFLAGS::GPS_READWRITE | GETPROPERTYSTOREFLAGS::GPS_VOLATILEPROPERTIESONLY,
        //    //        __uuidof(propStoreVolatile),
        //    //        propStoreVolatile.put_void()));

        //    //// The PKEY_StorageProviderTransferProgress property works with a UINT64 array that is two elements, with
        //    //// element 0 being the amount of data transferred, and element 1 being the total amount
        //    //// that will be transferred.
        //    //PROPVARIANT transferProgress;
        //    //UINT64 values[]{ completed , total };
        //    //winrt::check_hresult(InitPropVariantFromUInt64Vector(values, ARRAYSIZE(values), &transferProgress));
        //    //winrt::check_hresult(propStoreVolatile->SetValue(PKEY_StorageProviderTransferProgress, transferProgress));

        //    //// Set the sync transfer status accordingly
        //    //PROPVARIANT transferStatus;
        //    //winrt::check_hresult(
        //    //    InitPropVariantFromUInt32(
        //    //        (completed < total) ? SYNC_TRANSFER_STATUS::STS_TRANSFERRING : SYNC_TRANSFER_STATUS::STS_NONE,
        //    //        &transferStatus));
        //    //winrt::check_hresult(propStoreVolatile->SetValue(PKEY_SyncTransferStatus, transferStatus));

        //    //// Without this, all your hard work is wasted.
        //    //winrt::check_hresult(propStoreVolatile->Commit());

        //    //// Broadcast a notification that something about the file has changed, so that apps
        //    //// who subscribe (such as File Explorer) can update their UI to reflect the new progress
        //    //SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATH, static_cast<LPCVOID>(fullPath), nullptr);

        //    //wprintf(L"Succesfully Set Transfer Progress on \"%s\" to %llu/%llu\n", fullPath, completed, total);
        //}
        //catch (Exception ex)
        //{
        //    Debug.WriteLine(ex.ToString());
        //}
        #endregion
    }
    public static void ApplyTransferStateToFile(in CldApi.CF_CONNECTION_KEY connectionKey, in CldApi.CF_TRANSFER_KEY transferKey, long total, long completed)
    {
        LoggResponse(CldApi.CfReportProviderProgress(connectionKey, transferKey, total, completed));
    }

    public class DataActions
    {
        public long FileOffset;
        public long Length;
        public string NormalizedPath;
        public CldApi.CF_TRANSFER_KEY TransferKey;
        public CldApi.CF_REQUEST_KEY RequestKey;
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
        public CldApi.CF_TRANSFER_KEY TransferKey;
        public byte PriorityHint;
    }
    public class SyncContext
    {
        /// <summary>
        /// Absolute Path to the local Root Folder where the cached files are stored.
        /// </summary>
        public string LocalRootFolder;

        public string LocalRootFolderNormalized;

        public CldApi.CF_CONNECTION_KEY ConnectionKey;

        public iServerFileProvider ServerProvider;
        public SyncProviderParameters SyncProviderParameter;
    }

    public static void LoggResponse(HRESULT hResult)
    {
        if (hResult != HRESULT.S_OK)
        {
            Debug.WriteLine(hResult);
        }
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
    private CldApi.CF_CALLBACK_REGISTRATION[] _callbackMappings;
    private bool _isConnected;
    private FileSystemWatcher watcher;
    private System.Collections.Concurrent.BlockingCollection<FileSystemEventArgs> ChangedDataQueue;
    private Task ChangedDataWorkerThread;
    private CancellationTokenSource ChangedDataCancellationTokenSource;
    private SyncProviderUtils.SyncContext SyncContext;
    private System.Collections.Concurrent.ConcurrentDictionary<string, string> filesToCheck;
    private FileFetchHelper2 fileFetchHelper = new();


    public class FileFetchHelper2 : IDisposable
    {
        private readonly object lockObject = new();
        private readonly List<SyncProviderUtils.FetchRange> filesToProcess = new();
        private readonly AutoResetEvent _waitForNewItem = new(false);
        private readonly CancellationTokenSource cancellationToken = new();

        public FileFetchHelper2()
        {
            cancellationToken.Token.Register(() => _waitForNewItem.Close());
        }

        public AutoResetEvent Waiter => this._waitForNewItem;
        public CancellationToken CancellationToken { get { return cancellationToken.Token; } }
        public SyncProviderUtils.FetchRange WaitTakeNext()
        {
            var x = this.TakeNext();

            if (x == null)
            {
                _waitForNewItem.WaitOne();

                lock (lockObject)
                {
                    _waitForNewItem.Reset();
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



            this._waitForNewItem.Set();
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

    public class FileFetchHelper : IDisposable
    {
        private readonly object filesToProcessLockObject = new();
        private readonly List<SyncProviderUtils.DataActions> filesToProcess = new();
        private readonly AutoResetEvent _waitForNewItem = new(false);
        private readonly CancellationTokenSource cancellationToken = new();

        public FileFetchHelper()
        {
            cancellationToken.Token.Register(() => _waitForNewItem.Close());
        }

        public AutoResetEvent Waiter => this._waitForNewItem;
        public CancellationToken CancellationToken { get { return cancellationToken.Token; } }
        public SyncProviderUtils.DataActions WaitTakeNext()
        {
            var x = this.TakeNext();

            if (x == null)
            {
                _waitForNewItem.WaitOne();
                lock (filesToProcessLockObject)
                {
                    _waitForNewItem.Reset();
                    var t = TakeNext();
                    lastTaken = t;
                    return t;
                }
            }
            else
            {
                return x;
            }
        }
        public SyncProviderUtils.DataActions TakeNext()
        {
            SyncProviderUtils.DataActions ret = null;

            lock (filesToProcessLockObject)
            {
                ret = this.filesToProcess.OrderByDescending(a => a.PriorityHint)
                    .OrderBy(a => a.FileOffset).FirstOrDefault();

                if (ret != null) { this.filesToProcess.Remove(ret); }
            }

            return ret;
        }
        //public SyncProviderUtils.DataActions TakeNext(string normalizedPath)
        //{
        //    SyncProviderUtils.DataActions ret = null;

        //    lock (filesToProcessLockObject)
        //    {
        //        ret = this.filesToProcess.Where(a => a.NormalizedPath == normalizedPath)
        //            .OrderBy(a => a.FileOffset).FirstOrDefault();

        //        if (ret != null) { this.filesToProcess.Remove(ret); }
        //    }

        //    return ret;
        //}
        private SyncProviderUtils.DataActions lastTaken;
        public SyncProviderUtils.DataActions TakeNext(string normalizedPath, long offset)
        {
            SyncProviderUtils.DataActions ret = null;

            lock (filesToProcessLockObject)
            {
                ret = this.filesToProcess.Where(a => a.NormalizedPath == normalizedPath && a.FileOffset >= offset)
                    .OrderBy(a => a.FileOffset).FirstOrDefault();

                if (ret != null)
                {
                    this.filesToProcess.Remove(ret);
                    lastTaken = ret;
                }
            }

            return ret;
        }
        public List<SyncProviderUtils.DataActions> TakeNextRange(string normalizedPath, long offset, long blockSize)
        {
            List<SyncProviderUtils.DataActions> ret = new();
            lock (filesToProcessLockObject)
            {
                long currentOffset = offset;

                foreach (var item in this.filesToProcess.Where(a => a.NormalizedPath == normalizedPath && a.FileOffset >= offset)
                     .OrderBy(a => a.FileOffset))
                {
                    if (item.FileOffset == currentOffset)
                    {
                        currentOffset = item.FileOffset + item.Length;
                        if (currentOffset - offset > blockSize) { break; }

                        ret.Add(item);
                        this.filesToProcess.Remove(item);
                        lastTaken = item;
                    }

                }
            }

            return ret;
        }
        public void Add(SyncProviderUtils.DataActions data)
        {
            //var low = data.FileOffset;
            //var high = data.FileOffset + data.Length;

            //lock (filesToProcessLockObject)
            //{
            //foreach (var item in this.filesToProcess)
            //{
            //    if (!item.CancellationTokenSource.IsCancellationRequested && item.NormalizedPath == data.NormalizedPath)
            //    {
            //        if (item.FileOffset  <= low && item.FileOffset + item.Length >= high)
            //        {
            //            // Falls within Range of existing item: Extend Length.
            //            //item.Length = Math.Max((high - item.FileOffset), item.Length);
            //            //Debug.WriteLine(@"Change Queue Length to " + item.Length);
            //            return;
            //        }
            //    }
            //}

            // Add as New Entry if not found
            data.CancellationTokenSource = new();
            lock (filesToProcessLockObject)
            {
                this.filesToProcess.Add(data);
            }
            Debug.WriteLine("Add to Queue " + data.FileOffset + @" - " + data.Length);
            //}

            this._waitForNewItem.Set();
        }
        public void Cancel(SyncProviderUtils.DataActions data)
        {
            //lock (filesToProcessLockObject)
            //{
            var remove = new List<SyncProviderUtils.DataActions>();


            lock (filesToProcessLockObject)
            {
                if (lastTaken?.Id == data.Id)
                {
                    lastTaken.CancellationTokenSource.Cancel();
                }

                foreach (var item in this.filesToProcess)
                {
                    if (item.Id == data.Id)
                    {
                        item.CancellationTokenSource.Cancel();
                        remove.Add(item);
                    }
                    //if (item.NormalizedPath == data.NormalizedPath)
                    //{
                    //    if (item.FileOffset >= data.FileOffset && (item.FileOffset + item.Length) <= (data.FileOffset + data.Length))
                    //    {
                    //        Debug.WriteLine("Cancel Block " + item.FileOffset + " - " + item.Length);
                    //        item.CancellationTokenSource.Cancel();
                    //        remove.Add(item);
                    //    }
                    //}
                }
            }

            lock (filesToProcessLockObject)
            {
                foreach (var item in remove)
                {
                    this.filesToProcess.Remove(item);
                }
            }

            //}
        }
        //public long Finalize(SyncProviderUtils.DataActions data)
        //{
        //    long ret;

        //    lock (filesToProcessLockObject)
        //    {
        //        ret = data.Length;
        //        this.filesToProcess.Remove(data);
        //    }

        //    return ret;
        //}

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


    public class FileData
    {
        public Exception lastException;
        public DateTime lastTry;
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
        this.filesToCheck = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
    }

    public string GetSyncRootID()
    {
        string syncRootID = this.SyncContext.SyncProviderParameter.ProviderInfo.ProviderId.ToString();
        syncRootID += @"!";
        syncRootID += System.DirectoryServices.AccountManagement.UserPrincipal.Current.Sid.Value;
        syncRootID += @"!";
        syncRootID += this.SyncContext.LocalRootFolder.GetHashCode();  // Provider Account -> Used Hash of LocalPath asuming that no Account would be synchronized to the same Folder.
        return syncRootID;
    }
    public async Task Register()
    {
        if (StorageProviderSyncRootManager.IsSupported() == false)
        {
            Debug.WriteLine("OS not supported!");
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
            HydrationPolicyModifier = StorageProviderHydrationPolicyModifier.AutoDehydrationAllowed | StorageProviderHydrationPolicyModifier.StreamingAllowed,
            InSyncPolicy = StorageProviderInSyncPolicy.FileLastWriteTime,
            Path = path,
            PopulationPolicy = StorageProviderPopulationPolicy.Full,
            ProtectionMode = StorageProviderProtectionMode.Personal,
            ProviderId = this.SyncContext.SyncProviderParameter.ProviderInfo.ProviderId,
            Version = this.SyncContext.SyncProviderParameter.ProviderInfo.ProviderVersion,
            IconResource = @"%SystemRoot%\system32\charmap.exe,0",
            ShowSiblingsAsGroup = true,
            RecycleBinUri = null,
            Context = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(GetSyncRootID(), Windows.Security.Cryptography.BinaryStringEncoding.Utf8)
        };
        // Context is currently not used
        // RecycleBinUri = new Uri(@"http://cloudmirror.example.com/recyclebin"),

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
        #endregion


        // TODO: Verify reqirement of the registry entries.
        try
        {
            Microsoft.Win32.RegistryKey CLSIDkey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID", true).CreateSubKey(this.SyncContext.SyncProviderParameter.ProviderInfo.CLSID, true);
            CLSIDkey.SetValue("", this.SyncContext.SyncProviderParameter.ProviderInfo.ProviderName, Microsoft.Win32.RegistryValueKind.String);
            CLSIDkey.SetValue(@"System.IsPinnedToNameSpaceTree", 1, Microsoft.Win32.RegistryValueKind.DWord);

            CLSIDkey.CreateSubKey("DefaultIcon", true).SetValue("", @"C:\WINDOWS\system32\imageres.dll,-1043", Microsoft.Win32.RegistryValueKind.ExpandString);
            CLSIDkey.CreateSubKey("InProcServer32", true).SetValue("", @"C:\WINDOWS\system32\shell32.dll", Microsoft.Win32.RegistryValueKind.ExpandString);

            Microsoft.Win32.RegistryKey InstanceKey = CLSIDkey.CreateSubKey("Instance", true);
            InstanceKey.SetValue("CLSID", @"{0E5AAE11-A475-4c5b-AB00-C66DE400274E} ", Microsoft.Win32.RegistryValueKind.String);

            Microsoft.Win32.RegistryKey InitPropertyBagKey = InstanceKey.CreateSubKey(@"InitPropertyBag", true);
            InitPropertyBagKey.SetValue("TargetFolderPath", this.SyncContext.LocalRootFolder, Microsoft.Win32.RegistryValueKind.ExpandString);
            InitPropertyBagKey.SetValue("Attributes", 17, Microsoft.Win32.RegistryValueKind.DWord);

            Microsoft.Win32.RegistryKey ShellFolderKey = CLSIDkey.CreateSubKey(@"ShellFolder", true);
            ShellFolderKey.SetValue("Attributes", unchecked((int)0xF080004D), Microsoft.Win32.RegistryValueKind.DWord);
            ShellFolderKey.SetValue("FolderValueFlags", 40, Microsoft.Win32.RegistryValueKind.DWord);

            Microsoft.Win32.RegistryKey NameSpacekey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace", true).CreateSubKey(this.SyncContext.SyncProviderParameter.ProviderInfo.CLSID, true);
            NameSpacekey.SetValue("", this.SyncContext.SyncProviderParameter.ProviderInfo.ProviderName, Microsoft.Win32.RegistryValueKind.String);

            Microsoft.Win32.RegistryKey NewStartPanelkey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", true);
            NewStartPanelkey.SetValue(this.SyncContext.SyncProviderParameter.ProviderInfo.CLSID, 1, Microsoft.Win32.RegistryValueKind.DWord);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
    public void Unregister()
    {
        Stop();

        Debug.WriteLine("Unregister");

        try
        {
            StorageProviderSyncRootManager.Unregister(this.GetSyncRootID());
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }


        try
        {
            Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID", true).DeleteSubKeyTree(this.SyncContext.SyncProviderParameter.ProviderInfo.CLSID, false);
            Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace", true).DeleteSubKeyTree(this.SyncContext.SyncProviderParameter.ProviderInfo.CLSID, false);
            Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", true).DeleteSubKey(this.SyncContext.SyncProviderParameter.ProviderInfo.CLSID, false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

    }
    private void InitWatcher()
    {
        Debug.WriteLine("InitWatcher");

        StopWatcher();

        this.ChangedDataQueue = new System.Collections.Concurrent.BlockingCollection<FileSystemEventArgs>();
        this.ChangedDataCancellationTokenSource = new System.Threading.CancellationTokenSource();
        this.ChangedDataWorkerThread = StartNewChangedDataWorker(this.ChangedDataCancellationTokenSource.Token);

        watcher = new FileSystemWatcher();
        watcher.Path = this.SyncContext.LocalRootFolder;
        watcher.IncludeSubdirectories = true;
        watcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.Size;
        watcher.Filter = "*";
        watcher.Error += new ErrorEventHandler(FileSystemWatcher_OnError);
        watcher.Changed += new FileSystemEventHandler(FileSystemWatcher_OnChanged);
        watcher.Created += new FileSystemEventHandler(FileSystemWatcher_OnChanged);
        watcher.EnableRaisingEvents = true;
    }
    private void StopWatcher()
    {
        if (watcher != null)
        {
            Debug.WriteLine("StopWatcher");
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        if (this.ChangedDataQueue != null)
        {
            this.ChangedDataQueue.CompleteAdding();
        }

        if (this.ChangedDataCancellationTokenSource != null)
        {
            this.ChangedDataCancellationTokenSource.Cancel();
        }
    }
    private void FileSystemWatcher_OnError(object sender, ErrorEventArgs e)
    {
        Debug.WriteLine("FileSystemWatcher_OnError");
        Debug.WriteLine(e.GetException().Message);
    }
    private void FileSystemWatcher_OnChanged(object sender, FileSystemEventArgs e)
    {
        //Debug.WriteLine("FileSystemWatcher_OnChanged: " + e.FullPath);

        this.ChangedDataQueue.Add(e);
        this.filesToCheck.TryAdd(e.FullPath, null);
    }

    private Task StartNewChangedDataWorker(CancellationToken ctx)
    {
        return Task.Run(() =>
        {
            while (!ChangedDataQueue.IsCompleted)
            {
                if (ctx.IsCancellationRequested) { return; }

                if (!ChangedDataQueue.TryTake(out FileSystemEventArgs data, -1, ctx)) { return; }
                Thread.Sleep(100);

                try
                {
                    ProcessChangedDataAsync(data, ctx).Wait(ctx);
                }
                catch (Exception ex)
                {
                    var fData = new FileData
                    {
                        lastTry = DateTime.Now,
                        lastException = ex
                    };

                    //filesToProcess.AddOrUpdate(data.FullPath, fData, (k, v) => fData);
                    Debug.WriteLine(ex.ToString());
                }
            }
        }, ctx);
    }
    private async Task ProcessChangedDataAsync(FileSystemEventArgs e, CancellationToken ctx)
    {
        var state = new Styletronix.CloudFilterApi.ExtendedPlaceholderState(e.FullPath); //Styletronix.CloudFilterApi.GetExtendedPlaceholderState(e.FullPath);

        #region "Convert to Placeholder if required"
        if (!state.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER))
        {
            var relativePath = GetRelativePath(e.FullPath);
            string fileIdString = relativePath;
            if (state.Attributes.HasFlag(FileAttributes.Directory)) { fileIdString += "\\"; }

            Styletronix.CloudFilterApi.ConvertToPlaceholder(e.FullPath, fileIdString);
        }
        #endregion


        var serverInfo = await this.SyncContext.ServerProvider.GetFileInfo(GetRelativePath(e.FullPath), state.Attributes.HasFlag(FileAttributes.Directory));


        if (state.Attributes.HasFlag(FileAttributes.Directory))
        {
            if (state.PlaceholderInfoBasic.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC)
            {

            }
        }
        else
        {
            if (state.PlaceholderInfoBasic.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC)
            {
                if (state.LastWriteTime < serverInfo.LastWriteTime)
                {
                    Debug.WriteLine("TODO: FetchData if required");
                    // TODO: FetchData if required
                }
                else if (state.LastWriteTime > serverInfo.LastWriteTime)
                {
                    await UploadFileToServer(e.FullPath, ctx);
                }
                else
                {
                    Styletronix.CloudFilterApi.SetInSyncState(e.FullPath, CldApi.CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC,state.Attributes.HasFlag(FileAttributes.Directory));
                }
            }

            if (state.PlaceholderInfoBasic.PinState == CF_PIN_STATE.CF_PIN_STATE_UNPINNED)
            {
                if (state.LastWriteTime <= serverInfo.LastWriteTime)
                {
                    Styletronix.CloudFilterApi.DehydratePlaceholder(e.FullPath);
                }
                else if (state.LastWriteTime > serverInfo.LastWriteTime)
                {
                    await UploadFileToServer(e.FullPath, ctx);

                    if (ctx.IsCancellationRequested) { return; }

                    // Refresh State
                    state = new Styletronix.CloudFilterApi.ExtendedPlaceholderState(e.FullPath);
                    //info = Styletronix.CloudFilterApi.GetPlaceholderInfoBasic(e.FullPath, state.Attributes.HasFlag(FileAttributes.Directory));
                    if (state.PlaceholderInfoBasic.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC)
                    {
                        Styletronix.CloudFilterApi.DehydratePlaceholder(e.FullPath);
                    }
                }
            }

            if (state.PlaceholderInfoBasic.PinState == CF_PIN_STATE.CF_PIN_STATE_PINNED && state.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
            {
                Styletronix.CloudFilterApi.HydratePlaceholder(e.FullPath);
            }
        }
    }

    private async Task UploadFileToServer(string fullPath, CancellationToken ctx)
    {
        Debug.WriteLine("Upload File: " + fullPath);

        var relativePath = GetRelativePath(fullPath);

        using (var wrf = this.SyncContext.ServerProvider.GetNewWriteFile())
        {
            using (FileStream fStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Placeholder placeHolder = new(fullPath);
                long currentOffset = 0;
                var buffer = new byte[this.chunkSize];

                using var TransferKey = new Styletronix.CloudFilterApi.SafeTransferKey(fStream.SafeFileHandle);

                await wrf.OpenAsync(new OpenAsyncParams()
                {
                    RelativeFileName = relativePath,
                    FileInfo = placeHolder,
                    CancellationToken = ctx,
                    mode = UploadMode.fullFile,
                    ETag = null // TODO: ETAG hinzufügen
                });

                var readBytes = await fStream.ReadAsync(buffer, 0, this.chunkSize);
                while (readBytes > 0)
                {
                    SyncProviderUtils.ApplyTransferStateToFile(this.SyncContext.ConnectionKey, TransferKey, placeHolder.FileSize, currentOffset + readBytes);

                    await wrf.WriteAsync(buffer, 0, currentOffset, readBytes);
                    currentOffset += readBytes;
                    readBytes = await fStream.ReadAsync(buffer, 0, this.chunkSize);
                };

                var placeHolderNew = await wrf.CloseAsync(ctx.IsCancellationRequested == false);

                if (!ctx.IsCancellationRequested)
                {
                    if (placeHolderNew != null)
                    {
                        if (placeHolder.LastWriteTime != placeHolderNew.LastWriteTime)
                        {
                            File.SetLastWriteTime(fullPath, placeHolderNew.LastWriteTime);
                        }
                    }

                    Styletronix.CloudFilterApi.SetInSyncState(fStream.SafeFileHandle, CldApi.CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC);
                    //SyncProviderUtils.LoggResponse(CldApi.CfSetInSyncState((HFILE)fStream.SafeFileHandle,
                    //                                   CldApi.CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC,
                    //                                   CldApi.CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE));
                }
            }
        }

        Debug.WriteLine("Upload File Completed: " + fullPath);
    }

    public enum SyncMode
    {
        Local,
        Full
    }
    private void FindLocalChangedDataRecursive(string folder, CancellationToken ctx, SyncMode syncMode)
    {
        bool fileFound;

        using (var findHandle = Kernel32.FindFirstFile(@"\\?\" + folder + @"\*", out Vanara.PInvoke.WIN32_FIND_DATA findData))
        {
            fileFound = (findHandle.IsInvalid == false);

            while (fileFound)
            {
                if (findData.cFileName != "." && findData.cFileName != "..")
                {
                    Styletronix.CloudFilterApi.ExtendedPlaceholderState state = new(findData, folder);

                    if (state.Attributes.HasFlag(FileAttributes.Directory))
                    {
                        #region "Convert to Placeholder if required"
                        if (!state.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER))
                        {
                            var relativePath = GetRelativePath(folder + "\\" + findData.cFileName);
                            string fileIdString = relativePath;
                            if (state.Attributes.HasFlag(FileAttributes.Directory)) { fileIdString += "\\"; }

                            Styletronix.CloudFilterApi.ConvertToPlaceholder(folder + "\\" + findData.cFileName, fileIdString);
                        }
                        #endregion

                        if (!state.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
                        {
                            var b = state.PlaceholderInfoBasic;
                            // Traverse subdirectory
                            FindLocalChangedDataRecursive(folder + "\\" + findData.cFileName, ctx, syncMode);
                        }

                    }
                    else
                    {
                        if (syncMode.HasFlag(SyncMode.Full))
                        {
                            this.filesToCheck.TryAdd(folder + "\\" + findData.cFileName, null);
                            this.ChangedDataQueue.Add(new FileSystemEventArgs(WatcherChangeTypes.All, folder, findData.cFileName));
                        }
                        else
                        {
                            if (!state.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC))
                            {
                                this.filesToCheck.TryAdd(folder + "\\" + findData.cFileName, null);
                                this.ChangedDataQueue.Add(new FileSystemEventArgs(WatcherChangeTypes.All, folder, findData.cFileName));
                            }
                        }
                    }
                }

                if (ctx.IsCancellationRequested) { return; }
                fileFound = Kernel32.FindNextFile(findHandle, out findData);
            }
        }
    }

    public async Task SyncDataAsync(SyncMode syncMode, CancellationToken ctx)
    {
        await Task.Run(() =>
        {
            FindLocalChangedDataRecursive(this.SyncContext.LocalRootFolder, ctx, syncMode);
        }, ctx);
    }

    public async Task Start(CancellationToken ctx)
    {
        if (Directory.Exists(this.SyncContext.LocalRootFolder) == false)
        {
            Directory.CreateDirectory(this.SyncContext.LocalRootFolder);
        }

        Debug.WriteLine("Register");

        await Register();

        Debug.WriteLine("Connect");

        this._callbackMappings = new CldApi.CF_CALLBACK_REGISTRATION[]
      {
          new CldApi.CF_CALLBACK_REGISTRATION {
                    Callback = new CldApi.CF_CALLBACK(this.FETCH_PLACEHOLDERS),
                    Type = CldApi.CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_FETCH_PLACEHOLDERS
                } ,
           new CldApi.CF_CALLBACK_REGISTRATION {
                    Callback = new CldApi.CF_CALLBACK(this.CANCEL_FETCH_PLACEHOLDERS),
                    Type = CldApi.CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_CANCEL_FETCH_PLACEHOLDERS
                } ,
          new CldApi.CF_CALLBACK_REGISTRATION {
                    Callback = new CldApi.CF_CALLBACK(this.FETCH_DATA),
                    Type = CldApi.CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_FETCH_DATA
                } ,
          new CldApi.CF_CALLBACK_REGISTRATION {
                    Callback = new CldApi.CF_CALLBACK(this.CANCEL_FETCH_DATA),
                    Type = CldApi.CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_CANCEL_FETCH_DATA
                } ,
          new CldApi.CF_CALLBACK_REGISTRATION {
                    Callback = new CldApi.CF_CALLBACK(this.NOTIFY_FILE_OPEN_COMPLETION),
                    Type = CldApi.CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_FILE_OPEN_COMPLETION
                } ,
          new CldApi.CF_CALLBACK_REGISTRATION {
                    Callback = new CldApi.CF_CALLBACK(this.NOTIFY_FILE_CLOSE_COMPLETION),
                    Type = CldApi.CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_FILE_CLOSE_COMPLETION
                } ,
           new CldApi.CF_CALLBACK_REGISTRATION {
                    Callback = new CldApi.CF_CALLBACK(this.NOTIFY_DELETE),
                    Type = CldApi.CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DELETE
                } ,
           new CldApi.CF_CALLBACK_REGISTRATION {
                    Callback = new CldApi.CF_CALLBACK(this.NOTIFY_DELETE_COMPLETION),
                    Type = CldApi.CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DELETE_COMPLETION
                } ,
           new CldApi.CF_CALLBACK_REGISTRATION {
                    Callback = new CldApi.CF_CALLBACK(this.NOTIFY_RENAME),
                    Type = CldApi.CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_RENAME
                } ,
            new CldApi.CF_CALLBACK_REGISTRATION {
                    Callback = new CldApi.CF_CALLBACK(this.NOTIFY_RENAME_COMPLETION),
                    Type = CldApi.CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_RENAME_COMPLETION
                } ,
             new CldApi.CF_CALLBACK_REGISTRATION {
                    Callback = new CldApi.CF_CALLBACK(this.NOTIFY_DEHYDRATE),
                    Type = CldApi.CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DEHYDRATE
                } ,
             new CldApi.CF_CALLBACK_REGISTRATION {
                    Callback = new CldApi.CF_CALLBACK(this.NOTIFY_DEHYDRATE_COMPLETION),
                    Type = CldApi.CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DEHYDRATE_COMPLETION
                } ,
             new CldApi.CF_CALLBACK_REGISTRATION {
                    Callback = new CldApi.CF_CALLBACK(this.VALIDATE_DATA),
                    Type = CldApi.CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_VALIDATE_DATA
                } ,
           CldApi.CF_CALLBACK_REGISTRATION.CF_CALLBACK_REGISTRATION_END
      };

        this.SyncContext.ConnectionKey = default;

        HRESULT ret = CldApi.CfConnectSyncRoot(this.SyncContext.LocalRootFolder, _callbackMappings,
            IntPtr.Zero,
            CldApi.CF_CONNECT_FLAGS.CF_CONNECT_FLAG_REQUIRE_PROCESS_INFO |
            CldApi.CF_CONNECT_FLAGS.CF_CONNECT_FLAG_REQUIRE_FULL_FILE_PATH,
            out this.SyncContext.ConnectionKey);

        if (ret.Succeeded)
        {
            _isConnected = true;
            Debug.WriteLine("Connected");
        }
        else
        {
            Debug.WriteLine("Connection failed!");
        }
        ret.ThrowIfFailed();

        ret = CldApi.CfUpdateSyncProviderStatus(this.SyncContext.ConnectionKey, CldApi.CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_SYNC_INCREMENTAL);
        if (ret.Succeeded == false) { Debug.WriteLine("Fehler bei CfUpdateSyncProviderStatus: " + ret.ToString()); }

        InitWatcher();

        Debug.WriteLine("Initial Sync...");
        await SyncDataAsync(SyncMode.Local, ctx);

        ret = CldApi.CfUpdateSyncProviderStatus(this.SyncContext.ConnectionKey, CldApi.CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_IDLE);
        if (ret.Succeeded == false) { Debug.WriteLine("Fehler bei CfUpdateSyncProviderStatus: " + ret.ToString()); }

        Debug.WriteLine("Ready");
    }
    public void Stop()
    {
        if (this._isConnected == false) { return; }

        HRESULT ret = CldApi.CfDisconnectSyncRoot(this.SyncContext.ConnectionKey);
        if (ret.Succeeded)
        {
            this._isConnected = false;
        }
        Debug.WriteLine("DisconnectSyncRoot: " + ret.ToString());

        CldApi.CfUpdateSyncProviderStatus(this.SyncContext.ConnectionKey, CldApi.CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_TERMINATED);
    }

    public void DeleteLocalData()
    {
        this.Stop();

        HRESULT ret = CldApi.CfUnregisterSyncRoot(this.SyncContext.LocalRootFolder);
        Debug.WriteLine("UnregisterSyncRoot: " + ret.ToString());

        try
        {
            if (Directory.Exists(this.SyncContext.LocalRootFolder))
            {
                Directory.Delete(this.SyncContext.LocalRootFolder, true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Delete Root Folder: " + ex.ToString());
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
        return Task.Run(() =>
        {
            while (!this.fileFetchHelper.CancellationToken.IsCancellationRequested)
            {
                var item = this.fileFetchHelper.WaitTakeNext();
                if (this.fileFetchHelper.CancellationToken.IsCancellationRequested) { break; }
                if (item != null)
                {
                    try
                    {
                        FetchDataAsync(item).Wait(this.fileFetchHelper.CancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                }
            }
        }, this.fileFetchHelper.CancellationToken);
    }
    //private async Task FetchDataAsync(SyncProviderUtils.DataActions data)
    //{
    //    var ctx = data.CancellationTokenSource.Token;
    //    if (ctx.IsCancellationRequested) { return; }

    //    string relativePath = data.NormalizedPath.Remove(0, this.SyncContext.LocalRootFolderNormalized.Length).TrimStart(char.Parse("\\"));
    //    string targetFullPath = Path.Combine(this.SyncContext.LocalRootFolder, relativePath);

    //    Debug.WriteLine("Fetch Data " + data.FileOffset + @" - " + data.Length + @" / " + relativePath);


    //    using (iReadFileAsync fetchFile = SyncContext.ServerProvider.GetNewReadFile())
    //    {
    //        long Offset = data.FileOffset;
    //        long Length = data.Length;
    //        bool exitLoop = false;
    //        byte[] stackBuffer = new byte[stackSize];
    //        Task<int> fetchTask;


    //        //var rangeList = this.fileFetchHelper.TakeNextRange(data.NormalizedPath, Offset + Length, this.chunkSize);
    //        //if (rangeList.Count > 0)
    //        //{
    //        //    long rangeLength = rangeList.Max(a => (a.FileOffset + a.Length));
    //        //    Length = Math.Max(rangeLength, Length);
    //        //}


    //        var openAsyncResult = await fetchFile.OpenAsync(new OpenAsyncParams()
    //        {
    //            RelativeFileName = relativePath,
    //            CancellationToken = ctx,
    //            ETag = null // TODO: ETAG ausfüllen
    //        });
    //        byte[] buffer = new byte[this.chunkSize];
    //        byte[] buffer1 = new byte[this.chunkSize];

    //        while (!exitLoop)
    //        {
    //            int requiredBuffer = (int)Math.Min(this.chunkSize, Length);
    //            //byte[] buffer = new byte[requiredBuffer];
    //            //byte[] buffer1 = new byte[requiredBuffer];

    //            var realOffset = Offset;
    //            long readTotal = 0;
    //            int readLength = (int)Math.Min(Length - readTotal, (long)this.chunkSize);

    //            fetchTask = fetchFile.ReadAsync(buffer1, 0, realOffset, readLength);

    //            // if (Debugger.IsAttached) { System.Threading.Thread.Sleep(200); } // Testdelay TODO: Remove for production

    //            while (readLength > 0 && ctx.IsCancellationRequested == false)
    //            {
    //                //Length = data.Length;

    //                long mBufferOffset = Offset + readTotal;

    //                int mBufferLength = await fetchTask;
    //                buffer1.CopyTo(buffer, 0);

    //                realOffset += mBufferLength;
    //                readTotal += mBufferLength;
    //                readLength = (int)Math.Min(Length - readTotal, (long)this.chunkSize);

    //                if (readLength > 0)
    //                {
    //                    fetchTask = fetchFile.ReadAsync(buffer1, 0, realOffset, readLength);
    //                }

    //                SyncProviderUtils.ApplyTransferStateToFile(this.SyncContext.ConnectionKey, data.TransferKey, Length, readTotal);

    //                unsafe
    //                {
    //                    fixed (byte* StackBuffer = stackBuffer)
    //                    {
    //                        int stackTransfered = 0;
    //                        while (stackTransfered < mBufferLength)
    //                        {
    //                            if (ctx.IsCancellationRequested) { return; }

    //                            int realStackSize = Math.Min(stackSize, mBufferLength - stackTransfered);

    //                            Marshal.Copy(buffer, stackTransfered, (IntPtr)StackBuffer, realStackSize);

    //                            var opInfo = new CldApi.CF_OPERATION_INFO()
    //                            {
    //                                Type = CldApi.CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_DATA,
    //                                ConnectionKey = this.SyncContext.ConnectionKey,
    //                                TransferKey = data.TransferKey,
    //                                RequestKey = data.RequestKey
    //                            };
    //                            opInfo.StructSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(opInfo);

    //                            var TpParam = new CldApi.CF_OPERATION_PARAMETERS.TRANSFERDATA
    //                            {
    //                                Length = realStackSize,
    //                                Offset = mBufferOffset + stackTransfered,
    //                                Buffer = (IntPtr)StackBuffer,
    //                                Flags = CldApi.CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE
    //                            };
    //                            var opParams = CldApi.CF_OPERATION_PARAMETERS.Create(TpParam);

    //                            var ret = CldApi.CfExecute(opInfo, ref opParams);
    //                            if (ret.Succeeded == false)
    //                            {
    //                                Debug.WriteLine(ret.ToString());
    //                                return;
    //                            }
    //                            //ret.ThrowIfFailed();

    //                            stackTransfered += realStackSize;
    //                        }
    //                    }
    //                }
    //            }


    //            var nextItem = this.fileFetchHelper.TakeNext(data.NormalizedPath, Offset);
    //            while (nextItem != null)
    //            {
    //                if ((nextItem.FileOffset + nextItem.Length) > (Offset + Length))
    //                {
    //                    break;
    //                }
    //                nextItem = this.fileFetchHelper.TakeNext(data.NormalizedPath, Offset);
    //            }
    //            if (nextItem != null)
    //            {
    //                ctx = nextItem.CancellationTokenSource.Token;
    //                Offset = Math.Max((Offset + Length), nextItem.FileOffset);
    //                Length = nextItem.FileOffset + nextItem.Length - realOffset;
    //                Debug.WriteLine("  FETCH_DATA Next Block " + Offset);
    //            }
    //            else
    //            {
    //                exitLoop = true;
    //            }


    //            //rangeList = this.fileFetchHelper.TakeNextRange(data.NormalizedPath, Offset + Length, this.chunkSize);
    //            //if (rangeList.Count > 0)
    //            //{
    //            //    Length = rangeList.Max(a => (a.FileOffset + a.Length));
    //            //}
    //            //else
    //            //{
    //            //    exitLoop = true;
    //            //}
    //        }

    //        await fetchFile.CloseAsync();
    //    }


    //    if (ctx.IsCancellationRequested)
    //    {
    //        Debug.WriteLine("  FETCH_DATA CANCELED");
    //    }
    //    else
    //    {
    //        Debug.WriteLine("  FETCH_DATA Completed");
    //    }

    //}

    private async Task FetchDataAsync(SyncProviderUtils.FetchRange data)
    {
        var ctx = new CancellationTokenSource().Token; // data.CancellationTokenSource.Token;
        if (ctx.IsCancellationRequested) { return; }

        string relativePath = data.NormalizedPath.Remove(0, this.SyncContext.LocalRootFolderNormalized.Length).TrimStart(char.Parse("\\"));
        string targetFullPath = Path.Combine(this.SyncContext.LocalRootFolder, relativePath);

        Debug.WriteLine("Fetch DataRange " + data.RangeStart + @" - " + data.RangeEnd + @" / " + relativePath);


        using (iReadFileAsync fetchFile = SyncContext.ServerProvider.GetNewReadFile())
        {
            byte[] stackBuffer = new byte[stackSize];

            var openAsyncResult = await fetchFile.OpenAsync(new OpenAsyncParams()
            {
                RelativeFileName = relativePath,
                CancellationToken = ctx,
                ETag = null // TODO: ETAG ausfüllen
            });

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
                    int dataRead = await fetchFile.ReadAsync(buffer, 0, currentOffset, readLength);

                    if (data.RangeEnd == 0 || data.RangeEnd < currentOffset || data.RangeStart > currentOffset) { continue; }

                    SyncProviderUtils.ApplyTransferStateToFile(this.SyncContext.ConnectionKey, data.TransferKey, totalLength, currentOffset + dataRead);

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

                                var opInfo = new CldApi.CF_OPERATION_INFO()
                                {
                                    Type = CldApi.CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_DATA,
                                    ConnectionKey = this.SyncContext.ConnectionKey,
                                    TransferKey = data.TransferKey,
                                    RequestKey = new CldApi.CF_REQUEST_KEY()
                                };
                                opInfo.StructSize = (uint)Marshal.SizeOf(opInfo);

                                var TpParam = new CldApi.CF_OPERATION_PARAMETERS.TRANSFERDATA
                                {
                                    Length = realStackSize,
                                    Offset = currentOffset + stackTransfered,
                                    Buffer = (IntPtr)StackBuffer,
                                    Flags = CldApi.CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE
                                };
                                var opParams = CldApi.CF_OPERATION_PARAMETERS.Create(TpParam);

                                var ret = CldApi.CfExecute(opInfo, ref opParams);
                                if (ret.Succeeded == false)
                                {
                                    //380 -> ungültig
                                    //398 -> cancel
                                    Debug.WriteLine(ret.ToString());
                                    //return;
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

            //SyncProviderUtils.ApplyTransferStateToFile(this.SyncContext.ConnectionKey, data.TransferKey, 0, 0);
        }


        if (ctx.IsCancellationRequested)
        {
            Debug.WriteLine("  FETCH_DATA CANCELED");
        }
        else
        {
            Debug.WriteLine("  FETCH_DATA Completed");
        }

    }


    #region "Callback"
    public void FETCH_PLACEHOLDERS(in CldApi.CF_CALLBACK_INFO CallbackInfo, in CldApi.CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Debug.WriteLine("FETCH_PLACEHOLDERS");
        Debug.WriteLine(CallbackInfo.NormalizedPath);
        Debug.WriteLine("Priority: " + CallbackInfo.PriorityHint);

        CldApi.CF_OPERATION_INFO opInfo = CreateOPERATION_INFO(CallbackInfo, CldApi.CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_PLACEHOLDERS);
        var ctx = new CancellationTokenSource();


        FetchPlaceholdersCancellationTokens.TryAdd(CallbackInfo.NormalizedPath, ctx);

        FETCH_PLACEHOLDERS_Internal(GetRelativePath(CallbackInfo), opInfo, CallbackParameters.FetchPlaceholders.Pattern, ctx.Token);
    }
    public async void FETCH_PLACEHOLDERS_Internal(string RelativeFileName, CldApi.CF_OPERATION_INFO opInfo, string Pattern, CancellationToken cancellationToken)
    {
        using SafePlaceHolderList infos = new SafePlaceHolderList(); // Vanara.InteropServices.SafeNativeArray<CldApi.CF_PLACEHOLDER_CREATE_INFO> infos = new Vanara.InteropServices.SafeNativeArray<CldApi.CF_PLACEHOLDER_CREATE_INFO>();

        using (iFileListAsync fileList = SyncContext.ServerProvider.GetNewFileList())
        {
            await fileList.OpenAsync(RelativeFileName, cancellationToken);

            while (await fileList.MoveNextAsync())
            {
                infos.Add(CreatePlaceholder(fileList.Current));
                if (cancellationToken.IsCancellationRequested)
                    break;
            };

            await fileList.CloseAsync();
        }
        // TODO: Blockweise übertragung


        uint total = (uint)infos.Count;

        CldApi.CF_OPERATION_PARAMETERS.TRANSFERPLACEHOLDERS TpParam = new CldApi.CF_OPERATION_PARAMETERS.TRANSFERPLACEHOLDERS
        {
            PlaceholderArray = infos,
            Flags = CldApi.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_DISABLE_ON_DEMAND_POPULATION,
            PlaceholderCount = total,
            PlaceholderTotalCount = total,
            CompletionStatus = NTStatus.STATUS_SUCCESS
        };

        CldApi.CF_OPERATION_PARAMETERS opParams = CldApi.CF_OPERATION_PARAMETERS.Create(TpParam);

        HRESULT ret = CldApi.CfExecute(opInfo, ref opParams);


        Styletronix.CloudFilterApi.SetInSyncState(this.SyncContext.LocalRootFolder + "\\" + RelativeFileName, CldApi.CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC, true);
    }

    private class SafePlaceHolderList : Vanara.InteropServices.SafeNativeArray<CldApi.CF_PLACEHOLDER_CREATE_INFO>
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

    public void CANCEL_FETCH_PLACEHOLDERS(in CldApi.CF_CALLBACK_INFO CallbackInfo, in CldApi.CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Debug.WriteLine("CANCEL_FETCH_PLACEHOLDERS");
        Debug.WriteLine("Priority: " + CallbackInfo.PriorityHint);
        Debug.WriteLine(CallbackInfo.NormalizedPath);

        if (FetchPlaceholdersCancellationTokens.TryRemove(CallbackInfo.NormalizedPath, out CancellationTokenSource ctx))
        {
            ctx.Cancel();
            Debug.WriteLine("Canceled");
        }


    }

    public void FETCH_DATA(in CldApi.CF_CALLBACK_INFO CallbackInfo, in CldApi.CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Debug.WriteLine(@"FETCH_DATA: Priority " + CallbackInfo.PriorityHint +
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

    public void CANCEL_FETCH_DATA(in CldApi.CF_CALLBACK_INFO CallbackInfo, in CldApi.CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Debug.WriteLine(@"CANCEL_FETCH_DATA: " +
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

    public void NOTIFY_FILE_OPEN_COMPLETION(in CldApi.CF_CALLBACK_INFO CallbackInfo, in CldApi.CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        //Debug.WriteLine("NOTIFY_FILE_OPEN_COMPLETION");
        //Debug.WriteLine(CallbackInfo.NormalizedPath);
    }

    public void NOTIFY_FILE_CLOSE_COMPLETION(in CldApi.CF_CALLBACK_INFO CallbackInfo, in CldApi.CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Debug.WriteLine("NOTIFY_FILE_CLOSE_COMPLETION: " + CallbackInfo.NormalizedPath);
        //Debug.WriteLine(CallbackInfo.NormalizedPath);
    }

    public void NOTIFY_DELETE(in CldApi.CF_CALLBACK_INFO CallbackInfo, in CldApi.CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Debug.WriteLine("NOTIFY_DELETE");
        Debug.WriteLine(CallbackInfo.NormalizedPath);

        bool isDirectory = CallbackParameters.Delete.Flags.HasFlag(CldApi.CF_CALLBACK_DELETE_FLAGS.CF_CALLBACK_DELETE_FLAG_IS_DIRECTORY);
        CldApi.CF_OPERATION_INFO opInfo = CreateOPERATION_INFO(CallbackInfo, CldApi.CF_OPERATION_TYPE.CF_OPERATION_TYPE_ACK_DELETE);

        NOTIFY_DELETE_Internal(GetRelativePath(CallbackInfo), isDirectory, opInfo);
    }
    public async void NOTIFY_DELETE_Internal(string RelativeFileName, bool isDirectory, CldApi.CF_OPERATION_INFO opInfo)
    {
        NTStatus status;

        var result = await SyncContext.ServerProvider.DeleteFileAsync(RelativeFileName, isDirectory);
        if (result.Succeeded)
        {
            status = NTStatus.STATUS_SUCCESS;
        }
        else
        {
            status = NTStatus.STATUS_ACCESS_DENIED;
        }

        CldApi.CF_OPERATION_PARAMETERS opParams = CldApi.CF_OPERATION_PARAMETERS.Create(new CldApi.CF_OPERATION_PARAMETERS.ACKDELETE
        {
            Flags = CldApi.CF_OPERATION_ACK_DELETE_FLAGS.CF_OPERATION_ACK_DELETE_FLAG_NONE,
            CompletionStatus = status
        });

        HRESULT ret = CldApi.CfExecute(opInfo, ref opParams);
        Debug.WriteLine(ret.Succeeded);
    }

    public void NOTIFY_DELETE_COMPLETION(in CldApi.CF_CALLBACK_INFO CallbackInfo, in CldApi.CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Debug.WriteLine("NOTIFY_DELETE_COMPLETION");
        Debug.WriteLine(CallbackInfo.NormalizedPath);
    }

    public void NOTIFY_RENAME(in CldApi.CF_CALLBACK_INFO CallbackInfo, in CldApi.CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Debug.WriteLine("NOTIFY_RENAME");
        Debug.WriteLine(CallbackInfo.NormalizedPath);
        Debug.WriteLine("Move to: " + CallbackParameters.Rename.TargetPath);

        if (CallbackParameters.Rename.TargetPath.StartsWith(@"\$Recycle.Bin\"))
        {

        }
        CldApi.CF_OPERATION_INFO opInfo = CreateOPERATION_INFO(CallbackInfo, CldApi.CF_OPERATION_TYPE.CF_OPERATION_TYPE_ACK_RENAME);

        NOTIFY_RENAME_Internal(GetRelativePath(CallbackInfo), GetRelativePath(CallbackParameters.Rename),
            CallbackParameters.Rename.Flags.HasFlag(CldApi.CF_CALLBACK_RENAME_FLAGS.CF_CALLBACK_RENAME_FLAG_IS_DIRECTORY), opInfo);

    }
    public async void NOTIFY_RENAME_Internal(string RelativeFileName, string RelativeFileNameDestination, bool isDirectory, CldApi.CF_OPERATION_INFO opInfo)
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


        CldApi.CF_OPERATION_PARAMETERS opParams = CldApi.CF_OPERATION_PARAMETERS.Create(new CldApi.CF_OPERATION_PARAMETERS.ACKRENAME
        {
            Flags = CldApi.CF_OPERATION_ACK_RENAME_FLAGS.CF_OPERATION_ACK_RENAME_FLAG_NONE,
            CompletionStatus = status
        });

        HRESULT ret = CldApi.CfExecute(opInfo, ref opParams);
        Debug.WriteLine(ret.Succeeded);
    }

    public void NOTIFY_RENAME_COMPLETION(in CldApi.CF_CALLBACK_INFO CallbackInfo, in CldApi.CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Debug.WriteLine("NOTIFY_RENAME_COMPLETION: " + CallbackInfo.NormalizedPath);
        //Debug.WriteLine(CallbackInfo.NormalizedPath);
    }

    public void NOTIFY_DEHYDRATE(in CldApi.CF_CALLBACK_INFO CallbackInfo, in CldApi.CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Debug.WriteLine("NOTIFY_DEHYDRATE");
        Debug.WriteLine(CallbackInfo.NormalizedPath);

        CldApi.CF_OPERATION_INFO opInfo = CreateOPERATION_INFO(CallbackInfo, CldApi.CF_OPERATION_TYPE.CF_OPERATION_TYPE_ACK_DEHYDRATE);

        Debug.WriteLine("Reason: " + CallbackParameters.Dehydrate.Reason.ToString());

        CldApi.CF_OPERATION_PARAMETERS opParams = CldApi.CF_OPERATION_PARAMETERS.Create(new CldApi.CF_OPERATION_PARAMETERS.ACKDEHYDRATE
        {
            Flags = CldApi.CF_OPERATION_ACK_DEHYDRATE_FLAGS.CF_OPERATION_ACK_DEHYDRATE_FLAG_NONE,
            CompletionStatus = NTStatus.STATUS_SUCCESS,
        });

        //TODO: Check if Dehydrate is allowed.

        HRESULT ret = CldApi.CfExecute(opInfo, ref opParams);
        Debug.WriteLine(ret.ToString());
    }

    public void NOTIFY_DEHYDRATE_COMPLETION(in CldApi.CF_CALLBACK_INFO CallbackInfo, in CldApi.CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Debug.WriteLine("NOTIFY_DEHYDRATE_COMPLETION");
        Debug.WriteLine(CallbackInfo.NormalizedPath);
    }

    public void VALIDATE_DATA(in CldApi.CF_CALLBACK_INFO CallbackInfo, in CldApi.CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Debug.WriteLine("VALIDATE_DATA");
        Debug.WriteLine(CallbackInfo.NormalizedPath);

        CldApi.CF_OPERATION_INFO opInfo = CreateOPERATION_INFO(CallbackInfo, CldApi.CF_OPERATION_TYPE.CF_OPERATION_TYPE_ACK_DATA);

        CldApi.CF_OPERATION_PARAMETERS opParams = CldApi.CF_OPERATION_PARAMETERS.Create(new CldApi.CF_OPERATION_PARAMETERS.ACKDATA
        {
            Flags = CldApi.CF_OPERATION_ACK_DATA_FLAGS.CF_OPERATION_ACK_DATA_FLAG_NONE,
            CompletionStatus = NTStatus.STATUS_SUCCESS,
            Length = CallbackParameters.ValidateData.RequiredLength,
            Offset = CallbackParameters.ValidateData.RequiredFileOffset
        });

        //TODO: Check if File on Server is in Sync.


        HRESULT ret = CldApi.CfExecute(opInfo, ref opParams);
        Debug.WriteLine(ret.ToString());
    }
    #endregion


    private CldApi.CF_OPERATION_INFO CreateOPERATION_INFO(in CldApi.CF_CALLBACK_INFO CallbackInfo, CldApi.CF_OPERATION_TYPE OperationType)
    {
        CldApi.CF_OPERATION_INFO opInfo = new CldApi.CF_OPERATION_INFO()
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
    private CldApi.CF_PLACEHOLDER_CREATE_INFO CreatePlaceholder(Placeholder FileInfo)
    {
        CldApi.CF_PLACEHOLDER_CREATE_INFO cfInfo = new CldApi.CF_PLACEHOLDER_CREATE_INFO();

        //string fileIdString = FileInfo.RelativeFileName;
        //if (FileInfo.FileAttributes.HasFlag(System.IO.FileAttributes.Directory))
        //{
        //    fileIdString += "\\";
        //}
        //SyncProviderUtils.AddFileIdentity(ref cfInfo, fileIdString);

        SyncProviderUtils.AddFileIdentity(ref cfInfo, FileInfo);
        cfInfo.RelativeFileName = FileInfo.RelativeFileName;
        cfInfo.FsMetadata = new CldApi.CF_FS_METADATA
        {
            FileSize = FileInfo.FileSize,
            BasicInfo = new Kernel32.FILE_BASIC_INFO
            {
                FileAttributes = (FileFlagsAndAttributes)FileInfo.FileAttributes,
                CreationTime = FileInfo.CreationTime.ToFileTimeStruct(),
                LastWriteTime = FileInfo.LastWriteTime.ToFileTimeStruct(),
                LastAccessTime = FileInfo.LastAccessTime.ToFileTimeStruct(),
                ChangeTime = FileInfo.LastWriteTime.ToFileTimeStruct()
            }
        };
        cfInfo.Flags = CldApi.CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_MARK_IN_SYNC;

        return cfInfo;
    }
    private string GetRelativePath(string fullPath)
    {
        if (fullPath.StartsWith(this.SyncContext.LocalRootFolder, StringComparison.CurrentCultureIgnoreCase))
        {
            return fullPath.Remove(0, this.SyncContext.LocalRootFolder.Length + 1);
        }
        else
        {
            throw new NotSupportedException("Pad nicht unterstützt: " + fullPath);
        }
    }
    private string GetRelativePath(in CldApi.CF_CALLBACK_INFO callbackInfo)
    {
        if (callbackInfo.NormalizedPath.StartsWith(this.SyncContext.LocalRootFolderNormalized, StringComparison.CurrentCultureIgnoreCase))
        {
            string relativePath = callbackInfo.NormalizedPath.Remove(0, this.SyncContext.LocalRootFolderNormalized.Length);
            return relativePath.TrimStart(char.Parse("\\"));
        }
        return callbackInfo.NormalizedPath;
    }
    private string GetRelativePath(in CldApi.CF_CALLBACK_PARAMETERS.RENAME callbackInfo)
    {
        if (callbackInfo.TargetPath.StartsWith(this.SyncContext.LocalRootFolderNormalized, StringComparison.CurrentCultureIgnoreCase))
        {
            string relativePath = callbackInfo.TargetPath.Remove(0, this.SyncContext.LocalRootFolderNormalized.Length);
            return relativePath.TrimStart(char.Parse("\\"));
        }

        return callbackInfo.TargetPath;
    }
    private string GetLocalFullPath(in CldApi.CF_CALLBACK_INFO callbackInfo)
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


