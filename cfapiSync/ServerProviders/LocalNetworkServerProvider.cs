using Styletronix.CloudSyncProvider;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Styletronix.CloudFilterApi;

public partial class LocalNetworkServerProvider : IServerFileProvider
{
    public LocalNetworkServerProvider(string ServerPath)
    {
        Parameter = new ServerProviderParams()
        {
            ServerPath = ServerPath,
            UseRecycleBin = true,
            UseRecycleBinForChangedFiles = true,
            UseTempFilesForUpload = false
        };

        preferredServerProviderSettings = new()
        {
            AllowPartialUpdate = true,
            MaxChunkSize = int.MaxValue,
            MinChunkSize = 4096,
            PreferFullDirSync = true
        };

        connectionTimer = new(ConnectionTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        fullResyncTimer = new(FullResyncTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
    }

    public event EventHandler<ServerProviderStateChangedEventArgs> ServerProviderStateChanged;
    public event EventHandler<FileChangedEventArgs> FileChanged;

    public SyncContext SyncContext { get; set; }
    public PreferredSettings PreferredServerProviderSettings => preferredServerProviderSettings;
    public ServerProviderStatus Status { get => _Status; }


    private readonly ServerProviderParams Parameter;
    private ServerProviderStatus _Status = ServerProviderStatus.Disconnected;
    private ServerCallback serverCallback;
    private readonly PreferredSettings preferredServerProviderSettings;
    private readonly Timer connectionTimer;
    private readonly Timer fullResyncTimer;

    public class ServerProviderParams
    {
        public string ServerPath;
        public bool UseRecycleBin;
        public bool UseRecycleBinForChangedFiles;
        public bool UseTempFilesForUpload;
    }

    public void ConnectionTimerCallback(object state)
    {
        CheckProviderStatus();
    }
    public void FullResyncTimerCallback(object state)
    {
        //  TODO: Sync on reconnect.
        RaiseFileChanged(new() { ChangeType = WatcherChangeTypes.All, ResyncSubDirectories = true });
    }
    public Task<GenericResult> Connect()
    {
        GenericResult genericResult = new();

        if (serverCallback == null)
            serverCallback = new(this);

        SetProviderStatus(ServerProviderStatus.Connecting);

        try
        {
            if (!Directory.Exists(Parameter.ServerPath)) { Directory.CreateDirectory(Parameter.ServerPath); };
        }
        catch (Exception) { }

        if (!CheckProviderStatus())
            genericResult.Status = NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE;

        return Task.FromResult(genericResult);
    }
    public Task<GenericResult> Disconnect()
    {
        GenericResult genericResult = new();

        SetProviderStatus(ServerProviderStatus.Disabled);

        return Task.FromResult(genericResult);
    }


    public IReadFileAsync GetNewReadFile() { return new ReadFileAsyncInternal(this); }
    public IWriteFileAsync GetNewWriteFile() { return new WriteFileAsyncInternal(this); }
    public IFileListAsync GetNewFileList() { return new FileListAsyncInternal(this); }


    public Task<DeleteFileResult> DeleteFileAsync(string RelativeFileName, bool isDirectory)
    {
        DeleteFileResult deleteFileResult = new();

        try
        {
            DeleteOrMoveToRecycleBin(RelativeFileName, isDirectory);
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
        string fullPath = Path.Combine(Parameter.ServerPath, RelativeFileName);
        string fullPathDestination = Path.Combine(Parameter.ServerPath, RelativeDestination);

        if (!Directory.Exists(Path.GetDirectoryName(fullPathDestination)))
            Directory.CreateDirectory(Path.GetDirectoryName(fullPathDestination));

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

        string fullPath = Path.Combine(Parameter.ServerPath, RelativeFileName);

        try
        {
            if (isDirectory)
            {
                if (!Directory.Exists(fullPath))
                {
                    return Task.FromResult(new GetFileInfoResult(NtStatus.STATUS_NOT_A_CLOUD_FILE));
                }
            }
            else
            {
                if (!File.Exists(fullPath))
                {
                    return Task.FromResult(new GetFileInfoResult(NtStatus.STATUS_NOT_A_CLOUD_FILE));
                }
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

        string fullPath = Path.Combine(Parameter.ServerPath, RelativeFileName);

        try
        {
            if (isDirectory)
            {
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
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


    internal void MoveToRecycleBin(string relativePath, bool isDirectory)
    {
        string recyclePath = Parameter.ServerPath + @"\$Recycle.bin\" + relativePath;
        string fullPath = Parameter.ServerPath + @"\" + relativePath;

        recyclePath = Path.GetDirectoryName(recyclePath) + @"\(" + DateTime.Now.ToString("s").Replace(":", "_") + ") " + Path.GetFileName(recyclePath);

        if (!Directory.Exists(Path.GetDirectoryName(recyclePath))) { Directory.CreateDirectory(Path.GetDirectoryName(recyclePath)); }

        if (isDirectory)
        {
            if (Directory.Exists(fullPath))
            {
                Directory.Move(fullPath, recyclePath);
            }
        }
        else
        {
            if (File.Exists(fullPath))
            {
                File.Move(fullPath, recyclePath);
            }
        }
    }
    internal void DeleteOrMoveToRecycleBin(string relativePath, bool isDirectory)
    {
        if (Parameter.UseRecycleBin)
        {
            MoveToRecycleBin(relativePath, isDirectory);
        }
        else
        {
            string fullPath = Path.Combine(Parameter.ServerPath, relativePath);
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

    internal bool CheckProviderStatus()
    {
        if (Status == ServerProviderStatus.Disabled)
            return false;

        try
        {
            // Emulate "Disconnected / Offline" if ServerPath not found
            bool isOnline = Directory.Exists(Parameter.ServerPath);

            SetProviderStatus(isOnline ? ServerProviderStatus.Connected : ServerProviderStatus.Disconnected);

            return isOnline;
        }
        catch (Exception ex)
        {
            Styletronix.Debug.LogException(ex);
            return false;
        }
    }
    internal void SetProviderStatus(ServerProviderStatus status)
    {
        if (_Status != status)
        {
            _Status = status;
            if (status == ServerProviderStatus.Connected)
            {
                // Full sync after reconnect, then every 2 hour
                fullResyncTimer.Change(TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(120));

                // Check existing connection every 60 Seconds
                connectionTimer.Change(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
                if (_Status == ServerProviderStatus.Connected)
                    if (serverCallback != null)
                        serverCallback.fileSystemWatcher.EnableRaisingEvents = true;
            }
            else
            {
                // Disable full resyncs if not connected
                fullResyncTimer.Change(Timeout.Infinite, Timeout.Infinite);

                if (serverCallback != null)
                    serverCallback.fileSystemWatcher.EnableRaisingEvents = false;
            }

            if (status == ServerProviderStatus.Disabled)
                connectionTimer.Change(Timeout.Infinite, Timeout.Infinite);

            RaiseServerProviderStateChanged(new ServerProviderStateChangedEventArgs(status));
        }
    }
    internal string GetRelativePath(string fullPath)
    {
        if (!fullPath.StartsWith(Parameter.ServerPath, StringComparison.CurrentCultureIgnoreCase))
        {
            throw new Exception("File not part of Sync Root");
        }

        if (fullPath.Length == Parameter.ServerPath.Length)
        {
            return "";
        }

        return fullPath.Remove(0, Parameter.ServerPath.Length + 1);
    }

    protected virtual void RaiseServerProviderStateChanged(ServerProviderStateChangedEventArgs e)
    {
        ServerProviderStateChanged?.Invoke(this, e);
    }
    protected virtual void RaiseFileChanged(FileChangedEventArgs e)
    {
        try
        {
            FileChanged?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            Styletronix.Debug.LogException(ex);
        }
    }


    internal class ReadFileAsyncInternal : IReadFileAsync
    {
        private readonly LocalNetworkServerProvider provider;
        private FileStream fileStream;
        private OpenAsyncParams openAsyncParams;
        public ReadFileAsyncInternal(LocalNetworkServerProvider provider)
        {
            this.provider = provider;
        }

        public Task<ReadFileOpenResult> OpenAsync(OpenAsyncParams e)
        {
            if (!provider.CheckProviderStatus())
            {
                return Task.FromResult(new ReadFileOpenResult(NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE));
            }

            openAsyncParams = e;
            ReadFileOpenResult openResult = new();

            string fullPath = Path.Combine(provider.Parameter.ServerPath, e.RelativeFileName);

            // Simulate "Offline" if Serverfolder not found.
            if (!Directory.Exists(provider.Parameter.ServerPath))
            {
                openResult.SetException(CloudExceptions.Offline);
                goto skip;
            }

            try
            {
                if (!File.Exists(fullPath)) { throw new FileNotFoundException(e.RelativeFileName); }

                fileStream = File.OpenRead(fullPath);
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
            if (!provider.CheckProviderStatus())
            {
                return new ReadFileReadResult(NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE);
            }

            ReadFileReadResult readResult = new();

            try
            {
                fileStream.Position = offset;
                readResult.BytesRead = await fileStream.ReadAsync(buffer, offsetBuffer, count, openAsyncParams.CancellationToken);
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
                isClosed = true;
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
                        if (!isClosed)
                        {
                            isClosed = true;
                            fileStream?.Flush();
                            fileStream?.Close();
                        }
                    }
                    finally
                    {
                        fileStream?.Dispose();
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

        protected virtual async ValueTask DisposeAsyncCore()
        {
            try
            {
                if (!isClosed)
                {
                    isClosed = true;
                    await fileStream?.FlushAsync();
                    fileStream?.Close();
                }
            }
            finally
            {
                fileStream?.Dispose();
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
        private readonly LocalNetworkServerProvider provider;
        private FileStream fileStream;
        private string fullPath;
        private string tempFile;

        public WriteFileAsyncInternal(LocalNetworkServerProvider provider)
        {
            this.provider = provider;
        }

        public UploadMode SupportedUploadModes =>
                // Resume currently not implemented (Verification of file integrity not implemented)
                UploadMode.FullFile | UploadMode.PartialUpdate;
        public Task<WriteFileOpenResult> OpenAsync(OpenAsyncParams e)
        {
            if (!provider.CheckProviderStatus())
            {
                return Task.FromResult(new WriteFileOpenResult(NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE));
            }

            param = e;

            WriteFileOpenResult openResult = new();

            // PartialUpdate is done In-Place without temp file.
            if (e.mode == UploadMode.PartialUpdate) { provider.Parameter.UseTempFilesForUpload = false; }

            try
            {
                fullPath = Path.Combine(provider.Parameter.ServerPath, param.RelativeFileName);

                if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                }

                tempFile = Path.GetDirectoryName(fullPath) + @"\$_" + Path.GetFileName(fullPath);

                FileMode fileMode = param.mode switch
                {
                    UploadMode.FullFile => FileMode.Create,
                    UploadMode.Resume => FileMode.Open,
                    UploadMode.PartialUpdate => FileMode.Open,
                    _ => FileMode.OpenOrCreate,
                };

                // Resume currently not implemented (Verification of file integrity not implemented)

                if (provider.Parameter.UseTempFilesForUpload)
                {
                    fileStream = new FileStream(tempFile, fileMode, FileAccess.Write, FileShare.None);
                }
                else
                {
                    fileStream = new FileStream(fullPath, fileMode, FileAccess.Write, FileShare.None);
                }


                fileStream.SetLength(e.FileInfo.FileSize);
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
                await fileStream.WriteAsync(buffer, offsetBuffer, count, param.CancellationToken);
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
                isClosed = true;
                fileStream.Dispose();

                string pFile = fullPath;
                if (provider.Parameter.UseTempFilesForUpload)
                {
                    pFile = tempFile;
                }

                try
                {
                    var att = param.FileInfo.FileAttributes;
                        att &= ~FileAttributes.ReadOnly;

                    if (param.FileInfo.FileAttributes > 0) { File.SetAttributes(pFile, att); }
                    if (param.FileInfo.CreationTime > DateTime.MinValue) { File.SetCreationTime(pFile, param.FileInfo.CreationTime); }
                    if (this.param.FileInfo.LastAccessTime > DateTime.MinValue) { File.SetLastAccessTime(pFile, this.param.FileInfo.LastAccessTime); }
                    if (param.FileInfo.LastWriteTime > DateTime.MinValue) { File.SetLastWriteTime(pFile, param.FileInfo.LastWriteTime); }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }

                if (isCompleted)
                {
                    if (provider.Parameter.UseTempFilesForUpload)
                    {
                        if (File.Exists(fullPath))
                        {
                            if (provider.Parameter.UseRecycleBinForChangedFiles)
                            {
                                provider.MoveToRecycleBin(param.RelativeFileName, false);
                            }
                            else
                            {
                                File.Delete(fullPath);
                            }
                        }

                        File.Move(tempFile, fullPath);
                    }

                    closeResult.Placeholder = new(fullPath);
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
                        if (!isClosed)
                        {
                            isClosed = true;

                            fileStream?.Flush();
                            fileStream?.Close();
                        }
                    }
                    finally
                    {
                        fileStream?.Dispose();
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

        protected virtual async ValueTask DisposeAsyncCore()
        {
            try
            {
                if (!isClosed)
                {
                    isClosed = true;
                    if (fileStream != null)
                    {
                        await fileStream.FlushAsync();
                        fileStream.Close();
                    }
                }
            }
            finally
            {
                fileStream?.Dispose();
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
        private readonly LocalNetworkServerProvider provider;
        private readonly CancellationTokenSource ctx = new();
        private readonly System.Collections.Concurrent.BlockingCollection<Placeholder> infoList = new();
        private readonly GenericResult finalStatus = new();

        public FileListAsyncInternal(LocalNetworkServerProvider provider)
        {
            this.provider = provider;
        }

        public Task<GenericResult> OpenAsync(string relativeFileName, CancellationToken cancellationToken)
        {
            if (!provider.CheckProviderStatus())
            {
                return Task.FromResult(new GenericResult(NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE));
            }

            string fullPath = Path.Combine(provider.Parameter.ServerPath, relativeFileName);

            cancellationToken.Register(() => { ctx.Cancel(); });
            CancellationToken tctx = ctx.Token;

            DirectoryInfo directory = new(fullPath);

            if (!directory.Exists)
            {
                return Task.FromResult(new GenericResult(NtStatus.STATUS_NOT_A_CLOUD_FILE));
            }

            Task.Run(() =>
            {
                try
                {
                    foreach (FileSystemInfo fileSystemInfo in directory.EnumerateFileSystemInfos())
                    {
                        tctx.ThrowIfCancellationRequested();

                        if (!fileSystemInfo.Name.StartsWith(@"$"))
                        {
                            infoList.Add(new Placeholder(fileSystemInfo));
                        }
                    }
                }
                catch (Exception ex)
                {
                    finalStatus.SetException(ex);
                }
                finally
                {
                    infoList.CompleteAdding();
                }
            }, ctx.Token);


            // Open completed.... Itterating is running in Background.
            return Task.FromResult(new GenericResult());
        }
        public Task<GetNextResult> GetNextAsync()
        {
            return Task.Run(GetNextResult () =>
            {
                GetNextResult getNextResult = new();

                try
                {
                    if (!provider.CheckProviderStatus())
                    {
                        return new GetNextResult(NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE);
                    }

                    if (infoList.TryTake(out Placeholder item, -1, ctx.Token))
                    {
                        // STATUS_SUCCESS = Data found
                        getNextResult.Status = Styletronix.CloudFilterApi.NtStatus.STATUS_SUCCESS;
                        getNextResult.Placeholder = item;
                    }
                    else
                    {
                        // STATUS_UNSUCCESSFUL = No more Data available.
                        getNextResult.Status = Styletronix.CloudFilterApi.NtStatus.STATUS_UNSUCCESSFUL;
                    }
                }
                catch (Exception ex)
                {
                    getNextResult.SetException(ex);
                    finalStatus.SetException(ex);
                }

                return getNextResult;
            });
        }
        public Task<GenericResult> CloseAsync()
        {
            ctx.Cancel();
            if (!infoList.IsAddingCompleted)
            {
                infoList.CompleteAdding();
                finalStatus.Status = Styletronix.CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_REQUEST_ABORTED;
            }

            closed = true;

            return Task.FromResult(finalStatus);
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
                    ctx?.Cancel();
                    infoList?.Dispose();
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

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (!closed)
            {
                await CloseAsync();
            }

            infoList?.Dispose();
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
