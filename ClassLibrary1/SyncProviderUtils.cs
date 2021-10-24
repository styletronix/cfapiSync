using Styletronix.CloudSyncProvider;
using System;
using System.Threading;
using static Vanara.PInvoke.CldApi;

public class SyncProviderUtils
{
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
        //public CldApi.CF_CALLBACK_INFO CallbackInfo;
        //public CldApi.CF_CALLBACK_PARAMETERS CallbackParameters;
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
    }

}


