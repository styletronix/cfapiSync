using System;
using Windows.Win32.Storage.CloudFilters;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;
using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.PInvoke;
using System.Diagnostics;
using static Styletronix.CloudFilterApi.SafeHandlers;

namespace Styletronix
{
    public class CloudFilterApi
    {
        public enum CF_IN_SYNC_STATE
        {
            CF_IN_SYNC_STATE_NOT_IN_SYNC = 0,
            CF_IN_SYNC_STATE_IN_SYNC = 1
        }

        public static CF_PLACEHOLDER_BASIC_INFO GetPlaceholderInfoBasic(string fullPath, bool isDirectory)
        {
            using SafeCreateFileForCldApi h = new(fullPath, isDirectory);

            if (h.IsInvalid)
            {
                var err = Marshal.GetLastWin32Error();
                Debug.WriteLine("GetPlaceholderInfoBasic INVALID Handle! Error " + err + " - " + fullPath);
                return default;
            }
            try
            {
                return GetPlaceholderInfoBasic(h);
            }
            catch (Exception e)
            {
                Debug.WriteLine("GetPlaceholderInfoBasic FAILED: " + e.Message);
                return default;
            }
        }
        public static CF_PLACEHOLDER_STANDARD_INFO GetPlaceholderInfoStandard(string fullPath, bool isDirectory)
        {
            using SafeCreateFileForCldApi h = new(fullPath, isDirectory);

            if (h.IsInvalid)
            {
                var err = Marshal.GetLastWin32Error();
                Debug.WriteLine("GetPlaceholderInfoBasic INVALID Handle! Error " + err + " - " + fullPath);
                return default;
            }
            try
            {
                return GetPlaceholderInfoStandard(h);
            }
            catch (Exception e)
            {
                Debug.WriteLine("GetPlaceholderInfoBasic FAILED: " + e.Message);
                return default;
            }
        }
        public static CF_PLACEHOLDER_STANDARD_INFO GetPlaceholderInfoStandard(SafeFileHandle FileHandle)
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
        public static CF_PLACEHOLDER_BASIC_INFO GetPlaceholderInfoBasic(SafeFileHandle FileHandle)
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

        public static CF_PLACEHOLDER_STATE? GetPlaceholderState(SafeFileHandle FileHandle)
        {
            CF_PLACEHOLDER_STATE? info;
            unsafe
            {
                var infoBufferSize = sizeof(FILE_ATTRIBUTE_TAG_INFO);
                using var infoHandler = new SafeAllocCoTaskMem(infoBufferSize);
                void* infoBuffer = ((IntPtr)infoHandler).ToPointer();

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

        public class ExtendedPlaceholderState : IDisposable
        {
            private string _FullPath;
            private CF_PLACEHOLDER_BASIC_INFO? _PlaceholderInfoBasic;
            private SafeCreateFileForCldApi _SafeFileHandleForCldApi;
            private WIN32_FIND_DATA _FindData;

            public ExtendedPlaceholderState(string fullPath)
            {
                this._FullPath = fullPath;
                this._Reload();
            }
            public ExtendedPlaceholderState(WIN32_FIND_DATA findData, string directory)
            {
                if (!string.IsNullOrEmpty(directory)) { this._FullPath = directory + "\\" + findData.cFileName; }
                this._FindData = findData;

                this.PlaceholderState = (CF_PLACEHOLDER_STATE)CldApi.CfGetPlaceholderStateFromFindData(findData);
                this.Attributes = findData.dwFileAttributes;
                this.LastWriteTime = findData.ftLastWriteTime.ToDateTime();
            }


            public CF_PLACEHOLDER_STATE PlaceholderState;
            public System.IO.FileAttributes Attributes;
            public DateTime LastWriteTime;


            public CF_PLACEHOLDER_BASIC_INFO PlaceholderInfoBasic
            {
                get
                {
                    if (_PlaceholderInfoBasic == null)
                    {
                        if (string.IsNullOrEmpty(this._FullPath))
                        {
                            _PlaceholderInfoBasic = new CF_PLACEHOLDER_BASIC_INFO();
                        }
                        else if (!PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER))
                        {
                            _PlaceholderInfoBasic = new CF_PLACEHOLDER_BASIC_INFO();
                        }
                        else
                        {
                            _PlaceholderInfoBasic = GetPlaceholderInfoBasic(this.SafeFileHandleForCldApi);
                        }
                    }
                    return _PlaceholderInfoBasic.Value;
                }
            }
            public bool isDirectory { get { return this.Attributes.HasFlag(System.IO.FileAttributes.Directory); } }
            public bool isPlaceholder { get { return this.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER); } }

            public bool SetInSyncState(CF_IN_SYNC_STATE inSyncState)
            {
                if (!this.isPlaceholder) { return false; }
                if (this.PlaceholderInfoBasic.InSyncState.HasFlag((Windows.Win32.Storage.CloudFilters.CF_IN_SYNC_STATE)inSyncState)) { return true; }

                Debug.WriteLine("SetInSyncState " + this._FullPath + " " + inSyncState.ToString());

                var res = CldApi.CfSetInSyncState(this.SafeFileHandleForCldApi, (CldApi.CF_IN_SYNC_STATE)inSyncState, CldApi.CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);

                if (res.Succeeded)
                {
                    this._Reload();
                }
                else
                {
                    Debug.WriteLine("SetInSyncState FAILED " + this._FullPath + " Error: " + res.Code);
                }

                return res.Succeeded;
            }
            public bool ConvertToPlaceholder(string fileIdString)
            {
                if (string.IsNullOrEmpty(this._FullPath)) { return false; }
                if (this.isPlaceholder) { return true; }

                Debug.WriteLine("ConvertToPlaceholder " + this._FullPath);

                using SafeOpenFileWithOplock fHandle = new(this._FullPath, CldApi.CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE);
                if (fHandle.IsInvalid)
                {
                    Debug.WriteLine("ConvertToPlaceholder FAILED: Invalid Handle!");
                    return false;
                }

                HRESULT res;

                int fileIDSize = fileIdString.Length * Marshal.SizeOf(fileIdString[0]);
                unsafe
                {
                    fixed (void* fileID = fileIdString)
                    {
                        long* usnPtr = (long*)0;
                        res = CldApi.CfConvertToPlaceholder(fHandle, (IntPtr)fileID, (uint)fileIDSize, CldApi.CF_CONVERT_FLAGS.CF_CONVERT_FLAG_ENABLE_ON_DEMAND_POPULATION, out long usn);
                    }
                }

                if (res.Succeeded)
                {
                    this._Reload();
                }
                else
                {
                    Debug.WriteLine("ConvertToPlaceholder FAILED: Error " + res.Code);
                }
                return res.Succeeded;
            }
            public bool RevertPlaceholder()
            {
                if (string.IsNullOrEmpty(this._FullPath)) { return false; }
                if (this.isPlaceholder) { return true; }

                Debug.WriteLine("RevertPlaceholder " + this._FullPath);

                using SafeOpenFileWithOplock fHandle = new(this._FullPath, CldApi.CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE);
                if (fHandle.IsInvalid)
                {
                    Debug.WriteLine("RevertPlaceholder FAILED: Invalid Handle!");
                    return false;
                }

                HRESULT res = CldApi.CfRevertPlaceholder(fHandle, CldApi.CF_REVERT_FLAGS.CF_REVERT_FLAG_NONE);

                if (res.Succeeded)
                {
                    this._Reload();
                }
                else
                {
                    Debug.WriteLine("RevertPlaceholder FAILED: Error " + res.Code);
                }
                return res.Succeeded;
            }
            public bool DehydratePlaceholder(bool setPinStateUnspecified)
            {
                if (!this.isPlaceholder) { return false; }

                Debug.WriteLine("DehydratePlaceholder " + this._FullPath);

                var res = CldApi.CfDehydratePlaceholder(this.SafeFileHandleForCldApi, 0, -1, CldApi.CF_DEHYDRATE_FLAGS.CF_DEHYDRATE_FLAG_NONE);
                if (res.Succeeded)
                {
                    this._Reload();
                }
                else
                {
                    Debug.WriteLine("DehydratePlaceholder FAILED" + this._FullPath + " Error: " + res.Code);
                }

                if (res.Succeeded && setPinStateUnspecified)
                {
                    this.SetPinState(CF_PIN_STATE.CF_PIN_STATE_UNSPECIFIED);
                }

                return res.Succeeded;
            }
            public bool HydratePlaceholder(bool setPinStatePinned)
            {
                if (!this.isPlaceholder) { return false; }
                Debug.WriteLine("HydratePlaceholder " + this._FullPath);

                var res = CldApi.CfHydratePlaceholder(this.SafeFileHandleForCldApi, 0, -1, CldApi.CF_HYDRATE_FLAGS.CF_HYDRATE_FLAG_NONE);

                if (res.Succeeded)
                {
                    this._Reload();
                }
                else
                {
                    Debug.WriteLine("HydratePlaceholder FAILED" + this._FullPath + " Error: " + res.Code);
                }

                if (res.Succeeded && setPinStatePinned)
                {
                    this.SetPinState(CF_PIN_STATE.CF_PIN_STATE_PINNED);
                }

                return res.Succeeded;
            }
            public bool SetPinState(CF_PIN_STATE state)
            {
                if (!this.isPlaceholder) { return false; }
                if (this.PlaceholderInfoBasic.PinState == state) { return true; }

                Debug.WriteLine("SetPinState " + this._FullPath + " " + state.ToString());
                var res = CldApi.CfSetPinState(this.SafeFileHandleForCldApi, (CldApi.CF_PIN_STATE)state, CldApi.CF_SET_PIN_FLAGS.CF_SET_PIN_FLAG_NONE);

                if (res.Succeeded)
                {
                    this._Reload();
                }
                else
                {
                    Debug.WriteLine("SetPinState FAILED " + this._FullPath + " Error: " + res.Code);
                }

                return res.Succeeded;
            }

            public bool EnableOnDemandPopulation(string fileIdString)
            {
                if (string.IsNullOrEmpty(this._FullPath)) { return false; }
                if (!this.isPlaceholder) { return false; }
                if (!this.isDirectory) { return false; }

                Debug.WriteLine("EnableOnDemandPopulation " + this._FullPath);

                using SafeOpenFileWithOplock fHandle = new(this._FullPath, CldApi.CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE);
                if (fHandle.IsInvalid)
                {
                    Debug.WriteLine("EnableOnDemandPopulation FAILED: Invalid Handle!");
                    return false;
                }

                HRESULT res;

                int fileIDSize = fileIdString.Length * Marshal.SizeOf(fileIdString[0]);
                long usn = 0;

                unsafe
                {
                    fixed (void* fileID = fileIdString)
                    {
                        res = CldApi.CfUpdatePlaceholder(fHandle, new CldApi.CF_FS_METADATA(), (IntPtr)fileID, (uint)fileIDSize, null, 0, CldApi.CF_UPDATE_FLAGS.CF_UPDATE_FLAG_ENABLE_ON_DEMAND_POPULATION, ref usn);
                    }
                }

                if (res.Succeeded)
                {
                    this._Reload();
                }
                else
                {
                    Debug.WriteLine("ConvertToPlaceholder FAILED: Error " + res.Code);
                }
                return res.Succeeded;
            }
            public void Reload()
            {
                this._Reload();
            }
            public SafeCreateFileForCldApi SafeFileHandleForCldApi
            {
                get
                {
                    if (this._SafeFileHandleForCldApi == null)
                    {
                        this._SafeFileHandleForCldApi = new(this._FullPath, this.isDirectory);
                    }
                    return this._SafeFileHandleForCldApi;
                }
            }



            private void _Reload()
            {
                using var findHandle = Kernel32.FindFirstFile(@"\\?\" + this._FullPath, out WIN32_FIND_DATA findData);
                this.PlaceholderState = (CF_PLACEHOLDER_STATE)CldApi.CfGetPlaceholderStateFromFindData(findData);
                this.Attributes = findData.dwFileAttributes;
                this.LastWriteTime = findData.ftLastWriteTime.ToDateTime();
                this._PlaceholderInfoBasic = null;
            }


            private bool disposedValue;
            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        this._SafeFileHandleForCldApi?.Dispose();
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


        public static bool ConvertToPlaceholder(string fullPath, string fileIdString)
        {
            Debug.WriteLine("ConvertToPlaceholder " + fullPath);

            using SafeOpenFileWithOplock fHandle = new(fullPath, CldApi.CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE);

            if (!fHandle.IsInvalid)
            {
                int fileIDSize = fileIdString.Length * Marshal.SizeOf(fileIdString[0]);
                unsafe
                {
                    fixed (void* fileID = fileIdString)
                    {
                        long* usnPtr = (long*)0;
                        var res = CldApi.CfConvertToPlaceholder(fHandle, (IntPtr)fileID, (uint)fileIDSize, CldApi.CF_CONVERT_FLAGS.CF_CONVERT_FLAG_ENABLE_ON_DEMAND_POPULATION, out long usn);
                        res.ThrowIfFailed();
                    }
                }
                return true;
            }

            return false;
        }
        public static void DehydratePlaceholder(string fullPath)
        {
            Debug.WriteLine("DehydratePlaceholder " + fullPath);

            using SafeCreateFileForCldApi h = new(fullPath, false);

            var res = CldApi.CfDehydratePlaceholder(h, 0, -1, CldApi.CF_DEHYDRATE_FLAGS.CF_DEHYDRATE_FLAG_NONE);
            res.ThrowIfFailed();

            //res = CldApi.Cf(h, CldApi.CF_PIN_STATE.CF_PIN_STATE_UNSPECIFIED, CldApi.CF_SET_PIN_FLAGS.CF_SET_PIN_FLAG_NONE);
        }
        public static void HydratePlaceholder(string fullPath)
        {
            Debug.WriteLine("HydratePlaceholder " + fullPath);
            using SafeCreateFileForCldApi h = new(fullPath, false);

            unsafe
            {
                int infoBufferSize = sizeof(CldApi.CF_FILE_RANGE) * 1000;
                using var infoHandler = new SafeAllocCoTaskMem(infoBufferSize);
                var infoBuffer = (IntPtr)infoHandler;
                var fileSize = new System.IO.FileInfo(fullPath).Length;
                var ret = CldApi.CfGetPlaceholderRangeInfo(h, CldApi.CF_PLACEHOLDER_RANGE_INFO_CLASS.CF_PLACEHOLDER_RANGE_INFO_ONDISK, 0, fileSize, infoBuffer, (uint)infoBufferSize, out uint ReturnedLength);

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
        public static bool SetInSyncState(string fullPath, CF_IN_SYNC_STATE inSyncState, bool isDirectory)
        {
            string d = fullPath.TrimEnd('\\');
            Debug.WriteLine("SetInSyncState " + inSyncState.ToString() + " - " + d);

            using SafeCreateFileForCldApi h = new(fullPath, isDirectory);

            if (h.IsInvalid)
            {
                Debug.WriteLine("SetInSyncState INVALID Handle! " + fullPath.TrimEnd('\\'));
                return false;
            }

            var result = CldApi.CfSetInSyncState((SafeFileHandle)h, (CldApi.CF_IN_SYNC_STATE)inSyncState, CldApi.CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);
            SyncProviderUtils.LoggResponse(result);
            return result.Succeeded;
        }
        public static bool SetInSyncState(SafeFileHandle fileHandle, CF_IN_SYNC_STATE inSyncState)
        {
            Debug.WriteLine("SetInSyncState " + inSyncState.ToString() + " - FileHandle " + fileHandle.DangerousGetHandle().ToString());

            var res = CldApi.CfSetInSyncState(fileHandle, (CldApi.CF_IN_SYNC_STATE)inSyncState, CldApi.CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);
            SyncProviderUtils.LoggResponse(res);

            return res.Succeeded;
        }


        public class SafeHandlers
        {
            public class SafeCreateFileForCldApi : IDisposable
            {
                public SafeCreateFileForCldApi(string fullPath, bool isDirectory)
                {
                    FILE_ACCESS_FLAGS accessFlag = isDirectory ? FILE_ACCESS_FLAGS.FILE_GENERIC_READ : FILE_ACCESS_FLAGS.FILE_READ_EA;
                    FILE_FLAGS_AND_ATTRIBUTES attributsFlag = isDirectory ? FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS : FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL;

                    this._handle = PInvoke.CreateFileW(@"\\?\" + fullPath,
                         accessFlag,
                           FILE_SHARE_MODE.FILE_SHARE_READ |
                           FILE_SHARE_MODE.FILE_SHARE_WRITE |
                           FILE_SHARE_MODE.FILE_SHARE_DELETE,
                           null,
                           FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                           attributsFlag,
                           null);
                }

                public bool IsInvalid => _handle.IsInvalid;

                public static implicit operator SafeFileHandle(SafeCreateFileForCldApi instance)
                {
                    return instance._handle;
                }

                public static implicit operator HFILE(SafeCreateFileForCldApi instance)
                {
                    return (HFILE)instance._handle;
                }

                private SafeFileHandle _handle;


                private bool disposedValue;

                protected virtual void Dispose(bool disposing)
                {
                    if (!disposedValue)
                    {
                        if (disposing)
                        {
                            this._handle?.Dispose();
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
            public class SafeOpenFileWithOplock : IDisposable
            {
                public SafeOpenFileWithOplock(string fullPath, CldApi.CF_OPEN_FILE_FLAGS Flags)
                {
                    CldApi.CfOpenFileWithOplock(fullPath, Flags, out this._handle).ThrowIfFailed();
                }

                public bool IsInvalid => _handle.IsInvalid;

                public static implicit operator CldApi.SafeHCFFILE(SafeOpenFileWithOplock instance)
                {
                    return instance._handle;
                }
                public static implicit operator HFILE(SafeOpenFileWithOplock instance)
                {
                    return (HFILE)instance._handle.DangerousGetHandle();
                }

                private CldApi.SafeHCFFILE _handle;


                private bool disposedValue;

                protected virtual void Dispose(bool disposing)
                {
                    if (!disposedValue)
                    {
                        if (disposing)
                        {
                            this._handle?.Dispose();
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
                public SafeTransferKey(SafeFileHandle safeHandle)
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
        }
    }
}
