using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Styletronix.CloudFilterApi;
using static Vanara.PInvoke.CldApi;

namespace Styletronix.CloudSyncProvider
{
    public class OpenAsyncParams
    {
        public string RelativeFileName;
        public Placeholder FileInfo;
        public System.Threading.CancellationToken CancellationToken;
        public UploadMode mode;
        public string ETag;
    }
    public class ReadFileOpenResult : GenericResult
    {
        public ReadFileOpenResult() { }
        public ReadFileOpenResult(Placeholder placeholder)
        {
            Placeholder = placeholder;
        }
        public ReadFileOpenResult(NtStatus status)
        {
            Status = status;
        }

        public Placeholder Placeholder;
    }
    public class ReadFileReadResult : GenericResult
    {
        public ReadFileReadResult() { }
        public ReadFileReadResult(int bytesRead)
        {
            BytesRead = bytesRead;
        }
        public ReadFileReadResult(NtStatus status)
        {
            Status = status;
        }

        public int BytesRead;
    }
    public class ReadFileCloseResult : GenericResult
    {
    }

    public class WriteFileOpenResult : GenericResult
    {
        public WriteFileOpenResult() { }
        public WriteFileOpenResult(Placeholder placeholder)
        {
            Placeholder = placeholder;
        }
        public WriteFileOpenResult(NtStatus status)
        {
            Status = status;
        }
        public Placeholder Placeholder;
    }
    public class WriteFileWriteResult : GenericResult
    {

    }
    public class WriteFileCloseResult : GenericResult
    {
        public Placeholder Placeholder;
    }

    public class GetFileInfoResult : GenericResult
    {
        public GetFileInfoResult() { }
        public GetFileInfoResult(NtStatus status)
        {
            Status = status;
        }
        public Placeholder Placeholder;
    }
    /// <summary>
    /// Preferred Settings are used by the Sync Provider to change parmeters depending on the used ServerProvider.
    /// </summary>
    public class PreferredSettings
    {
        /// <summary>
        /// Minimum chunk size for file Up-/Download
        /// </summary>
        public int MinChunkSize = 4096;
        /// <summary>
        /// Maximum chunk Size for file Up-/Download
        /// </summary>
        public int MaxChunkSize = int.MaxValue;
        /// <summary>
        /// Allows the SyncProvider to update parts of an existing file instead replacing the entire file.
        /// ETag is used to validate the file consistence before starting partial updates.
        /// </summary>
        public bool AllowPartialUpdate = true;

        public bool PreferFullDirSync = false;
    }


    public interface IServerFileProvider
    {
        public event EventHandler<ServerProviderStateChangedEventArgs> ServerProviderStateChanged;
        public event EventHandler<FileChangedEventArgs> FileChanged;

        public SyncContext SyncContext { get; set; }
        public PreferredSettings PreferredServerProviderSettings { get; }
        public ServerProviderStatus Status { get; }

        /// <summary>
        /// Establish a connection to the Server to check Authentication and for receiving realtime Updates.
        /// ServerProvider is responsible for authentication, reconnect, timeout handling....
        /// </summary>
        /// <returns>Status if Connection was successful. If not successfull, Connect will not be called again and the ServerProvider is responsible to report  later successfull connect.</returns>
        public Task<GenericResult> Connect();
        /// <summary>
        /// Disconnect from Server and stop receiving realtime Updates.
        /// </summary>
        /// <returns></returns>
        public Task<GenericResult> Disconnect();

        public IReadFileAsync GetNewReadFile();
        public IWriteFileAsync GetNewWriteFile();
        public IFileListAsync GetNewFileList();

        /// <summary>
        /// Delete a File or Directory
        /// </summary>
        /// <param name="RelativeFileName"></param>
        /// <param name="isDirectory"></param>
        /// <returns></returns>
        public Task<DeleteFileResult> DeleteFileAsync(string RelativeFileName, bool isDirectory);
        /// <summary>
        /// Move or Rename a File or Directory
        /// </summary>
        /// <param name="RelativeFileName"></param>
        /// <param name="RelativeDestination"></param>
        /// <param name="isDirectory"></param>
        /// <returns></returns>
        public Task<MoveFileResult> MoveFileAsync(string RelativeFileName, string RelativeDestination, bool isDirectory);
        /// <summary>
        /// Get Placeholder-Data for a File or Directory
        /// </summary>
        /// <param name="RelativeFileName"></param>
        /// <param name="isDirectory"></param>
        /// <returns></returns>
        public Task<GetFileInfoResult> GetFileInfo(string RelativeFileName, bool isDirectory);
        /// <summary>
        /// Create a new File or Directory. It is likely that CreateFileAsync is not called to create new files. 
        /// Files should created by calling IWriteFileAsync.OpenAsync();
        /// </summary>
        /// <param name="RelativeFileName"></param>
        /// <param name="isDirectory"></param>
        /// <returns></returns>
        public Task<CreateFileResult> CreateFileAsync(string RelativeFileName, bool isDirectory);
    }
    public interface IReadFileAsync : IDisposable, IAsyncDisposable
    {

        /// <summary>
        /// This is called at the beginning of a file Transfer, just after instance creation and bevore the first call to ReadAsync()
        /// </summary>
        /// <param name="RelativeFileName"></param>
        /// <param name="srcFolder"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public Task<ReadFileOpenResult> OpenAsync(OpenAsyncParams e);

        /// <summary>
        /// Read a maximum of <paramref name="count"/> bytes of the file, starting at byte <paramref name="offset"/> and write the data to the supplied <paramref name="buffer"/>
        /// </summary>
        /// <param name="buffer">The Buffer, where the data should be stored</param>
        /// <param name="offsetBuffer">Offset of the <paramref name="buffer"/> where to start writing to</param>
        /// <param name="offset">Offset of the File to start reading</param>
        /// <param name="count">Maximum Bytes to read</param>
        /// <returns>Bytes read and written to the <paramref name="buffer"/></returns>
        public Task<ReadFileReadResult> ReadAsync(byte[] buffer, int offsetBuffer, long offset, int count);

        public Task<ReadFileCloseResult> CloseAsync();
    }
    public interface IWriteFileAsync : IDisposable, IAsyncDisposable
    {
        public UploadMode SupportedUploadModes { get; }
        public Task<WriteFileOpenResult> OpenAsync(OpenAsyncParams e);
        public Task<WriteFileWriteResult> WriteAsync(byte[] buffer, int offsetBuffer, long offset, int count);
        public Task<WriteFileCloseResult> CloseAsync(bool completed);
    }
    public interface IFileListAsync : IDisposable, IAsyncDisposable
    {
        public Task<GenericResult> OpenAsync(string RelativeFileName, System.Threading.CancellationToken ctx);

        public Task<GetNextResult> GetNextAsync();

        public Task<GenericResult> CloseAsync();
    }


    [Flags]
    public enum UploadMode : short
    {
        FullFile = 0,
        Resume = 1,
        PartialUpdate = 2
    }
    public enum ServerProviderStatus
    {
        Disabled = 0, // Connection is disabled. No retry possible. 
        AuthenticationRequired = 1, // Authentication is required. Retry based on ServerProvider decision

        Failed = 10, // Retry based on ServerProvider decision
        Disconnected = 11, // Retry required by ServerProvider.
        Connecting = 12, // ServerProvider tries to connect.
        Connected = 13,// ServerProvider connected to cloud
    }
    public enum FileChangedType
    {
        Created,
        Deleted
    }
    public enum CloudExceptions
    {
        Offline = 1,
        FileOrDirectoryNotFound = 2,
        AccessDenied = 3
    }
    public enum SyncMode
    {
        Local = 0,
        Full = 1,
        FullQueue = 2
    }

    //The Dynamic Placeholder provides a way to supply a already downloaded remote placeholder or to get the remote placeholder on demand instead of always downloading remote date even if it may not be required.
    public class DynamicServerPlaceholder
    {
        private readonly string _relativePath;
        private readonly SyncContext _syncContext;
        private Placeholder _placeholder;
        private readonly bool _isDirectory;

        public DynamicServerPlaceholder(string relativePath, bool isDirectory, SyncContext syncContext)
        {
            this._relativePath = relativePath;
            this._syncContext = syncContext;
            this._isDirectory = isDirectory;
        }
        public DynamicServerPlaceholder(Placeholder placeholder)
        {
            this._placeholder = placeholder;
        }

        public async Task<Placeholder> GetPlaceholder()
        {
            if (_placeholder == null && !string.IsNullOrWhiteSpace(this._relativePath))
            {
                if (!this._syncContext.SyncProvider.IsExcludedFile(this._relativePath))
                {
                    Styletronix.Debug.WriteLine("ServerProvider.GetFileInfo: " + this._relativePath);
                    GetFileInfoResult getFileResult = await _syncContext.ServerProvider.GetFileInfo(this._relativePath, this._isDirectory);
                    _placeholder = getFileResult.Placeholder;

                    // Handle new local file or file on server deleted
                    if (getFileResult.Status == NtStatus.STATUS_NOT_A_CLOUD_FILE)
                    {
                        // File not found on Server.... New local file or File deleted on Server.
                        // Do not raise any exception and continue processing
                        _placeholder = null;
                    }
                    else
                    {
                        getFileResult.ThrowOnFailure();
                    }
                }
            }

            return _placeholder;
        }

        public static explicit operator DynamicServerPlaceholder(Placeholder placeholder)
        {
            return new DynamicServerPlaceholder(placeholder);
        }
    }
    public class Placeholder : BasicFileInfo
    {
        public Placeholder(FileSystemInfo fileInfo)
        {
            RelativeFileName = fileInfo.Name;
            FileSize = fileInfo.Attributes.HasFlag(FileAttributes.Directory) ? 0 : ((FileInfo)fileInfo).Length;
            FileAttributes = fileInfo.Attributes;
            CreationTime = fileInfo.CreationTime;
            LastWriteTime = fileInfo.LastWriteTime;
            LastAccessTime = fileInfo.LastAccessTime;
            ChangeTime = fileInfo.LastWriteTime;
            ETag = "_" + fileInfo.LastWriteTime.ToUniversalTime().Ticks + "_" + FileSize;
        }
        public Placeholder(string fullPath)
        {
            FileInfo fileInfo = new(fullPath);

            RelativeFileName = fileInfo.Name;
            FileSize = fileInfo.Attributes.HasFlag(FileAttributes.Directory) ? 0 : fileInfo.Length;
            FileAttributes = fileInfo.Attributes;
            CreationTime = fileInfo.CreationTime;
            LastWriteTime = fileInfo.LastWriteTime;
            LastAccessTime = fileInfo.LastAccessTime;
            ChangeTime = fileInfo.LastWriteTime;
            ETag = "_" + fileInfo.LastWriteTime.ToUniversalTime().Ticks + "_" + FileSize;
        }
        public Placeholder(string fullPath, string relativeFileName)
        {
            FileInfo fileInfo = new(fullPath);

            RelativeFileName = relativeFileName;
            FileSize = fileInfo.Attributes.HasFlag(FileAttributes.Directory) ? 0 : fileInfo.Length;
            FileAttributes = fileInfo.Attributes;
            CreationTime = fileInfo.CreationTime;
            LastWriteTime = fileInfo.LastWriteTime;
            LastAccessTime = fileInfo.LastAccessTime;
            ChangeTime = fileInfo.LastWriteTime;
            ETag = "_" + fileInfo.LastWriteTime.ToUniversalTime().Ticks + "_" + FileSize;
        }
        public Placeholder(string relativeFileName, bool isDirectory)
        {
            RelativeFileName = relativeFileName;
            FileAttributes = isDirectory ? FileAttributes.Directory : FileAttributes.Normal;
        }

        public string RelativeFileName;
        public long FileSize;
        public string ETag;
    }
    public class BasicFileInfo
    {
        public FileAttributes FileAttributes;
        public DateTime CreationTime;
        public DateTime LastWriteTime;
        public DateTime LastAccessTime;
        public DateTime ChangeTime;
    }
    public class DeleteFileResult : GenericResult
    {

    }
    public class MoveFileResult : GenericResult
    {

    }
    public class CreateFileResult : GenericResult
    {
        public Placeholder Placeholder;
    }
    public class GenericResult
    {
        public GenericResult()
        {
            Succeeded = true;
            Status = CloudFilterApi.NtStatus.STATUS_SUCCESS;
            Message = Status.ToString();
        }
        public GenericResult(bool succeeded)
        {
            Succeeded = succeeded;
            Status = succeeded ? CloudFilterApi.NtStatus.STATUS_SUCCESS : CloudFilterApi.NtStatus.STATUS_UNSUCCESSFUL;
            Message = Status.ToString();
        }
        public GenericResult(Exception ex)
        {
            SetException(ex);
        }
        public GenericResult(CloudExceptions ex)
        {
            SetException(ex);
        }
        public GenericResult(CloudFilterApi.NtStatus status)
        {
            Succeeded = (status == CloudFilterApi.NtStatus.STATUS_SUCCESS);
            Status = status;
            Message = Status.ToString();
        }
        public GenericResult(int ntStatus)
        {
            Status = (CloudFilterApi.NtStatus)ntStatus;
            Succeeded = (ntStatus == 0);
            Message = Status.ToString();
        }
        public GenericResult(CloudFilterApi.NtStatus status, string message)
        {
            Succeeded = (status == CloudFilterApi.NtStatus.STATUS_SUCCESS);
            Status = status;
            Message = message;
        }

        public static implicit operator bool(GenericResult instance)
        {
            return instance.Succeeded;
        }

        public void SetException(Exception ex)
        {
            Succeeded = false;
            Message = ex.ToString();

            Status = ex switch
            {
                FileNotFoundException => CloudFilterApi.NtStatus.STATUS_NOT_A_CLOUD_FILE,
                DirectoryNotFoundException => CloudFilterApi.NtStatus.STATUS_NOT_A_CLOUD_FILE,
                UnauthorizedAccessException => CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_ACCESS_DENIED,
                IOException => CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_IN_USE,
                NotSupportedException => CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_NOT_SUPPORTED,
                InvalidOperationException => CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_INVALID_REQUEST,
                OperationCanceledException => CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_REQUEST_CANCELED,
                _ => CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_UNSUCCESSFUL
            };
        }
        public void SetException(CloudExceptions ex)
        {
            Succeeded = false;
            Message = ex.ToString();

            Status = ex switch
            {
                CloudExceptions.Offline => CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE,
                CloudExceptions.FileOrDirectoryNotFound => CloudFilterApi.NtStatus.STATUS_NOT_A_CLOUD_FILE,
                CloudExceptions.AccessDenied => CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_ACCESS_DENIED,
                _ => CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_UNSUCCESSFUL
            };
        }

        public bool Succeeded;
        public string Message;

        private CloudFilterApi.NtStatus _status;
        public CloudFilterApi.NtStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                Succeeded = (_status == CloudFilterApi.NtStatus.STATUS_SUCCESS);
            }
        }

        public void ThrowOnFailure()
        {
            if (!Succeeded)
            {
                throw new System.ComponentModel.Win32Exception((int)Status, Message);
            }
        }
    }
    public class GenericResult<T> : GenericResult
    {
        public GenericResult()
        {
            Succeeded = true;
            Status = CloudFilterApi.NtStatus.STATUS_SUCCESS;
            Message = Status.ToString();
        }
        public GenericResult(Exception ex)
        {
            SetException(ex);
        }
        public GenericResult(CloudExceptions ex)
        {
            SetException(ex);
        }
        public GenericResult(CloudFilterApi.NtStatus status)
        {
            Succeeded = (status == CloudFilterApi.NtStatus.STATUS_SUCCESS);
            Status = status;
            Message = Status.ToString();
        }
        public GenericResult(int ntStatus)
        {
            Status = (CloudFilterApi.NtStatus)ntStatus;
            Succeeded = (ntStatus == 0);
            Message = Status.ToString();
        }
        public GenericResult(CloudFilterApi.NtStatus status, string message)
        {
            Succeeded = (status == CloudFilterApi.NtStatus.STATUS_SUCCESS);
            Status = status;
            Message = message;
        }

        public T Data;
    }
    public class GetNextResult : GenericResult
    {
        public GetNextResult() { }
        public GetNextResult(CloudFilterApi.NtStatus status)
        {
            Status = status;
        }
        public Placeholder Placeholder;
    }

    public class BasicSyncProviderInfo
    {
        public Guid ProviderId;
        public string ProviderName;
        public string ProviderVersion;
    }
    public class SyncProviderParameters
    {
        public BasicSyncProviderInfo ProviderInfo;
        public IServerFileProvider ServerProvider;
        public string LocalDataPath;
    }
    public class ServerProviderStateChangedEventArgs : EventArgs
    {
        public ServerProviderStateChangedEventArgs() { }
        public ServerProviderStateChangedEventArgs(ServerProviderStatus status)
        {
            Status = status;
            Message = status.ToString();
        }
        public ServerProviderStateChangedEventArgs(ServerProviderStatus status, string message)
        {
            Status = status;
            Message = message;
        }

        public string Message;
        public ServerProviderStatus Status;
    }
    public class FileChangedEventArgs : EventArgs
    {
        public WatcherChangeTypes ChangeType;
        public bool ResyncSubDirectories;
        public string OldRelativePath;
        public Placeholder Placeholder;
    }
    public class FailedData
    {
        public DateTime LastTry;
        public DateTime NextTry;
        public Exception LastException;
        public int RetryCount;
        public SyncMode SyncMode;
    }

    public class FileProgressEventArgs : EventArgs
    {
        private long _BytesCompleted;
        private long _BytesTotal;

        public string relativeFilePath;
        public short Progress;

        public FileProgressEventArgs(string relativeFilePath, long fileBytesCompleted, long fileBytesTotal)
        {
            this.relativeFilePath = relativeFilePath;
            FileBytesCompleted = fileBytesCompleted;
            FileBytesTotal = fileBytesTotal;
        }

        public long FileBytesCompleted
        {
            get => _BytesCompleted;
            set
            {
                _BytesCompleted = value;
                UpdateProgress();
            }
        }
        public long FileBytesTotal
        {
            get => _BytesTotal;
            set
            {
                _BytesTotal = value;
                UpdateProgress();
            }
        }
        private void UpdateProgress()
        {
            try
            {
                if (FileBytesTotal == 0)
                {
                    Progress = 0;
                }
                else
                {
                    short x = (short)((FileBytesCompleted / (float)FileBytesTotal) * 100);
                    if (x > 100)
                    {
                        x = 100;
                    }

                    Progress = x;
                }
            }
            catch (Exception)
            {
                Progress = 0;
            }
        }
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
        public SyncProvider SyncProvider;
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
    }
    public class FetchRange
    {
        public FetchRange() { }
        public FetchRange(DataActions data)
        {
            NormalizedPath = data.NormalizedPath;
            PriorityHint = data.PriorityHint;
            RangeStart = data.FileOffset;
            RangeEnd = data.FileOffset + data.Length;
            TransferKey = data.TransferKey;
        }

        public long RangeStart;
        public long RangeEnd;
        public string NormalizedPath;
        public CF_TRANSFER_KEY TransferKey;
        public byte PriorityHint;
    }
    public class DeleteAction
    {
        public CF_OPERATION_INFO OpInfo;
        public string RelativePath;
        public bool IsDirectory;
    }
    public class SyncProviderStatusEventArgs : EventArgs
    {
        public long QueueLength;
        public long RetryQueueLength;
        public ServerProviderStatus ServerProviderStatus;

        // TODO: Additional status messages
    }
}