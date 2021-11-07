using Styletronix.CloudSyncProvider;
using System;
using System.IO;

public partial class LocalNetworkServerProvider
{
    internal class ServerCallback : IDisposable
    {
        internal readonly FileSystemWatcher fileSystemWatcher;
        private readonly LocalNetworkServerProvider serverProvider;
        private bool disposedValue;
        public readonly System.Threading.Tasks.Dataflow.ActionBlock<FileChangedEventArgs> fileChangedActionBlock;

        public ServerCallback(LocalNetworkServerProvider serverProvider)
        {
            this.serverProvider = serverProvider;

            fileChangedActionBlock = new(data => serverProvider.RaiseFileChanged(data));

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
            fileSystemWatcher.EnableRaisingEvents = false;

            // Set ServerProviderState to FAILED.
            // Connection Monitoring will trigger a FullSync if connection could be reestablished.
            serverProvider.SetProviderStatus(ServerProviderStatus.Failed);
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.Contains(@"$Recycle.bin"))
            {
                return;
            }

            if (Path.GetFileName(e.FullPath).StartsWith("$_"))
            {
                return;
            }

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
            if (e.FullPath.Contains(@"$Recycle.bin") && e.OldFullPath.Contains(@"$Recycle.bin"))
            {
                return;
            }

            if (e.Name.StartsWith("$_"))
            {
                return;
            }

            if (e.OldName.StartsWith("$_"))
            {
                FileSystemWatcher_Changed(sender, new FileSystemEventArgs(WatcherChangeTypes.Changed, Path.GetDirectoryName(e.FullPath), e.Name));
                return;
            }

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
            if (e.FullPath.Contains(@"$Recycle.bin"))
            {
                return;
            }

            if (Path.GetFileName(e.FullPath).StartsWith("$_"))
            {
                return;
            }

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
            if (e.FullPath.Contains(@"$Recycle.bin"))
            {
                return;
            }

            if (Path.GetFileName(e.FullPath).StartsWith("$_"))
            {
                return;
            }

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
                    if (fileSystemWatcher != null)
                    {
                        fileSystemWatcher.EnableRaisingEvents = false;
                        fileSystemWatcher.Dispose();
                    }
                    fileChangedActionBlock?.Complete();
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
