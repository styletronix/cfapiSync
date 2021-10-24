using Styletronix.CloudSyncProvider;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Styletronix.CloudFilterApi;

public class ServerProvider : IServerFileProvider
{
    public ServerProvider(string ServerPath)
    {
        Parameter = new ServerProviderParams()
        {
            ServerPath = ServerPath,
            UseRecycleBin = true,
            UseRecycleBinForChangedFiles = true,
            UseTempFilesForUpload = true
        };
    }

    public event EventHandler<ServerProviderStateChangedEventArgs> ServerProviderStateChanged;
    public event EventHandler<FileChangedEventArgs> FileChanged;

    public SyncProviderUtils.SyncContext SyncContext { get; set; }
    private readonly ServerProviderParams Parameter;
    private ServerProviderStatus lastStatus = ServerProviderStatus.Disconnected;
    private ServerCallback serverCallback;

    public class ServerProviderParams
    {
        public string ServerPath;
        public bool UseRecycleBin;
        public bool UseRecycleBinForChangedFiles;
        public bool UseTempFilesForUpload;
    }

    public Task<GenericResult> Connect()
    {
        var genericResult = new GenericResult();

        try
        {
            if (!Directory.Exists(Parameter.ServerPath)) { Directory.CreateDirectory(Parameter.ServerPath); };
        }
        catch (Exception) { }

        if (!this.CheckProviderStatus())
            genericResult.Status = NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE;

        serverCallback = new ServerCallback(this);

        return Task.FromResult(genericResult);
    }

    public Task<GenericResult> Disconnect()
    {
        var genericResult = new GenericResult();

        serverCallback?.Dispose();

        return Task.FromResult(genericResult);
    }



    public IReadFileAsync GetNewReadFile() { return new ReadFileAsyncInternal(this); }
    public IWriteFileAsync GetNewWriteFile() { return new WriteFileAsyncInternal(this); }
    public IFileListAsync GetNewFileList() { return new FileListAsyncInternal(this); }


    public Task<DeleteFileResult> DeleteFileAsync(string RelativeFileName, bool isDirectory)
    {
        var deleteFileResult = new DeleteFileResult();

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

        var moveFileResult = new MoveFileResult();

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
        var getFileInfoResult = new GetFileInfoResult();

        string fullPath = Path.Combine(Parameter.ServerPath, RelativeFileName);

        try
        {
            if (isDirectory)
            {
                if (!Directory.Exists(fullPath))
                    return Task.FromResult(new GetFileInfoResult(NtStatus.STATUS_NOT_A_CLOUD_FILE));
            }
            else
            {
                if (!File.Exists(fullPath))
                    return Task.FromResult(new GetFileInfoResult(NtStatus.STATUS_NOT_A_CLOUD_FILE));
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
        var createFileResult = new CreateFileResult();

        string fullPath = Path.Combine(Parameter.ServerPath, RelativeFileName);

        try
        {
            if (isDirectory)
            {
                if(!Directory.Exists(fullPath))
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


    internal void MoveToRecycleBin(string relativePath, bool isDirectory)
    {
        string recyclePath = Parameter.ServerPath + @"\$Recycle.bin\" + relativePath;
        string fullPath = Parameter.ServerPath + @"\" + relativePath;

        recyclePath = Path.GetDirectoryName(recyclePath) + @"\(" + DateTime.Now.ToString("s").Replace(":", "_") + ") " + Path.GetFileName(recyclePath);

        if (!Directory.Exists(Path.GetDirectoryName(recyclePath))) { Directory.CreateDirectory(Path.GetDirectoryName(recyclePath)); }

        if (isDirectory)
        {
            if (Directory.Exists(fullPath))
                Directory.Move(fullPath, recyclePath);
        }
        else
        {
            if (File.Exists(fullPath))
                File.Move(fullPath, recyclePath);
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
        // Emulate "Disconnected / Offline" if ServerPath not found
        var isOnline = Directory.Exists(this.Parameter.ServerPath);

        this.SetProviderStatus(isOnline ? ServerProviderStatus.Connected : ServerProviderStatus.Disconnected);

        return isOnline;
    }
    internal void SetProviderStatus(ServerProviderStatus status)
    {
        if (this.lastStatus != status)
        {
            this.RaiseServerProviderStateChanged(new ServerProviderStateChangedEventArgs(status));
            this.lastStatus = status;
        }
    }
    internal string GetRelativePath(string fullPath)
    {
        if (!fullPath.StartsWith(this.Parameter.ServerPath, StringComparison.CurrentCultureIgnoreCase)) throw new Exception("File not part of Sync Root");
        if (fullPath.Length == this.Parameter.ServerPath.Length) return "";

        return fullPath.Remove(0, this.Parameter.ServerPath.Length + 1);
    }

    protected virtual void RaiseServerProviderStateChanged(ServerProviderStateChangedEventArgs e)
    {
        this.ServerProviderStateChanged?.Invoke(this, e);
    }
    protected virtual void RaiseFileChanged(FileChangedEventArgs e)
    {
        this.FileChanged?.Invoke(this, e);
    }


    internal class ReadFileAsyncInternal : IReadFileAsync
    {
        private readonly ServerProvider provider;
        private FileStream fileStream;
        private OpenAsyncParams openAsyncParams;
        public ReadFileAsyncInternal(ServerProvider provider)
        {
            this.provider = provider;
        }

        public Task<ReadFileOpenResult> OpenAsync(OpenAsyncParams e)
        {
            if (!this.provider.CheckProviderStatus())
                return Task.FromResult(new ReadFileOpenResult(NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE));

            openAsyncParams = e;
            var openResult = new ReadFileOpenResult();

            string fullPath = Path.Combine(this.provider.Parameter.ServerPath, e.RelativeFileName);

            // Simulate "Offline" if Serverfolder not found.
            if (!Directory.Exists(this.provider.Parameter.ServerPath))
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
            if (!this.provider.CheckProviderStatus())
                return new ReadFileReadResult(NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE);

            var readResult = new ReadFileReadResult();

            try
            {
                fileStream.Position = offset;
                readResult.BytesRead = await fileStream.ReadAsync(buffer, offsetBuffer, count, this.openAsyncParams.CancellationToken);
            }
            catch (Exception ex)
            {
                readResult.SetException(ex);
            }

            return readResult;
        }

        public Task<ReadFileCloseResult> CloseAsync()
        {
            var closeResult = new ReadFileCloseResult();

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
        private readonly ServerProvider provider;
        private FileStream fileStream;
        private string fullPath;
        private string tempFile;

        public WriteFileAsyncInternal(ServerProvider provider)
        {
            this.provider = provider;
        }

        public UploadMode SupportedUploadModes =>
                // Resume currently not implemented (Verification of file integrity not implemented)
                UploadMode.FullFile | UploadMode.PartialUpdate;
        public Task<WriteFileOpenResult> OpenAsync(OpenAsyncParams e)
        {
            if (!this.provider.CheckProviderStatus())
                return Task.FromResult(new WriteFileOpenResult(NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE));

            param = e;
            var openResult = new WriteFileOpenResult();

            // PartialUpdate is done In-Place without temp file.
            if (e.mode == UploadMode.PartialUpdate) { this.provider.Parameter.UseTempFilesForUpload = false; }

            try
            {
                fullPath = Path.Combine(this.provider.Parameter.ServerPath, param.RelativeFileName);

                if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                }

                tempFile = Path.GetDirectoryName(fullPath) + @"\$_" + Path.GetFileName(fullPath);

                var fileMode = param.mode switch
                {
                    UploadMode.FullFile => FileMode.Create,
                    UploadMode.Resume => FileMode.Open,
                    UploadMode.PartialUpdate => FileMode.Open,
                    _ => FileMode.OpenOrCreate,
                };

                // Resume currently not implemented (Verification of file integrity not implemented)

                if (this.provider.Parameter.UseTempFilesForUpload)
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
            var writeResult = new WriteFileWriteResult();

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
            var closeResult = new WriteFileCloseResult();

            try
            {
                await fileStream.FlushAsync();
                fileStream.Close();
                isClosed = true;

                string pFile = fullPath;
                if (this.provider.Parameter.UseTempFilesForUpload)
                    pFile = tempFile;

                try
                {
                    if (param.FileInfo.FileAttributes > 0) { File.SetAttributes(pFile, param.FileInfo.FileAttributes); }
                    if (param.FileInfo.CreationTime > DateTime.MinValue) { File.SetCreationTime(pFile, param.FileInfo.CreationTime); }
                    //if (this.param.FileInfo.LastAccessTime > DateTime.MinValue) { File.SetLastAccessTime(pFile, this.param.FileInfo.LastAccessTime); }
                    //if (this._Params.FileInfo.LastWriteTime > DateTime.MinValue) { File.SetLastWriteTime(pFile, this._Params.FileInfo.LastWriteTime); }
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }

                if (isCompleted)
                {
                    if (this.provider.Parameter.UseTempFilesForUpload)
                    {
                        if (File.Exists(fullPath))
                        {
                            if (this.provider.Parameter.UseRecycleBinForChangedFiles)
                            {
                                this.provider.MoveToRecycleBin(param.RelativeFileName, false);
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
    internal class FileListAsyncInternal : IFileListAsync
    {
        private readonly ServerProvider provider;
        private readonly CancellationTokenSource ctx = new();
        private readonly System.Collections.Concurrent.BlockingCollection<Placeholder> infoList = new();
        private readonly GenericResult finalStatus = new();

        public FileListAsyncInternal(ServerProvider provider)
        {
            this.provider = provider;
        }

        public Task<GenericResult> OpenAsync(string relativeFileName, CancellationToken cancellationToken)
        {
            if (!provider.CheckProviderStatus())
                return Task.FromResult(new GenericResult(NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE));

            var fullPath = Path.Combine(provider.Parameter.ServerPath, relativeFileName);

            cancellationToken.Register(() => { ctx.Cancel(); });
            var tctx = ctx.Token;

            var directory = new DirectoryInfo(fullPath);

            if (!directory.Exists)
                return Task.FromResult(new GenericResult(NtStatus.STATUS_NOT_A_CLOUD_FILE));

            Task.Run(() =>
            {
                try
                {
                    foreach (var fileSystemInfo in directory.EnumerateFileSystemInfos())
                    {
                        tctx.ThrowIfCancellationRequested();

                        if (!fileSystemInfo.Name.StartsWith(@"$"))
                            infoList.Add(new Placeholder(fileSystemInfo));
                    }
                }
                catch (Exception ex)
                {
                    this.finalStatus.SetException(ex);
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
                var getNextResult = new GetNextResult();

                try
                {
                    if (!provider.CheckProviderStatus())
                        return new GetNextResult(NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE);

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
                    this.finalStatus.SetException(ex);
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
                this.finalStatus.Status = Styletronix.CloudFilterApi.NtStatus.STATUS_CLOUD_FILE_REQUEST_ABORTED;
            }

            closed = true;

            return Task.FromResult(this.finalStatus);
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
                    this.ctx?.Cancel();
                    this.infoList?.Dispose();
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
    internal class ServerCallback : IDisposable
    {
        internal FileSystemWatcher fileSystemWatcher;
        internal ServerProvider serverProvider;
        internal bool disposedValue;
        internal readonly System.Threading.Tasks.Dataflow.ActionBlock<FileChangedEventArgs> fileChangedActionBlock;

        public ServerCallback(ServerProvider serverProvider)
        {
            this.serverProvider = serverProvider;

            this.fileChangedActionBlock = new(data => serverProvider.RaiseFileChanged(data));

            fileSystemWatcher = new FileSystemWatcher
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.Attributes,
                Filter = "*",
                IncludeSubdirectories = true,
                Path = serverProvider.Parameter.ServerPath
            };
            fileSystemWatcher.Created += FileSystemWatcher_Created;
            fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;
            fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;
            fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            fileSystemWatcher.Error += FileSystemWatcher_Error;
            fileSystemWatcher.EnableRaisingEvents = true;
        }


        private void FileSystemWatcher_Error(object sender, ErrorEventArgs e)
        {
            try
            {
                fileChangedActionBlock.Post(new FileChangedEventArgs()
                {
                    ChangeType = WatcherChangeTypes.All,
                    ResyncSubDirectories = true,
                    Placeholder = new(serverProvider.Parameter.ServerPath, serverProvider.GetRelativePath(serverProvider.Parameter.ServerPath))
                });
            }
            catch (Exception ex)
            {
                Styletronix.Debug.WriteLine(ex.Message, System.Diagnostics.TraceLevel.Error);
            }
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.Contains(@"$Recycle.bin")) return;

            try
            {
                fileChangedActionBlock.Post(new FileChangedEventArgs()
                {
                    ChangeType = WatcherChangeTypes.Changed,
                    ResyncSubDirectories = false,
                    Placeholder = new(e.FullPath, serverProvider.GetRelativePath(e.FullPath))
                });
            }
            catch (Exception ex)
            {
                Styletronix.Debug.WriteLine(ex.Message, System.Diagnostics.TraceLevel.Error);
            }
        }

        private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            if (e.FullPath.Contains(@"$Recycle.bin") && e.OldFullPath.Contains(@"$Recycle.bin")) return;

            try
            {
                if (e.FullPath.Contains(@"$Recycle.bin"))
                {
                    fileChangedActionBlock.Post(new FileChangedEventArgs()
                    {
                        ChangeType = WatcherChangeTypes.Deleted,
                        Placeholder = new(serverProvider.GetRelativePath(e.OldFullPath), false)
                    });
                }
                else if (e.OldFullPath.Contains(@"$Recycle.bin"))
                {
                    fileChangedActionBlock.Post(new FileChangedEventArgs()
                    {
                        ChangeType = WatcherChangeTypes.Created,
                        Placeholder = new(serverProvider.GetRelativePath(e.FullPath), false)
                    });
                }
                else
                {
                    fileChangedActionBlock.Post(new FileChangedEventArgs()
                    {
                        ChangeType = WatcherChangeTypes.Renamed,
                        Placeholder = new(e.FullPath, serverProvider.GetRelativePath(e.FullPath)),
                        OldRelativePath = serverProvider.GetRelativePath(e.OldFullPath)
                    });
                }
            }
            catch (Exception ex)
            {
                Styletronix.Debug.WriteLine(ex.Message, System.Diagnostics.TraceLevel.Error);
            }
        }

        private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.Contains(@"$Recycle.bin")) return;

            try
            {
                fileChangedActionBlock.Post(new FileChangedEventArgs()
                {
                    ChangeType = e.ChangeType,
                    ResyncSubDirectories = false,
                    Placeholder = new(serverProvider.GetRelativePath(e.FullPath), false)
                });
            }
            catch (Exception ex)
            {
                Styletronix.Debug.WriteLine(ex.Message, System.Diagnostics.TraceLevel.Error);
            }

        }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.Contains(@"$Recycle.bin")) return;

            try
            {
                fileChangedActionBlock.Post(new FileChangedEventArgs()
                {
                    ChangeType = e.ChangeType,
                    ResyncSubDirectories = false,
                    Placeholder = new(e.FullPath, serverProvider.GetRelativePath(e.FullPath))
                });
            }
            catch (Exception ex)
            {
                Styletronix.Debug.WriteLine(ex.Message, System.Diagnostics.TraceLevel.Error);
            }
        }



        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.fileSystemWatcher != null)
                    {
                        this.fileSystemWatcher.EnableRaisingEvents = false;
                        this.fileSystemWatcher.Dispose();
                    }
                    this.fileChangedActionBlock?.Complete();
                }

                disposedValue = true;
            }
        }

        // // TODO: Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
        // ~ServerCallback()
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
