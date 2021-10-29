using Styletronix.CloudSyncProvider;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Styletronix.CloudFilterApi.SafeHandlers;
using static Vanara.PInvoke.CldApi;

namespace Styletronix
{
    public partial class CloudFilterApi
    {
        public class ExtendedPlaceholderState : IDisposable
        {
            private readonly string _FullPath;
            private CF_PLACEHOLDER_BASIC_INFO? _PlaceholderInfoBasic;
            private CF_PLACEHOLDER_STANDARD_INFO? _PlaceholderInfoStandard;
            private SafeCreateFileForCldApi _SafeFileHandleForCldApi;
            private WIN32_FIND_DATA _FindData;

            public ExtendedPlaceholderState(string fullPath)
            {
                _FullPath = fullPath;
                Reload();
            }
            public ExtendedPlaceholderState(WIN32_FIND_DATA findData, string directory)
            {
                if (!string.IsNullOrEmpty(directory)) { _FullPath = directory + "\\" + findData.cFileName; }
                _FindData = findData;

                PlaceholderState = CfGetPlaceholderStateFromFindData(findData);
                Attributes = findData.dwFileAttributes;
                LastWriteTime = findData.ftLastWriteTime.ToDateTime();
            }

            public string FullPath => _FullPath;

            public CF_PLACEHOLDER_STATE PlaceholderState;
            public System.IO.FileAttributes Attributes;
            public DateTime LastWriteTime;
            public ulong FileSize;
            public string ETag;
            // Fake ID because FileID is not usable due to only 1 byte is returned from the System.
            public string FileId = Guid.NewGuid().ToString();

            public CF_PLACEHOLDER_BASIC_INFO PlaceholderInfoBasic
            {
                get
                {
                    if (_PlaceholderInfoBasic == null)
                    {
                        if (string.IsNullOrEmpty(_FullPath))
                        {
                            _PlaceholderInfoBasic = new CF_PLACEHOLDER_BASIC_INFO();
                        }
                        else if (!PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER))
                        {
                            _PlaceholderInfoBasic = new CF_PLACEHOLDER_BASIC_INFO();
                        }
                        else
                        {
                            _PlaceholderInfoBasic = GetPlaceholderInfoBasic(SafeFileHandleForCldApi);
                        }
                    }
                    return _PlaceholderInfoBasic.Value;
                }
            }
            public CF_PLACEHOLDER_STANDARD_INFO PlaceholderInfoStandard
            {
                get
                {
                    if (_PlaceholderInfoStandard == null)
                    {
                        if (string.IsNullOrEmpty(_FullPath))
                        {
                            _PlaceholderInfoStandard = new CF_PLACEHOLDER_STANDARD_INFO();
                        }
                        else if (!PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER))
                        {
                            _PlaceholderInfoStandard = new CF_PLACEHOLDER_STANDARD_INFO();
                        }
                        else
                        {
                            _PlaceholderInfoStandard = GetPlaceholderInfoStandard(SafeFileHandleForCldApi);
                        }
                    }
                    return _PlaceholderInfoStandard.Value;
                }
            }
            public bool IsDirectory => Attributes.HasFlag(System.IO.FileAttributes.Directory);
            public bool IsPlaceholder => PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER);

            public GenericResult SetInSyncState(CF_IN_SYNC_STATE inSyncState)
            {
                if (!IsPlaceholder) { return new GenericResult(NtStatus.STATUS_NOT_A_CLOUD_FILE); }
                if (PlaceholderInfoBasic.InSyncState == inSyncState) { return new GenericResult(); }

                Debug.WriteLine("SetInSyncState " + _FullPath + " " + inSyncState.ToString(), System.Diagnostics.TraceLevel.Info);

                HRESULT res = CfSetInSyncState(SafeFileHandleForCldApi, inSyncState, CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);

                if (res.Succeeded)
                {
                    Reload();
                    return new GenericResult();
                }
                else
                {
                    Debug.WriteLine("SetInSyncState FAILED " + _FullPath + " Error: " + res.Code, System.Diagnostics.TraceLevel.Warning);
                    return new GenericResult((int)res);
                }
            }
            /// <summary>
            ///  Convert File or Directory to Placeholder if required. Returns true if conversion was successful or if it is a placeholder already.
            /// </summary>
            /// <param name="fileIdString"></param>
            /// <returns></returns>
            public bool ConvertToPlaceholder()
            {
                if (string.IsNullOrEmpty(_FullPath)) { return false; }
                if (IsPlaceholder) { return true; }

                Debug.WriteLine("ConvertToPlaceholder " + _FullPath, System.Diagnostics.TraceLevel.Info);

                using SafeOpenFileWithOplock fHandle = new(_FullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE);

                if (fHandle.IsInvalid)
                {
                    Debug.WriteLine("ConvertToPlaceholder FAILED: Invalid Handle!", System.Diagnostics.TraceLevel.Warning);
                    return false;
                }

                bool isExcluded = false;
                bool pinStateSet = false;

                if (System.IO.Path.GetFileName(_FullPath).Equals(@"$Recycle.bin", StringComparison.CurrentCultureIgnoreCase))
                {
                    isExcluded = true;
                }

                CF_CONVERT_FLAGS flags = isExcluded ? CF_CONVERT_FLAGS.CF_CONVERT_FLAG_MARK_IN_SYNC : CF_CONVERT_FLAGS.CF_CONVERT_FLAG_ENABLE_ON_DEMAND_POPULATION;

                HRESULT res;
                int fileIDSize = this.FileId.Length * Marshal.SizeOf(this.FileId[0]);
                unsafe
                {
                    fixed (void* fileID = this.FileId)
                    {
                        long* usnPtr = (long*)0;
                        res = CfConvertToPlaceholder(fHandle, (IntPtr)fileID, (uint)fileIDSize, flags, out long usn);
                    }
                }

                if (res.Succeeded && isExcluded)
                {
                    pinStateSet = SetPinState(CF_PIN_STATE.CF_PIN_STATE_EXCLUDED);
                }

                if (res.Succeeded)
                {
                    if (!pinStateSet)
                    {
                        Reload();
                    }
                }
                else
                {
                    Debug.WriteLine("ConvertToPlaceholder FAILED: Error " + res.Code, System.Diagnostics.TraceLevel.Error);
                }
                return res.Succeeded;


            }
            public GenericResult RevertPlaceholder(bool allowDataLoos)
            {
                if (string.IsNullOrEmpty(_FullPath))
                {
                    return new GenericResult(CloudExceptions.FileOrDirectoryNotFound);
                }

                if (!IsPlaceholder)
                {
                    return new GenericResult()
                    {
                        Status = NtStatus.STATUS_NOT_A_CLOUD_FILE,
                        Message = NtStatus.STATUS_NOT_A_CLOUD_FILE.ToString(),
                        Succeeded = true
                    };
                }

                Debug.WriteLine("RevertPlaceholder " + _FullPath, System.Diagnostics.TraceLevel.Info);

                using SafeOpenFileWithOplock fHandle = new(_FullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE);

                if (fHandle.IsInvalid)
                {
                    Debug.WriteLine("RevertPlaceholder FAILED: Invalid Handle!", System.Diagnostics.TraceLevel.Warning);
                    return new GenericResult(NtStatus.STATUS_CLOUD_FILE_IN_USE);
                }

                if (!allowDataLoos)
                {
                    GenericResult ret = HydratePlaceholder();
                    if (!ret.Succeeded)
                    {
                        return ret;
                    }

                    if (PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL) ||
                        PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_INVALID) ||
                        !PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC))
                    {
                        return new GenericResult(NtStatus.STATUS_CLOUD_FILE_NOT_IN_SYNC);
                    }
                }

                HRESULT res = CfRevertPlaceholder(fHandle, CF_REVERT_FLAGS.CF_REVERT_FLAG_NONE);

                if (res.Succeeded)
                {
                    Reload();
                }
                else
                {
                    Debug.WriteLine("RevertPlaceholder FAILED: Error " + res.Code, System.Diagnostics.TraceLevel.Warning);
                }

                return new GenericResult((int)res);
            }
            public GenericResult DehydratePlaceholder(bool setPinStateUnspecified)
            {
                if (!IsPlaceholder) { return new GenericResult(NtStatus.STATUS_NOT_A_CLOUD_FILE); }
                if (PlaceholderInfoBasic.PinState == CF_PIN_STATE.CF_PIN_STATE_PINNED) { return new GenericResult(NtStatus.STATUS_CLOUD_FILE_PINNED); }

                Debug.WriteLine("DehydratePlaceholder " + _FullPath, System.Diagnostics.TraceLevel.Info);

                HRESULT res = CfDehydratePlaceholder(SafeFileHandleForCldApi, 0, -1, CF_DEHYDRATE_FLAGS.CF_DEHYDRATE_FLAG_NONE);
                if (res.Succeeded)
                {
                    Reload();
                }
                else
                {
                    Debug.WriteLine("DehydratePlaceholder FAILED" + _FullPath + " Error: " + res.Code, System.Diagnostics.TraceLevel.Warning);
                    return new GenericResult((int)res);
                }

                if (res.Succeeded && setPinStateUnspecified)
                {
                    SetPinState(CF_PIN_STATE.CF_PIN_STATE_UNSPECIFIED);
                }

                return new GenericResult((int)res);
            }
            public GenericResult HydratePlaceholder()
            {
                if (string.IsNullOrEmpty(_FullPath)) { return new GenericResult(CloudExceptions.FileOrDirectoryNotFound); }
                if (!IsPlaceholder) { return new GenericResult(NtStatus.STATUS_CLOUD_FILE_NOT_SUPPORTED); }

                Debug.WriteLine("HydratePlaceholder " + _FullPath, System.Diagnostics.TraceLevel.Info);

                HRESULT res = CfHydratePlaceholder(SafeFileHandleForCldApi, 0, -1, CF_HYDRATE_FLAGS.CF_HYDRATE_FLAG_NONE);

                if (res.Succeeded)
                {
                    Reload();
                    return new GenericResult();
                }
                else
                {
                    Debug.WriteLine("HydratePlaceholder FAILED " + _FullPath + " Error: " + res.Code, System.Diagnostics.TraceLevel.Warning);
                    return new GenericResult((int)res);
                }
            }

            public async Task<GenericResult> HydratePlaceholderAsync()
            {
                if (string.IsNullOrEmpty(_FullPath)) { return new GenericResult(CloudExceptions.FileOrDirectoryNotFound); }
                if (!IsPlaceholder) { return new GenericResult(NtStatus.STATUS_CLOUD_FILE_NOT_SUPPORTED); }

                Debug.WriteLine("HydratePlaceholderAsync " + _FullPath, System.Diagnostics.TraceLevel.Info);


                HRESULT res = await Task.Run(() =>
                 {
                     return CfHydratePlaceholder(SafeFileHandleForCldApi, 0, -1, CF_HYDRATE_FLAGS.CF_HYDRATE_FLAG_NONE);
                 }).ConfigureAwait(false);

                if (res.Succeeded)
                {
                    Debug.WriteLine("HydratePlaceholderAsync Completed: " + _FullPath, System.Diagnostics.TraceLevel.Verbose);
                    Reload();
                    return new GenericResult();
                }
                else
                {
                    Debug.WriteLine("HydratePlaceholderAsync FAILED " + _FullPath + " Error: " + res.Code, System.Diagnostics.TraceLevel.Warning);
                    return new GenericResult((int)res);
                }
            }

            public bool SetPinState(CF_PIN_STATE state)
            {
                if (!IsPlaceholder) { return false; }
                if (((int)PlaceholderInfoBasic.PinState) == ((int)state)) { return true; }

                Debug.WriteLine("SetPinState " + _FullPath + " " + state.ToString(), System.Diagnostics.TraceLevel.Info);
                HRESULT res = CfSetPinState(SafeFileHandleForCldApi, state, CF_SET_PIN_FLAGS.CF_SET_PIN_FLAG_NONE);

                if (res.Succeeded)
                {
                    Reload();
                }
                else
                {
                    Debug.WriteLine("SetPinState FAILED " + _FullPath + " Error: " + res.Code, System.Diagnostics.TraceLevel.Warning);
                }

                return res.Succeeded;
            }

            public bool EnableOnDemandPopulation()
            {
                if (string.IsNullOrEmpty(_FullPath)) { return false; }
                if (!IsPlaceholder) { return false; }
                if (!IsDirectory) { return false; }

                Debug.WriteLine("EnableOnDemandPopulation " + _FullPath, System.Diagnostics.TraceLevel.Info);

                using SafeOpenFileWithOplock fHandle = new(_FullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_NONE);

                if (fHandle.IsInvalid)
                {
                    Debug.WriteLine("EnableOnDemandPopulation FAILED: Invalid Handle!", System.Diagnostics.TraceLevel.Warning);
                    return false;
                }

                HRESULT res;

                int fileIDSize = this.FileId.Length * Marshal.SizeOf(this.FileId[0]);
                long usn = 0;

                unsafe
                {
                    fixed (void* fileID = this.FileId)
                    {
                        res = CfUpdatePlaceholder(fHandle, new CF_FS_METADATA(), (IntPtr)fileID, (uint)fileIDSize, null, 0, CF_UPDATE_FLAGS.CF_UPDATE_FLAG_ENABLE_ON_DEMAND_POPULATION, ref usn);
                    }
                }

                if (res.Succeeded)
                {
                    Reload();
                }
                else
                {
                    Debug.WriteLine("ConvertToPlaceholder FAILED: Error " + res.Code, System.Diagnostics.TraceLevel.Warning);
                }
                return res.Succeeded;
            }

            public GenericResult UpdatePlaceholder(CloudSyncProvider.Placeholder placeholder, CF_UPDATE_FLAGS cF_UPDATE_FLAGS)
            {
                return UpdatePlaceholder(placeholder, cF_UPDATE_FLAGS, false);
            }
            public GenericResult UpdatePlaceholder(CloudSyncProvider.Placeholder placeholder, CF_UPDATE_FLAGS cF_UPDATE_FLAGS, bool markDataInvalid)
            {
                if (string.IsNullOrEmpty(_FullPath)) { return new GenericResult(CloudExceptions.FileOrDirectoryNotFound); }
                if (!IsPlaceholder) { return new GenericResult(NtStatus.STATUS_CLOUD_FILE_NOT_SUPPORTED); }

                Debug.WriteLine("UpdatePlaceholder " + _FullPath + " Flags: " + cF_UPDATE_FLAGS.ToString(), System.Diagnostics.TraceLevel.Info);
                GenericResult res = new();

                using SafeOpenFileWithOplock fHandle = new(_FullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE);

                if (fHandle.IsInvalid)
                {
                    Debug.WriteLine("UpdatePlaceholder FAILED: Invalid Handle!", System.Diagnostics.TraceLevel.Warning);
                    return new GenericResult(NtStatus.STATUS_CLOUD_FILE_IN_USE);
                }

                int fileIDSize = this.FileId.Length * Marshal.SizeOf(this.FileId[0]);
                long usn = 0;

                unsafe
                {
                    fixed (void* fileID = this.FileId)
                    {
                        CF_FILE_RANGE[] dehydrateRanges = null;
                        uint dehydrateRangesCount = 0;
                        if (markDataInvalid)
                        {
                            dehydrateRanges = new CF_FILE_RANGE[1];
                            dehydrateRanges[0] = new CF_FILE_RANGE() { StartingOffset = 0, Length = (long)this.FileSize };
                            dehydrateRangesCount = 1;
                        }
                        HRESULT res1 = CfUpdatePlaceholder(fHandle, CreateFSMetaData(placeholder), (IntPtr)fileID, (uint)fileIDSize, dehydrateRanges, dehydrateRangesCount, cF_UPDATE_FLAGS, ref usn);
                        if (!res1.Succeeded)
                        {
                            res.SetException(res1.GetException());
                        }
                    }
                }

                if (res.Succeeded)
                {
                    Reload();
                }
                else
                {
                    Debug.WriteLine("UpdatePlaceholder FAILED: Error " + res.Message, System.Diagnostics.TraceLevel.Warning);
                }
                return res;
            }

            public HFILE SafeFileHandleForCldApi
            {
                get
                {
                    if (_SafeFileHandleForCldApi == null)
                    {
                        _SafeFileHandleForCldApi = new(_FullPath, IsDirectory);
                    }
                    return _SafeFileHandleForCldApi;
                }
            }


            public void Reload()
            {
                Styletronix.Debug.WriteLine("Reload Placeholder Data: " + this.FullPath, System.Diagnostics.TraceLevel.Verbose);
                using Kernel32.SafeSearchHandle findHandle = Kernel32.FindFirstFile(@"\\?\" + _FullPath, out WIN32_FIND_DATA findData);
                PlaceholderState = CfGetPlaceholderStateFromFindData(findData);
                Attributes = findData.dwFileAttributes;
                LastWriteTime = findData.ftLastWriteTime.ToDateTime();
                _PlaceholderInfoBasic = null;
                FileSize = findData.FileSize;
                ETag = "_" + LastWriteTime.ToUniversalTime().Ticks + "_" + FileSize;
            }


            private bool disposedValue;
            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        _SafeFileHandleForCldApi?.Dispose();
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
}
