using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Storage.CloudFilters;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;
using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.PInvoke;
using System.Diagnostics;

namespace Styletronix
{
    public class CloudFilterApi
    {
        //public class SafeOpenFileWithOplock : IDisposable
        //{
        //    private bool disposedValue;
        //    private Microsoft.Win32.SafeHandles.SafeFileHandle _FileHandle = default;
        //    private CF_PLACEHOLDER_STANDARD_INFO? _PlaceHolder;
        //    private CF_PLACEHOLDER_BASIC_INFO? _PlaceHolderBasic;
        //    private Windows.Win32.Foundation.HRESULT _result;

        //    public SafeOpenFileWithOplock(string FilePath, CF_OPEN_FILE_FLAGS Flags)
        //    {
        //        this._result = PInvoke.CfOpenFileWithOplock(FilePath, Flags, out this._FileHandle);
        //        _result.ThrowOnFailure();
        //    }

        //    public Windows.Win32.Foundation.HRESULT GetHRESULT()
        //    {
        //        return (Windows.Win32.Foundation.HRESULT)this;
        //    }
        //    public CF_PLACEHOLDER_STANDARD_INFO GetPlaceholderStandardInfo()
        //    {
        //        return (CF_PLACEHOLDER_STANDARD_INFO)this;
        //    }
        //    public CF_PLACEHOLDER_BASIC_INFO GetPlaceholderBasicInfo()
        //    {
        //        return (CF_PLACEHOLDER_BASIC_INFO)this;
        //    }
        //    public Microsoft.Win32.SafeHandles.SafeFileHandle GetSafeFileHandle()
        //    {
        //        return (Microsoft.Win32.SafeHandles.SafeFileHandle)this;
        //    }

        //    public static implicit operator Windows.Win32.Foundation.HRESULT(SafeOpenFileWithOplock instance)
        //    {
        //        return instance._result;
        //    }
        //    public static implicit operator CF_PLACEHOLDER_STANDARD_INFO(SafeOpenFileWithOplock instance)
        //    {
        //        if (instance._PlaceHolder == null)
        //        {
        //            instance._PlaceHolder = GetPlaceholderInfo(instance._FileHandle);
        //        }
        //        return (CF_PLACEHOLDER_STANDARD_INFO)instance._PlaceHolder;
        //    }
        //    public static implicit operator CF_PLACEHOLDER_BASIC_INFO(SafeOpenFileWithOplock instance)
        //    {
        //        if (instance._PlaceHolderBasic == null)
        //        {
        //            instance._PlaceHolderBasic = GetPlaceholderInfoBasic(instance._FileHandle);
        //        }
        //        return (CF_PLACEHOLDER_BASIC_INFO)instance._PlaceHolderBasic;
        //    }
        //    public static implicit operator Microsoft.Win32.SafeHandles.SafeFileHandle(SafeOpenFileWithOplock instance)
        //    {
        //        return instance._FileHandle;
        //    }

        //    protected virtual void Dispose(bool disposing)
        //    {
        //        if (!disposedValue)
        //        {
        //            if (disposing)
        //            {
        //                this._PlaceHolder = null;
        //            }

        //            // Calling this._FileHandle.Close() or this._FileHandle.Dispose() throws invalid handle exception.....
        //            PInvoke.CfCloseHandle(this._FileHandle);
        //            this._FileHandle.SetHandleAsInvalid();
        //            disposedValue = true;
        //        }
        //    }

        //    ~SafeOpenFileWithOplock()
        //    {
        //        // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
        //        Dispose(disposing: false);
        //    }

        //    public void Dispose()
        //    {
        //        // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
        //        Dispose(disposing: true);
        //        GC.SuppressFinalize(this);
        //    }
        //}
        public class SafeAllocCoTaskMem : IDisposable
        {
            private IntPtr _pointer;

            public SafeAllocCoTaskMem(int size)
            {
                this._pointer = Marshal.AllocCoTaskMem(size);
            }
            public static implicit operator IntPtr(SafeAllocCoTaskMem instance)
            {
                return instance._pointer;
            }

            #region "Dispose"

            private bool disposedValue;
            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {

                    }

                    Marshal.FreeCoTaskMem(this._pointer);
                    disposedValue = true;
                }
            }

            ~SafeAllocCoTaskMem()
            {
                // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
                Dispose(disposing: false);
            }

            public void Dispose()
            {
                // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }

            #endregion
        }
        public class SafeTransferKey : IDisposable
        {
            private CldApi.CF_TRANSFER_KEY TransferKey;
            private HFILE handle;

            public SafeTransferKey(HFILE handle)
            {
                this.handle = handle;

                CldApi.CfGetTransferKey(this.handle, out this.TransferKey).ThrowIfFailed();
            }
            public SafeTransferKey(Microsoft.Win32.SafeHandles.SafeFileHandle safeHandle)
            {
                this.handle = (HFILE)safeHandle;

                CldApi.CfGetTransferKey(this.handle, out this.TransferKey).ThrowIfFailed();
            }


            public static implicit operator CldApi.CF_TRANSFER_KEY(SafeTransferKey instance)
            {
                return instance.TransferKey;
            }


            private bool disposedValue;
            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {

                    }

                    if (!handle.IsInvalid)
                    {
                        CldApi.CfReleaseTransferKey(handle, TransferKey);
                    }

                    disposedValue = true;
                }
            }

            ~SafeTransferKey()
            {
                // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
                Dispose(disposing: false);
            }

            public void Dispose()
            {
                // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        public static CF_PLACEHOLDER_BASIC_INFO GetPlaceholderInfoBasic(string fullPath, bool isDirectory)
        {
            FILE_ACCESS_FLAGS accessFlag = isDirectory ? FILE_ACCESS_FLAGS.FILE_GENERIC_READ : FILE_ACCESS_FLAGS.FILE_READ_EA;

            using var h = PInvoke.CreateFileW(@"\\?\" + fullPath,
                              accessFlag,
                                FILE_SHARE_MODE.FILE_SHARE_READ |
                                FILE_SHARE_MODE.FILE_SHARE_WRITE |
                                FILE_SHARE_MODE.FILE_SHARE_DELETE, 
                                null,
                                FILE_CREATION_DISPOSITION.OPEN_EXISTING, FILE_FLAGS_AND_ATTRIBUTES.SECURITY_ANONYMOUS
                                null);

            if (h.IsInvalid)
            {
                Debug.WriteLine("SetInSyncState INVALID Handle! " + fullPath.TrimEnd('\\'));
                return default;
            }
            return GetPlaceholderInfoBasic(h);
        }

        public static CF_PLACEHOLDER_STANDARD_INFO GetPlaceholderInfoStandard(string fullPath)
        {
            using (var h = PInvoke.CreateFileW(@"\\?\" + fullPath,
                FILE_ACCESS_FLAGS.FILE_READ_EA |
                  FILE_ACCESS_FLAGS.FILE_READ_ATTRIBUTES,
                  FILE_SHARE_MODE.FILE_SHARE_READ |
                  FILE_SHARE_MODE.FILE_SHARE_WRITE |
                  FILE_SHARE_MODE.FILE_SHARE_DELETE,
                  null,
                  FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                  FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_EA,
                  null))
            {
                if (h.IsInvalid)
                {
                    Debug.WriteLine("SetInSyncState INVALID Handle! " + fullPath.TrimEnd('\\'));
                    return default;
                }
                return GetPlaceholderInfoStandard(h);
            }
        }
        public static CF_PLACEHOLDER_STANDARD_INFO GetPlaceholderInfoStandard(Microsoft.Win32.SafeHandles.SafeFileHandle FileHandle)
        {
            int InfoBufferLength = 1024;
            CF_PLACEHOLDER_STANDARD_INFO ResultInfo = default;

            using var bufferPointerHandler = new SafeAllocCoTaskMem(InfoBufferLength);
            using var returnedLengtPointerHandler = new SafeAllocCoTaskMem(sizeof(uint));

            unsafe
            {
                void* unsafeBuffer = ((IntPtr)bufferPointerHandler).ToPointer();
                uint* unsafereturnedLengt = (uint*)((IntPtr)returnedLengtPointerHandler).ToPointer();

                var ret = PInvoke.CfGetPlaceholderInfo(FileHandle, CF_PLACEHOLDER_INFO_CLASS.CF_PLACEHOLDER_INFO_STANDARD, unsafeBuffer, (uint)InfoBufferLength, unsafereturnedLengt);

                var returnedLengthSpan = new ReadOnlySpan<byte>(unsafereturnedLengt, sizeof(uint));
                UInt32 returnedLength = BitConverter.ToUInt32(returnedLengthSpan.ToArray(), 0);

                if (returnedLength > 0)
                {
                    var bufferSpan = new ReadOnlySpan<byte>(unsafeBuffer, (int)returnedLength);
                    ResultInfo = MemoryMarshal.Read<CF_PLACEHOLDER_STANDARD_INFO>(bufferSpan);
                }
            }

            return ResultInfo;
        }
        public static CF_PLACEHOLDER_BASIC_INFO GetPlaceholderInfoBasic(Microsoft.Win32.SafeHandles.SafeFileHandle FileHandle)
        {
            int InfoBufferLength = 1024;
            CF_PLACEHOLDER_BASIC_INFO ResultInfo = default;

            using var bufferPointerHandler = new SafeAllocCoTaskMem(InfoBufferLength);
            using var returnedLengtPointerHandler = new SafeAllocCoTaskMem(sizeof(uint));

            unsafe
            {
                void* unsafeBuffer = ((IntPtr)bufferPointerHandler).ToPointer();
                uint* unsafereturnedLengt = (uint*)((IntPtr)returnedLengtPointerHandler).ToPointer();

                var ret = PInvoke.CfGetPlaceholderInfo(FileHandle, CF_PLACEHOLDER_INFO_CLASS.CF_PLACEHOLDER_INFO_BASIC, unsafeBuffer, (uint)InfoBufferLength, unsafereturnedLengt);

                var returnedLengthSpan = new ReadOnlySpan<byte>(unsafereturnedLengt, sizeof(uint));
                UInt32 returnedLength = BitConverter.ToUInt32(returnedLengthSpan.ToArray(), 0);

                if (returnedLength > 0)
                {
                    var bufferSpan = new ReadOnlySpan<byte>(unsafeBuffer, (int)returnedLength);
                    ResultInfo = MemoryMarshal.Read<CF_PLACEHOLDER_BASIC_INFO>(bufferSpan);
                }
            }
            return ResultInfo;
        }

        public static CF_PLACEHOLDER_STATE? GetPlaceholderState(Microsoft.Win32.SafeHandles.SafeFileHandle FileHandle)
        {
            CF_PLACEHOLDER_STATE? info;
            unsafe
            {
                var infoBufferSize = sizeof(FILE_ATTRIBUTE_TAG_INFO);
                using var infoHandler = new SafeAllocCoTaskMem(infoBufferSize);
                void* infoBuffer = ((IntPtr)infoHandler).ToPointer();

                //var result = DeviceIoControl(handle.DangerousGetHandle(), FSCTL_GET_REPARSE_POINT,
                //    IntPtr.Zero, 0, outBuffer, outBufferSize, out bytesReturned, IntPtr.Zero);


                if (PInvoke.GetFileInformationByHandleEx(FileHandle, FILE_INFO_BY_HANDLE_CLASS.FileAttributeTagInfo, infoBuffer, (uint)infoBufferSize))
                {
                    info = PInvoke.CfGetPlaceholderStateFromFileInfo(infoBuffer, FILE_INFO_BY_HANDLE_CLASS.FileAttributeTagInfo);
                }
                else
                {
                    info = null;
                }
            }

            return info;
        }

        public class ExtendedPlaceholderState
        {
            public ExtendedPlaceholderState(string fullPath)
            {
                this._FullPath = fullPath;

                using var findHandle = Kernel32.FindFirstFile(@"\\?\" + fullPath, out WIN32_FIND_DATA findData);
                this.PlaceholderState = (CF_PLACEHOLDER_STATE)CldApi.CfGetPlaceholderStateFromFindData(findData);
                this.Attributes = findData.dwFileAttributes;
                this.LastWriteTime = findData.ftLastWriteTime.ToDateTime();
            }
            public ExtendedPlaceholderState(WIN32_FIND_DATA findData, string directory)
            {
                if (!string.IsNullOrEmpty(directory)) { this._FullPath = directory + "\\" + findData.cFileName; }

                this.PlaceholderState = (CF_PLACEHOLDER_STATE)CldApi.CfGetPlaceholderStateFromFindData(findData);
                this.Attributes = findData.dwFileAttributes;
                this.LastWriteTime = findData.ftLastWriteTime.ToDateTime();
            }

            private string _FullPath;

            public CF_PLACEHOLDER_STATE PlaceholderState;
            public System.IO.FileAttributes Attributes;
            public DateTime LastWriteTime;

            private CF_PLACEHOLDER_BASIC_INFO? _PlaceholderInfoBasic;
            public CF_PLACEHOLDER_BASIC_INFO PlaceholderInfoBasic
            {
                get
                {
                    if (string.IsNullOrEmpty(this._FullPath)) { return default; }

                    if (_PlaceholderInfoBasic == null)
                    {
                        _PlaceholderInfoBasic = GetPlaceholderInfoBasic(this._FullPath, this.Attributes.HasFlag(System.IO.FileAttributes.Directory));
                    }
                    return _PlaceholderInfoBasic.Value;
                }
            }
        }
        //public static ExtendedPlaceholderState GetExtendedPlaceholderState(string fullPath)
        //{
        //    using (var findHandle = Vanara.PInvoke.Kernel32.FindFirstFile(@"\\?\" + fullPath, out Vanara.PInvoke.WIN32_FIND_DATA findData))
        //    {
        //        return GetExtendedPlaceholderState(findData);
        //    }
        //}
        //public static ExtendedPlaceholderState GetExtendedPlaceholderState(Vanara.PInvoke.WIN32_FIND_DATA findData)
        //{
        //    return new ExtendedPlaceholderState()
        //    {
        //        PlaceholderState = (CF_PLACEHOLDER_STATE)Vanara.PInvoke.CldApi.CfGetPlaceholderStateFromFindData(findData),
        //        Attributes = findData.dwFileAttributes,
        //        LastWriteTime = findData.ftLastWriteTime.ToDateTime()
        //    };

        //    var info = GetPlaceholderInfoBasic(directory + "\\" + findData.cFileName, findData.dwFileAttributes.HasFlag(Windows.Storage.FileAttributes.Directory));
        //}



        public static void ConvertToPlaceholder(string fullPath, string fileIdString)
        {
            Debug.WriteLine("ConvertToPlaceholder " + fullPath);

            CldApi.CfOpenFileWithOplock(fullPath, CldApi.CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE, out CldApi.SafeHCFFILE fHandle);
            try
            {
                if (!fHandle.IsInvalid)
                {
                    int fileIDSize = fileIdString.Length * Marshal.SizeOf(fileIdString[0]);
                    unsafe
                    {
                        fixed (void* fileID = fileIdString)
                        {
                            long* usnPtr = (long*)0;
                            var res = CldApi.CfConvertToPlaceholder((HFILE)fHandle.DangerousGetHandle(), (IntPtr)fileID, (uint)fileIDSize, CldApi.CF_CONVERT_FLAGS.CF_CONVERT_FLAG_ENABLE_ON_DEMAND_POPULATION, out long usn);
                            res.ThrowIfFailed();
                        }
                    }
                }
            }
            finally
            {
                fHandle.Dispose();
            }
        }
        public static void DehydratePlaceholder(string fullPath)
        {
            Debug.WriteLine("DehydratePlaceholder " + fullPath);
            using (var h = PInvoke.CreateFileW(@"\\?\" + fullPath,
                 FILE_ACCESS_FLAGS.FILE_READ_ATTRIBUTES,
                   FILE_SHARE_MODE.FILE_SHARE_READ |
                   FILE_SHARE_MODE.FILE_SHARE_WRITE |
                   FILE_SHARE_MODE.FILE_SHARE_DELETE,
                   null,
                   FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                   FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_EA,
                   null))
            {
                var res = CldApi.CfDehydratePlaceholder(h, 0, -1, CldApi.CF_DEHYDRATE_FLAGS.CF_DEHYDRATE_FLAG_NONE);
                res.ThrowIfFailed();

                res = CldApi.CfSetPinState(h, CldApi.CF_PIN_STATE.CF_PIN_STATE_UNSPECIFIED, CldApi.CF_SET_PIN_FLAGS.CF_SET_PIN_FLAG_NONE);
                //res = CldApi.CfSetInSyncState(h, CldApi.CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC, CldApi.CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);
            }
        }
        public static void HydratePlaceholder(string fullPath)
        {
            Debug.WriteLine("HydratePlaceholder " + fullPath);
            using (var h = PInvoke.CreateFileW(@"\\?\" + fullPath,
                 FILE_ACCESS_FLAGS.FILE_READ_ATTRIBUTES,
                   FILE_SHARE_MODE.FILE_SHARE_READ |
                   FILE_SHARE_MODE.FILE_SHARE_WRITE |
                   FILE_SHARE_MODE.FILE_SHARE_DELETE,
                   null,
                   FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                   FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_EA,
                   null))
            {
                unsafe
                {
                    int infoBufferSize = sizeof(CldApi.CF_FILE_RANGE) * 1000;
                    using var infoHandler = new SafeAllocCoTaskMem(infoBufferSize);
                    var infoBuffer = (IntPtr)infoHandler;
                    var fileSize = new System.IO.FileInfo(fullPath).Length;
                    var ret = CldApi.CfGetPlaceholderRangeInfo(h, CldApi.CF_PLACEHOLDER_RANGE_INFO_CLASS.CF_PLACEHOLDER_RANGE_INFO_ONDISK, 0, fileSize, infoBuffer, (uint)infoBufferSize, out uint ReturnedLength);


                    //var dat = Marshal.PtrToStructure<CldApi.CF_FILE_RANGE_BUFFER>(infoBuffer);
                    var pointPosition = infoBuffer;
                    var returnedItems = ReturnedLength / sizeof(CldApi.CF_FILE_RANGE);

                    CldApi.CF_FILE_RANGE[] arr = new CldApi.CF_FILE_RANGE[returnedItems];
                    for (int nI = 0; nI < returnedItems; nI++)
                    {
                        arr[nI] = Marshal.PtrToStructure<CldApi.CF_FILE_RANGE>(pointPosition);
                        pointPosition += sizeof(CldApi.CF_FILE_RANGE);
                    }
                }

                var res = CldApi.CfHydratePlaceholder(h, 0, -1, CldApi.CF_HYDRATE_FLAGS.CF_HYDRATE_FLAG_NONE);
                res.ThrowIfFailed();
            }
        }
        public static void SetInSyncState(string fullPath, CldApi.CF_IN_SYNC_STATE inSyncState, bool isDirectory)
        {
            Debug.WriteLine("SetInSyncState " + inSyncState.ToString() + " - " + fullPath.TrimEnd('\\'));

            FILE_ACCESS_FLAGS accessFlag = isDirectory ? FILE_ACCESS_FLAGS.FILE_GENERIC_READ : FILE_ACCESS_FLAGS.FILE_READ_EA;

            using (var h = PInvoke.CreateFileW(@"\\?\" + fullPath.TrimEnd('\\'),
                 accessFlag,
                   FILE_SHARE_MODE.FILE_SHARE_READ |
                   FILE_SHARE_MODE.FILE_SHARE_WRITE |
                   FILE_SHARE_MODE.FILE_SHARE_DELETE,
                   null,
                   FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                   0,
                   null))
            {
                if (h.IsInvalid)
                {
                    Debug.WriteLine("SetInSyncState INVALID Handle! " + fullPath.TrimEnd('\\'));
                    return;
                }
                SyncProviderUtils.LoggResponse(CldApi.CfSetInSyncState(h, inSyncState, CldApi.CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE));
            }
        }
        public static void SetInSyncState(Microsoft.Win32.SafeHandles.SafeFileHandle fileHandle, CldApi.CF_IN_SYNC_STATE inSyncState)
        {
            Debug.WriteLine("SetInSyncState " + inSyncState.ToString() + " - FileHandle " + fileHandle.DangerousGetHandle().ToString());

            SyncProviderUtils.LoggResponse(CldApi.CfSetInSyncState(fileHandle, inSyncState, CldApi.CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE));
        }
    }
}
