using Styletronix.CloudSyncProvider;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Vanara.Extensions;
using Vanara.PInvoke;

public class ServerProvider : iServerFileProvider
{
    #region "iHandleServerFiles"

    public SyncProviderUtils.SyncContext SyncContext { get; set; }

    public iReadFileAsync GetNewReadFile() => new ReadFileAsyncInternal(this);

    public iWriteFileAsync GetNewWriteFile() => new WriteFileAsyncInternal(this);

    public iFileListAsync GetNewFileList() => new FileListAsyncInternal(this);

    public Task<DeleteFileResult> DeleteFileAsync(string RelativeFileName, bool isDirectory)
    {
        var fullPath = System.IO.Path.Combine(this.Parameter.ServerPath, RelativeFileName);
        var ret = new DeleteFileResult();

        try
        {
            if (isDirectory)
            {
                System.IO.Directory.Delete(fullPath, false);
            }
            else
            {
                System.IO.File.Delete(fullPath);
            }

            ret.Succeeded = true;
        }
        catch (System.IO.DirectoryNotFoundException ex2)
        {
            ret.Succeeded = true;
        }
        catch (Exception ex)
        {
            ret.Succeeded = false;
            ret.Exception = ex;
            ret.Message = ex.Message;
        }

        return Task.FromResult(ret);
    }

    public Task<MoveFileResult> MoveFileAsync(string RelativeFileName, string RelativeDestination, bool isDirectory)
    {
        var fullPath = Path.Combine(this.Parameter.ServerPath, RelativeFileName);
        var fullPathDestination = Path.Combine(this.Parameter.ServerPath, RelativeDestination);

        var ret = new MoveFileResult();

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

            ret.Succeeded = true;
        }
        catch (Exception ex)
        {
            ret.Succeeded = false;
            ret.Exception = ex;
            ret.Message = ex.Message;
        }

        return Task.FromResult(ret);
    }

    public Task<Placeholder> GetFileInfo(string RelativeFileName, bool isDirectory)
    {
        var fullPath = Path.Combine(this.Parameter.ServerPath, RelativeFileName);
        return Task.FromResult(new Placeholder(fullPath));
    }
    #endregion



    #region "Server Parameter"

    public class ServerProviderParams
    {
        public string ServerPath;
    }
    internal ServerProviderParams Parameter;

    public ServerProvider(string ServerPath)
    {
        this.Parameter = new ServerProviderParams()
        {
            ServerPath = ServerPath
        };

        if (System.IO.Directory.Exists(this.Parameter.ServerPath) == false) { System.IO.Directory.CreateDirectory(this.Parameter.ServerPath); };
    }

    #endregion


    internal class ReadFileAsyncInternal : iReadFileAsync
    {
        private ServerProvider Provider;
        private FileStream fileStream;
        private OpenAsyncParams _Params;
        public ReadFileAsyncInternal(ServerProvider provider)
        {
            this.Provider = provider;
        }

        public Task<OpenAsyncResult> OpenAsync(OpenAsyncParams e)
        {
            this._Params = e;

            var fullPath = Path.Combine(this.Provider.Parameter.ServerPath, e.RelativeFileName);
            this.fileStream = File.OpenRead(fullPath);

            return Task.FromResult(new OpenAsyncResult()
            {
                ETag = "_" + File.GetLastWriteTimeUtc(fullPath).Ticks + "_"
            });
        }

        public Task<int> ReadAsync(byte[] buffer, int offsetBuffer, long offset, int count)
        {
            fileStream.Position = offset;
            return fileStream.ReadAsync(buffer, offsetBuffer, count, this._Params.CancellationToken);
        }

        public Task CloseAsync()
        {
            fileStream.Close();
            this.isClosed = true;

            return Task.CompletedTask;
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
                            this.fileStream.Close();
                        }
                    }
                    finally
                    {
                        this.fileStream.Dispose();
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
        #endregion
    }


    internal class WriteFileAsyncInternal : iWriteFileAsync
    {
        private bool useTempFile = true;
        private OpenAsyncParams _Params;
        private ServerProvider Provider;
        private FileStream fileStream;
        private string fullPath;
        private string tempFile;


        public WriteFileAsyncInternal(ServerProvider provider)
        {
            this.Provider = provider;
        }

        public UploadMode supportedUploadModes
        {
            get
            {
                return UploadMode.fullFile;
            }
        }
        public Task<OpenAsyncResult> OpenAsync(OpenAsyncParams e)
        {
            this._Params = e;
            string ETag = null;

            if (this._Params.mode != UploadMode.fullFile) { throw new NotSupportedException(); }

            this.fullPath = Path.Combine(this.Provider.Parameter.ServerPath, this._Params.RelativeFileName);

            if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            }

            tempFile = Path.GetDirectoryName(this.fullPath) + @"\$_" + Path.GetFileName(fullPath);

            var fileMode = FileMode.OpenOrCreate;
            if (this._Params.mode == UploadMode.resume)
            {
                fileMode = FileMode.Open;
            }

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
                ETag = "_" + File.GetLastWriteTimeUtc(fullPath).Ticks + "_";
            }

            return Task.FromResult(new OpenAsyncResult()
            {
                ETag = ETag
            });
        }

        public Task WriteAsync(byte[] buffer, int offsetBuffer, long offset, int count)
        {
            fileStream.Position = offset;
            return fileStream.WriteAsync(buffer, offsetBuffer, count, this._Params.CancellationToken);
        }

        public async Task<Placeholder> CloseAsync(bool isCompleted)
        {
            await fileStream.FlushAsync();
            fileStream.Close();
            this.isClosed = true;

            var pFile = this.fullPath;
            if (this.useTempFile) pFile = this.tempFile;

            try
            {
                if (this._Params.FileInfo.FileAttributes > 0) { File.SetAttributes(pFile, this._Params.FileInfo.FileAttributes); }
                if (this._Params.FileInfo.CreationTime > DateTime.MinValue) { File.SetCreationTime(pFile, this._Params.FileInfo.CreationTime); }
                if (this._Params.FileInfo.LastAccessTime > DateTime.MinValue) { File.SetLastAccessTime(pFile, this._Params.FileInfo.LastAccessTime); }
                if (this._Params.FileInfo.LastWriteTime > DateTime.MinValue) { File.SetLastWriteTime(pFile, this._Params.FileInfo.LastWriteTime); }
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            if (isCompleted)
            {
                if (this.useTempFile)
                {
                    if (File.Exists(this.fullPath)) { File.Delete(this.fullPath); }
                    File.Move(this.tempFile, this.fullPath);
                }

                return new Placeholder(this.fullPath);
            }

            return null;
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
                    if (this.fileStream != null)
                    {
                        try
                        {
                            if (!this.isClosed)
                            {
                                this.isClosed = true;

                                this.fileStream.Flush();
                                this.fileStream.Close();
                            }
                        }
                        finally
                        {
                            this.fileStream.Dispose();
                        }
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


        #endregion
    }

    internal class FileListAsyncInternal : iFileListAsync
    {
        private ServerProvider Provider;
        private CancellationTokenSource ctx;
        private Task enumTask;
        private Placeholder _current;
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
                return this.infoList.TryTake(out this._current, -1, t);
            }, t);
        }
        public Placeholder Current
        {
            get
            {
                return this._current;
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
                    if (!this.closed)
                        CloseAsync().GetAwaiter().GetResult();

                    this.infoList.Dispose();
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


        #endregion
    }
}
