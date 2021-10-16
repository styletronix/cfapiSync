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
    public class OpenAsyncResult
    {
        public string ETag;
    }

    public interface iServerFileProvider
    {
        public SyncProviderUtils.SyncContext SyncContext { get; set; }

        public iReadFileAsync GetNewReadFile();

        public iWriteFileAsync GetNewWriteFile();

        public iFileListAsync GetNewFileList();

        public Task<DeleteFileResult> DeleteFileAsync(string RelativeFileName, bool isDirectory);
        public Task<MoveFileResult> MoveFileAsync(string RelativeFileName, string RelativeDestination, bool isDirectory);
        public Task<Placeholder> GetFileInfo(string RelativeFileName, bool isDirectory);
    }

    public interface iReadFileAsync : IDisposable
    {

        /// <summary>
        /// This is called at the beginning of a file Transfer, just after instance creation and bevore the first call to ReadAsync()
        /// </summary>
        /// <param name="RelativeFileName"></param>
        /// <param name="srcFolder"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public Task<OpenAsyncResult> OpenAsync(OpenAsyncParams e);

        /// <summary>
        /// Read a maximum of <paramref name="count"/> bytes of the file, starting at byte <paramref name="offset"/> and write the data to the supplied <paramref name="buffer"/>
        /// </summary>
        /// <param name="buffer">The Buffer, where the data should be stored</param>
        /// <param name="offsetBuffer">Offset of the <paramref name="buffer"/> where to start writing to</param>
        /// <param name="offset">Offset of the File to start reading</param>
        /// <param name="count">Maximum Bytes to read</param>
        /// <returns>Bytes read and written to the <paramref name="buffer"/></returns>
        public System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offsetBuffer, long offset, int count);

        public System.Threading.Tasks.Task CloseAsync();
    }

    public interface iWriteFileAsync : IDisposable
    {
        public UploadMode supportedUploadModes { get; }
        public Task<OpenAsyncResult> OpenAsync(OpenAsyncParams e);
        public Task WriteAsync(byte[] buffer, int offsetBuffer, long offset, int count);
        public Task<Placeholder> CloseAsync(bool completed);
    }

    public interface iFileListAsync : IDisposable
    {
        public System.Threading.Tasks.Task OpenAsync(string RelativeFileName, System.Threading.CancellationToken ctx);

        public System.Threading.Tasks.Task<bool> MoveNextAsync();

        public Placeholder Current { get; }

        public System.Threading.Tasks.Task CloseAsync();
    }

    public enum UploadMode
    {
        fullFile = 1,
        resume = 2,
        partialUpdate = 4
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
            this.ETag = "_" + fileInfo.LastWriteTime.Ticks + "_";
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
            this.ETag = "_" + fileInfo.LastWriteTime.Ticks + "_";
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

    public class GenericResult
    {
        public bool Succeeded;
        public string Message;
        public Exception Exception;
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
        public iServerFileProvider ServerProvider;
        public string LocalDataPath;
    }
}