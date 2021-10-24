using Styletronix.CloudSyncProvider;
using System;
using System.IO;

public partial class ServerProvider
{
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
                NotifyFilter = NotifyFilters.LastWrite |
                NotifyFilters.DirectoryName |
                NotifyFilters.FileName |
                NotifyFilters.Size |
                NotifyFilters.Attributes,

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
            var x = e.GetException();
            if (x.HResult == -2147467259)
            {
                System.Threading.Tasks.Task.Delay(5000).ContinueWith((t) =>
                {
                    fileSystemWatcher.EnableRaisingEvents = false;
                    fileSystemWatcher.EnableRaisingEvents = true;

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
                });
            }

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

        public void Dispose()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
