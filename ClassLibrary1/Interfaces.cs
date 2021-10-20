using System;
using System.IO;
using System.Threading.Tasks;

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
        public Placeholder Placeholder;
    }
    public class ReadFileReadResult : GenericResult
    {
        public int BytesRead;
    }
    public class ReadFileCloseResult : GenericResult
    {
    }

    public class WriteFileOpenResult : GenericResult
    {
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
        public Placeholder Placeholder;
    }

    public enum CloudExceptions
    {
        Offline = 1,
        FileOrDirectoryNotFound = 2,
        AccessDenied = 3
    }

    public interface IServerFileProvider
    {
        public SyncProviderUtils.SyncContext SyncContext { get; set; }

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
        public Task OpenAsync(string RelativeFileName, System.Threading.CancellationToken ctx);

        public Task<bool> MoveNextAsync();

        public Placeholder Current { get; }

        public Task CloseAsync();
    }

    [Flags]
    public enum UploadMode : short
    {
        FullFile = 0,
        Resume = 1,
        PartialUpdate = 2
    }

    public class Placeholder : BasicFileInfo
    {
        public Placeholder(FileSystemInfo fileInfo)
        {
            this.RelativeFileName = fileInfo.Name;
            this.FileSize = fileInfo.Attributes.HasFlag(FileAttributes.Directory) ? 0 : ((FileInfo)fileInfo).Length;
            this.FileAttributes = fileInfo.Attributes;
            this.CreationTime = fileInfo.CreationTime;
            this.LastWriteTime = fileInfo.LastWriteTime;
            this.LastAccessTime = fileInfo.LastAccessTime;
            this.ChangeTime = fileInfo.LastWriteTime;
            this.ETag = "_" + fileInfo.LastWriteTime.ToUniversalTime().Ticks + "_" + this.FileSize;
        }
        public Placeholder(string fullPath)
        {
            var fileInfo = new FileInfo(fullPath);

            this.RelativeFileName = fileInfo.Name;
            this.FileSize = fileInfo.Attributes.HasFlag(FileAttributes.Directory) ? 0 : ((FileInfo)fileInfo).Length;
            this.FileAttributes = fileInfo.Attributes;
            this.CreationTime = fileInfo.CreationTime;
            this.LastWriteTime = fileInfo.LastWriteTime;
            this.LastAccessTime = fileInfo.LastAccessTime;
            this.ChangeTime = fileInfo.LastWriteTime;
            this.ETag = "_" + fileInfo.LastWriteTime.ToUniversalTime().Ticks + "_" + this.FileSize;
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
    public class CreateFileResult: GenericResult
    {
        public Placeholder Placeholder;
    }

    public class GenericResult
    {
        public GenericResult()
        {
            this.Succeeded = true;
            this.Status = CloudFilterApi.NtStatus.STATUS_SUCCESS;
            this.Message = this.Status.ToString();
        }
        public GenericResult(Exception ex)
        {
            this.SetException(ex);
        }
        public GenericResult(CloudExceptions ex)
        {
            this.SetException(ex);
        }
        public GenericResult(CloudFilterApi.NtStatus status)
        {
            if (status == CloudFilterApi.NtStatus.STATUS_SUCCESS)
                this.Succeeded = true;
            this.Status = status;
            this.Message = this.Status.ToString();
        }
        public GenericResult(int ntStatus)
        {
            this.Status = (CloudFilterApi.NtStatus)ntStatus;
            this.Succeeded = (ntStatus == 0);
            this.Message = this.Status.ToString();
        }
        public GenericResult(CloudFilterApi.NtStatus status, string message)
        {
            if (status == CloudFilterApi.NtStatus.STATUS_SUCCESS)
                this.Succeeded = true;
            this.Status = status;
            this.Message = message;
        }

        public static implicit operator bool(GenericResult instance)
        {
            return instance.Succeeded;
        }

        public void SetException(Exception ex)
        {
            this.Succeeded = false;
            this.Message = ex.ToString();

            this.Status = ex switch
            {
                FileNotFoundException => CloudFilterApi.NtStatus.STATUS_NOT_A_CLOUD_FILE,
                DirectoryNotFoundException => CloudFilterApi.NtStatus.STATUS_NOT_A_CLOUD_FILE,
                UnauthorizedAccessException => CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_ACCESS_DENIED,
                IOException => CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_IN_USE,
                NotSupportedException => CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_NOT_SUPPORTED,
                InvalidOperationException => CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_INVALID_REQUEST,
                _ => CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_UNSUCCESSFUL
            };
        }
        public void SetException(CloudExceptions ex)
        {
            this.Succeeded = false;
            this.Message = ex.ToString();

            this.Status = ex switch
            {
                CloudExceptions.Offline => CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE,
                CloudExceptions.FileOrDirectoryNotFound => CloudFilterApi.NtStatus.STATUS_NOT_A_CLOUD_FILE,
                CloudExceptions.AccessDenied => CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_ACCESS_DENIED,
                _ => CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_UNSUCCESSFUL
            };
        }

        public bool Succeeded;
        public string Message;
        public CloudFilterApi.NtStatus Status;
        public void ThrowOnFailure()
        {
            if (!this.Succeeded)
            {
                throw new System.ComponentModel.Win32Exception((int)this.Status, this.Message);
            }
        }
    }

    public class BasicSyncProviderInfo
    {
        public Guid ProviderId;
        public string CLSID;
        public string ProviderName;
        public string ProviderVersion;
    }
    public class SyncProviderParameters
    {
        public BasicSyncProviderInfo ProviderInfo;
        public IServerFileProvider ServerProvider;
        public string LocalDataPath;
    }
}