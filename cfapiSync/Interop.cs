using System;
using System.Runtime.InteropServices;
using Windows.Win32.System.PropertiesSystem;

namespace Windows.Win32
{
    public static partial class Constants
    {
        public static readonly PROPERTYKEY PKEY_StorageProviderTransferProgress = new() { 
            fmtid = new Guid("e77e90df-6271-4f5b-834f-2dd1f245dda4"), 
            pid = 4 };

        public static readonly PROPERTYKEY PKEY_SyncTransferStatus = new()
        {
            fmtid = new Guid ("FCEFF153-E839-4CF3-A9E7-EA22832094B8"),
            pid = 103
        };

        public enum SYNC_TRANSFER_STATUS
        {
            STS_NONE = 0,
            STS_NEEDSUPLOAD = 0x1,
            STS_NEEDSDOWNLOAD = 0x2,
            STS_TRANSFERRING = 0x4,
            STS_PAUSED = 0x8,
            STS_HASERROR = 0x10,
            STS_FETCHING_METADATA = 0x20,
            STS_USER_REQUESTED_REFRESH = 0x40,
            STS_HASWARNING = 0x80,
            STS_EXCLUDED = 0x100,
            STS_INCOMPLETE = 0x200,
            STS_PLACEHOLDER_IFEMPTY = 0x400
        }
    }


}
