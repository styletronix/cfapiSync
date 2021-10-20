using Styletronix.CloudSyncProvider;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class ServerProvider : IServerFileProvider
{
    #region "Server Parameter"

    public class ServerProviderParams
    {
        public string ServerPath;
        public bool UseRecycleBin;
        public bool UseRecycleBinForChangedFiles;
        public bool UseTempFilesForUpload;
    }
    internal ServerProviderParams Parameter;

    public ServerProvider(string ServerPath)
    {
        this.Parameter = new ServerProviderParams()
        {
            ServerPath = ServerPath,
            UseRecycleBin = true,
            UseRecycleBinForChangedFiles = true,
            UseTempFilesForUpload = true
        };

        if (System.IO.Directory.Exists(this.Parameter.ServerPath) == false) { System.IO.Directory.CreateDirectory(this.Parameter.ServerPath); };
    }

    #endregion


    #region "IHandleServerFiles"

    public SyncProviderUtils.SyncContext SyncContext { get; set; }

    public IReadFileAsync GetNewReadFile() => new ReadFileAsyncInternal(this);

    public IWriteFileAsync GetNewWriteFile() => new WriteFileAsyncInternal(this);

    public IFileListAsync GetNewFileList() => new FileListAsyncInternal(this);

    public Task<DeleteFileResult> DeleteFileAsync(string RelativeFileName, bool isDirectory)
    {
        DeleteFileResult deleteFileResult = new();

        try
        {
            this.DeleteOrMoveToRecycleBin(RelativeFileName, isDirectory);
        }
        catch (DirectoryNotFoundException ex)
        {
            // Directory already deleted?
            deleteFileResult.Message = ex.Message;
        }
        catch (FileNotFoundException ex)
        {
            // File already deleted?
            deleteFileResult.Message = ex.Message;
        }
        catch (Exception ex)
        {
            deleteFileResult.SetException(ex);
        }

        return Task.FromResult(deleteFileResult);
    }



    public Task<MoveFileResult> MoveFileAsync(string RelativeFileName, string RelativeDestination, bool isDirectory)
    {
        var fullPath = Path.Combine(this.Parameter.ServerPath, RelativeFileName);
        var fullPathDestination = Path.Combine(this.Parameter.ServerPath, RelativeDestination);

        MoveFileResult moveFileResult = new();

        try
        {
            if (isDirectory)
            {
                Directory.Move(fullPath, fullPathDestination);
            }
            else
            {
                File.Move(fullPath, fullPathDestination);
            }
        }
        catch (Exception ex)
        {
            moveFileResult.SetException(ex);
        }

        return Task.FromResult(moveFileResult);
    }

    public Task<GetFileInfoResult> GetFileInfo(string RelativeFileName, bool isDirectory)
    {
        GetFileInfoResult getFileInfoResult = new();

        var fullPath = Path.Combine(this.Parameter.ServerPath, RelativeFileName);

        try
        {
            if (isDirectory)
            {
                if (!Directory.Exists(fullPath)) { throw new DirectoryNotFoundException(RelativeFileName); }
            }
            else
            {
                if (!File.Exists(fullPath)) { throw new FileNotFoundException(RelativeFileName); }
            }

            getFileInfoResult.Placeholder = new Placeholder(fullPath);
        }
        catch (Exception ex)
        {
            getFileInfoResult.SetException(ex);
        }

        return Task.FromResult(getFileInfoResult);
    }

    public Task<CreateFileResult> CreateFileAsync(string RelativeFileName, bool isDirectory)
    {
        CreateFileResult createFileResult = new();

        var fullPath = Path.Combine(this.Parameter.ServerPath, RelativeFileName);

        try
        {
            if (isDirectory)
            {
                Directory.CreateDirectory(fullPath);
            }
            else
            {
                if (File.Exists(fullPath))
                {
                    createFileResult.Status = Styletronix.CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_IN_USE;
                    createFileResult.Message = "Datei existiert bereits";
                    createFileResult.Succeeded = false;
                }
                else
                {
                    using FileStream strm = File.Create(fullPath);
                    strm.Close();
                }

            }
            createFileResult.Placeholder = new(fullPath);
        }
        catch (Exception ex)
        {
            createFileResult.SetException(ex);
        }

        return Task.FromResult(createFileResult);
    }
    #endregion



    internal void MoveToRecycleBin(string relativePath, bool isDirectory)
    {
        string recyclePath = this.Parameter.ServerPath + @"\$Recycle.bin\" + relativePath;
        string fullPath = this.Parameter.ServerPath + @"\" + relativePath;

        recyclePath = Path.GetDirectoryName(recyclePath) + @"\(" + DateTime.Now.ToString("s").Replace(":", "_") + ") " + Path.GetFileName(recyclePath);

        if (!Directory.Exists(Path.GetDirectoryName(recyclePath))) { Directory.CreateDirectory(Path.GetDirectoryName(recyclePath)); }

        if (isDirectory)
        {
            Directory.Move(fullPath, recyclePath);
        }
        else
        {
            File.Move(fullPath, recyclePath);
        }
    }
    internal void DeleteOrMoveToRecycleBin(string relativePath, bool isDirectory)
    {
        if (this.Parameter.UseRecycleBin)
        {
            MoveToRecycleBin(relativePath, isDirectory);
        }
        else
        {
            string fullPath = Path.Combine(this.Parameter.ServerPath, relativePath);
            if (isDirectory)
            {
                Directory.Delete(fullPath, false);
            }
            else
            {
                File.Delete(fullPath);
            }
        }
    }


    internal class ReadFileAsyncInternal : IReadFileAsync
    {
        private ServerProvider Provider;
        private FileStream fileStream;
        private OpenAsyncParams _Params;
        public ReadFileAsyncInternal(ServerProvider provider)
        {
            this.Provider = provider;
        }

        public Task<ReadFileOpenResult> OpenAsync(OpenAsyncParams e)
        {
            this._Params = e;
            ReadFileOpenResult openResult = new();

            var fullPath = Path.Combine(this.Provider.Parameter.ServerPath, e.RelativeFileName);

            // Simulate "Offline" if Serverfolder not found.
            if (!Directory.Exists(this.Provider.Parameter.ServerPath))
            {
                openResult.SetException(CloudExceptions.Offline);
                goto skip;
            }

            try
            {
                if (!File.Exists(fullPath)) { throw new FileNotFoundException(e.RelativeFileName); }

                this.fileStream = File.OpenRead(fullPath);
                openResult.Placeholder = new(fullPath);
            }
            catch (Exception ex)
            {
                openResult.SetException(ex);
            }

        skip:
            return Task.FromResult(openResult);
        }

        public async Task<ReadFileReadResult> ReadAsync(byte[] buffer, int offsetBuffer, long offset, int count)
        {
            ReadFileReadResult readResult = new();

            try
            {
                fileStream.Position = offset;
                readResult.BytesRead = await fileStream.ReadAsync(buffer, offsetBuffer, count, this._Params.CancellationToken);
            }
            catch (Exception ex)
            {
                readResult.SetException(ex);
            }

            return readResult;
        }

        public Task<ReadFileCloseResult> CloseAsync()
        {
            ReadFileCloseResult closeResult = new();

            try
            {
                fileStream.Close();
                this.isClosed = true;
            }
            catch (Exception ex)
            {
                closeResult.SetException(ex);
            }

            return Task.FromResult(closeResult);
        }



        #region "Dispose"
        private bool isClosed;
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        if (!this.isClosed)
                        {
                            this.isClosed = true;
                            this.fileStream?.Flush();
                            this.fileStream?.Close();
                        }
                    }
                    finally
                    {
                        this.fileStream?.Dispose();
                    }
                }
                disposedValue = true;
            }
        }

        // // TODO: Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
        // ~FetchServerFileAsync()
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

        protected async virtual ValueTask DisposeAsyncCore()
        {
            try
            {
                if (!this.isClosed)
                {
                    this.isClosed = true;
                    await this.fileStream?.FlushAsync();
                    this.fileStream?.Close();
                }
            }
            finally
            {
                this.fileStream?.Dispose();
            }
        }
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();

            Dispose(false);
            GC.SuppressFinalize(this);
        }
        #endregion
    }


    internal class WriteFileAsyncInternal : IWriteFileAsync
    {
        private OpenAsyncParams param;
        private readonly ServerProvider Provider;
        private FileStream fileStream;
        private string fullPath;
        private string tempFile;
        private bool useTempFile;

        public WriteFileAsyncInternal(ServerProvider provider)
        {
            this.Provider = provider;
            this.useTempFile = provider.Parameter.UseTempFilesForUpload;
        }

        public UploadMode SupportedUploadModes
        {
            get
            {
                // Resume currently not implemented (Verification of file integrity not implemented)
                return UploadMode.FullFile | UploadMode.PartialUpdate;
            }
        }
        public Task<WriteFileOpenResult> OpenAsync(OpenAsyncParams e)
        {
            this.param = e;
            WriteFileOpenResult openResult = new();

            // PartialUpdate is done In-Place without temp file.
            if (e.mode == UploadMode.PartialUpdate) { this.useTempFile = false; }

            try
            {
                this.fullPath = Path.Combine(this.Provider.Parameter.ServerPath, this.param.RelativeFileName);

                if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                }

                tempFile = Path.GetDirectoryName(this.fullPath) + @"\$_" + Path.GetFileName(fullPath);

                FileMode fileMode = this.param.mode switch
                {
                    UploadMode.FullFile => FileMode.Create,
                    UploadMode.Resume => FileMode.Open,
                    UploadMode.PartialUpdate => FileMode.Open,
                    _ => FileMode.OpenOrCreate,
                };

                // Resume currently not implemented (Verification of file integrity not implemented)

                if (this.useTempFile)
                {
                    this.fileStream = new FileStream(tempFile, fileMode, FileAccess.Write, FileShare.None);
                }
                else
                {
                    this.fileStream = new FileStream(fullPath, fileMode, FileAccess.Write, FileShare.None);
                }


                this.fileStream.SetLength(e.FileInfo.FileSize);
                if (File.Exists(fullPath))
                {
                    openResult.Placeholder = new(fullPath);
                }
            }
            catch (Exception ex)
            {
                openResult.SetException(ex);
            }

            return Task.FromResult(openResult);
        }

        public async Task<WriteFileWriteResult> WriteAsync(byte[] buffer, int offsetBuffer, long offset, int count)
        {
            WriteFileWriteResult writeResult = new();

            try
            {
                fileStream.Position = offset;
                await fileStream.WriteAsync(buffer, offsetBuffer, count, this.param.CancellationToken);
            }
            catch (Exception ex)
            {
                writeResult.SetException(ex);
            }

            return writeResult;
        }

        public async Task<WriteFileCloseResult> CloseAsync(bool isCompleted)
        {
            WriteFileCloseResult closeResult = new();

            try
            {
                await fileStream.FlushAsync();
                fileStream.Close();
                this.isClosed = true;

                var pFile = this.fullPath;
                if (this.useTempFile) pFile = this.tempFile;

                try
                {
                    if (this.param.FileInfo.FileAttributes > 0) { File.SetAttributes(pFile, this.param.FileInfo.FileAttributes); }
                    if (this.param.FileInfo.CreationTime > DateTime.MinValue) { File.SetCreationTime(pFile, this.param.FileInfo.CreationTime); }
                    //if (this.param.FileInfo.LastAccessTime > DateTime.MinValue) { File.SetLastAccessTime(pFile, this.param.FileInfo.LastAccessTime); }
                    //if (this._Params.FileInfo.LastWriteTime > DateTime.MinValue) { File.SetLastWriteTime(pFile, this._Params.FileInfo.LastWriteTime); }
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }

                if (isCompleted)
                {
                    if (this.useTempFile)
                    {
                        if (File.Exists(this.fullPath))
                        {
                            if (this.Provider.Parameter.UseRecycleBinForChangedFiles)
                            {
                                this.Provider.MoveToRecycleBin(this.param.RelativeFileName, false);
                            }
                            else
                            {
                                File.Delete(this.fullPath);
                            }
                        }

                        File.Move(this.tempFile, this.fullPath);
                    }

                    closeResult.Placeholder = new(this.fullPath);
                }
            }
            catch (Exception ex)
            {
                closeResult.SetException(ex);
            }

            return closeResult;
        }

        #region "Dispose"
        private bool isClosed;
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        if (!this.isClosed)
                        {
                            this.isClosed = true;

                            this.fileStream?.Flush();
                            this.fileStream?.Close();
                        }
                    }
                    finally
                    {
                        this.fileStream?.Dispose();
                    }
                }
                disposedValue = true;
            }
        }

        // // TODO: Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
        // ~FetchServerFileAsync()
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

        protected async virtual ValueTask DisposeAsyncCore()
        {
            try
            {
                if (!this.isClosed)
                {
                    this.isClosed = true;
                    await this.fileStream?.FlushAsync();
                    this.fileStream?.Close();
                }
            }
            finally
            {
                this.fileStream?.Dispose();
            }
        }
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();

            Dispose(false);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    internal class FileListAsyncInternal : IFileListAsync
    {
        private readonly ServerProvider Provider;
        private CancellationTokenSource ctx;
        private Placeholder current;
        private Task enumTask;
        private System.Collections.Concurrent.BlockingCollection<Placeholder> infoList;

        public FileListAsyncInternal(ServerProvider provider)
        {
            this.Provider = provider;
        }

        public Task OpenAsync(string RelativeFileName, CancellationToken cancellationToken)
        {
            this.ctx = new CancellationTokenSource();
            var fullPath = Path.Combine(this.Provider.Parameter.ServerPath, RelativeFileName);

            this.infoList = new System.Collections.Concurrent.BlockingCollection<Placeholder>();

            cancellationToken.Register(() => { this.ctx.Cancel(); });


            this.enumTask = Task.Run(() =>
            {
                var tctx = ctx.Token;
                var di = new DirectoryInfo(fullPath);

                try
                {
                    if (di.Exists)
                    {
                        foreach (var fi in di.EnumerateFileSystemInfos())
                        {
                            tctx.ThrowIfCancellationRequested();
                            if (!fi.Name.StartsWith(@"$"))
                            {
                                infoList.Add(new Placeholder(fi));
                            }
                        }
                    }
                }
                finally
                {
                    infoList.CompleteAdding();
                }
            }, ctx.Token);


            // Open completed.... Itterating is running in Background.
            return Task.CompletedTask;
        }
        public Task<bool> MoveNextAsync()
        {
            var t = this.ctx.Token;

            return Task.Run(() =>
            {
                return this.infoList.TryTake(out this.current, -1, t);
            }, t);
        }
        public Placeholder Current
        {
            get
            {
                return this.current;
            }
        }

        public Task CloseAsync()
        {
            this.ctx.Cancel();
            this.infoList.CompleteAdding();
            this.closed = true;

            return Task.CompletedTask;
        }

        #region "Dispose"
        private bool disposedValue;
        private bool closed;


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                }
                disposedValue = true;
            }
        }

        // // TODO: Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
        // ~FetchServerFileAsync()
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

        protected async virtual ValueTask DisposeAsyncCore()
        {
            if (!this.closed)
                await CloseAsync();

            this.infoList?.Dispose();
        }
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();

            Dispose(false);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
