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
            private CF_PLACEHOLDER_STANDARD_INFO? _PlaceholderInfoStandard;
            private SafeCreateFileForCldApi _SafeFileHandleForCldApi;
            private WIN32_FIND_DATA _FindData;

            public ExtendedPlaceholderState(string fullPath)
            {
                _FullPath = fullPath;

                using Kernel32.SafeSearchHandle findHandle = Kernel32.FindFirstFile(@"\\?\" + _FullPath, out WIN32_FIND_DATA findData);
                SetValuesByFindData(findData);
            }
            public ExtendedPlaceholderState(WIN32_FIND_DATA findData, string directory)
            {
                if (!string.IsNullOrEmpty(directory)) { _FullPath = directory + "\\" + findData.cFileName; }
                _FindData = findData;

                SetValuesByFindData(findData);
            }

            public string FullPath => _FullPath;

            public CF_PLACEHOLDER_STATE PlaceholderState;
            public System.IO.FileAttributes Attributes;
            public DateTime LastWriteTime;
            public ulong FileSize;
            public string ETag;
            // Fake ID.
            public string FileId = Guid.NewGuid().ToString();

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
                            Styletronix.Debug.WriteLine("GetPlaceholderInfoStandard: " + FullPath, System.Diagnostics.TraceLevel.Verbose);
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
                if (PlaceholderInfoStandard.InSyncState == inSyncState) { return new GenericResult(); }

                Debug.WriteLine("SetInSyncState " + _FullPath + " " + inSyncState.ToString(), System.Diagnostics.TraceLevel.Verbose);

                HRESULT res = CfSetInSyncState(SafeFileHandleForCldApi, inSyncState, CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);

                if (res.Succeeded)
                {
                    //Reload();

                    // Prevent reload by applying results directly to cached values:
                    if (_PlaceholderInfoStandard != null)
                    {
                        CF_PLACEHOLDER_STANDARD_INFO p = _PlaceholderInfoStandard.Value;
                        p.InSyncState = inSyncState;
                        _PlaceholderInfoStandard = p;
                    }

                    if (inSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC)
                    {
                        PlaceholderState |= CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC;
                    }
                    else
                    {
                        PlaceholderState ^= CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC;
                    }


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
            public GenericResult ConvertToPlaceholder(bool markInSync)
            {
                if (string.IsNullOrEmpty(_FullPath)) { return new GenericResult(NtStatus.STATUS_UNSUCCESSFUL); }
                if (IsPlaceholder) { return new GenericResult(NtStatus.STATUS_SUCCESS); }

                Debug.WriteLine("ConvertToPlaceholder " + _FullPath, System.Diagnostics.TraceLevel.Verbose);

                using SafeOpenFileWithOplock fHandle = new(_FullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE);

                if (fHandle.IsInvalid)
                {
                    Debug.WriteLine("ConvertToPlaceholder FAILED: Invalid Handle!", System.Diagnostics.TraceLevel.Warning);
                    return new GenericResult(NtStatus.STATUS_UNSUCCESSFUL);
                }

                CF_CONVERT_FLAGS flags = markInSync ? CF_CONVERT_FLAGS.CF_CONVERT_FLAG_MARK_IN_SYNC : CF_CONVERT_FLAGS.CF_CONVERT_FLAG_ENABLE_ON_DEMAND_POPULATION;

                HRESULT res;
                int fileIDSize = FileId.Length * Marshal.SizeOf(FileId[0]);

                unsafe
                {
                    fixed (void* fileID = FileId)
                    {
                        res = CfConvertToPlaceholder(fHandle, (IntPtr)fileID, (uint)fileIDSize, flags, out long usn);
                    }
                }

                if (res.Succeeded)
                {
                        Reload();
                }
                else
                {
                    Debug.WriteLine("ConvertToPlaceholder FAILED: Error " + res.Code, System.Diagnostics.TraceLevel.Error);
                }
                return new GenericResult(res.Succeeded);
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

                Debug.WriteLine("RevertPlaceholder " + _FullPath, System.Diagnostics.TraceLevel.Verbose);

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
                if (PlaceholderInfoStandard.PinState == CF_PIN_STATE.CF_PIN_STATE_PINNED) { return new GenericResult(NtStatus.STATUS_CLOUD_FILE_PINNED); }

                Debug.WriteLine("DehydratePlaceholder " + _FullPath, System.Diagnostics.TraceLevel.Verbose);

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

                Debug.WriteLine("HydratePlaceholder " + _FullPath, System.Diagnostics.TraceLevel.Verbose);

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
                if (((int)PlaceholderInfoStandard.PinState) == ((int)state)) { return true; }

                Debug.WriteLine("SetPinState " + _FullPath + " " + state.ToString(), System.Diagnostics.TraceLevel.Verbose);
                HRESULT res = CfSetPinState(SafeFileHandleForCldApi, state, CF_SET_PIN_FLAGS.CF_SET_PIN_FLAG_NONE);

                if (res.Succeeded)
                {
                    //Reload();

                    // Prevent reload by applying results directly to cached values:
                    if (_PlaceholderInfoStandard != null)
                    {
                        CF_PLACEHOLDER_STANDARD_INFO p = _PlaceholderInfoStandard.Value;
                        p.PinState = state;
                        _PlaceholderInfoStandard = p;
                    }
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
                if (this.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL)) { return true; }

                Debug.WriteLine("EnableOnDemandPopulation " + _FullPath, System.Diagnostics.TraceLevel.Verbose);

                using SafeOpenFileWithOplock fHandle = new(_FullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_NONE);

                if (fHandle.IsInvalid)
                {
                    Debug.WriteLine("EnableOnDemandPopulation FAILED: Invalid Handle!", System.Diagnostics.TraceLevel.Warning);
                    return false;
                }

                HRESULT res;

                int fileIDSize = FileId.Length * Marshal.SizeOf(FileId[0]);
                long usn = 0;

                unsafe
                {
                    fixed (void* fileID = FileId)
                    {
                        res = CfUpdatePlaceholder(fHandle, new CF_FS_METADATA(), (IntPtr)fileID, (uint)fileIDSize, null, 0, CF_UPDATE_FLAGS.CF_UPDATE_FLAG_ENABLE_ON_DEMAND_POPULATION, ref usn);
                    }
                }

                if (res.Succeeded)
                {
                    //Reload of Placeholder after EnableOnDemandPopulation triggers FETCH_PLACEHOLDERS  
                    //Reload();
                    PlaceholderState |= CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL | CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIALLY_ON_DISK;
                }
                else
                {
                    Debug.WriteLine("ConvertToPlaceholder FAILED: Error " + res.Code, System.Diagnostics.TraceLevel.Warning);
                }
                return res.Succeeded;
            }
            public bool DisableOnDemandPopulation()
            {
                if (string.IsNullOrEmpty(_FullPath)) { return false; }
                if (!IsPlaceholder) { return false; }
                if (!IsDirectory) { return false; }
                if (!this.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL)) { return true; }

                Debug.WriteLine("EnableOnDemandPopulation " + _FullPath, System.Diagnostics.TraceLevel.Verbose);

                using SafeOpenFileWithOplock fHandle = new(_FullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_NONE);

                if (fHandle.IsInvalid)
                {
                    Debug.WriteLine("EnableOnDemandPopulation FAILED: Invalid Handle!", System.Diagnostics.TraceLevel.Warning);
                    return false;
                }

                HRESULT res;

                int fileIDSize = FileId.Length * Marshal.SizeOf(FileId[0]);
                long usn = 0;

                unsafe
                {
                    fixed (void* fileID = FileId)
                    {
                        res = CfUpdatePlaceholder(fHandle, new CF_FS_METADATA(), (IntPtr)fileID, (uint)fileIDSize, null, 0, CF_UPDATE_FLAGS.CF_UPDATE_FLAG_DISABLE_ON_DEMAND_POPULATION, ref usn);
                    }
                }

                if (res.Succeeded)
                {
                    //Reload of Placeholder after EnableOnDemandPopulation triggers FETCH_PLACEHOLDERS  
                    //Reload();
                    PlaceholderState ^= CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL;
                    PlaceholderState ^= CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIALLY_ON_DISK;
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

                Debug.WriteLine("UpdatePlaceholder " + _FullPath + " Flags: " + cF_UPDATE_FLAGS.ToString(), System.Diagnostics.TraceLevel.Verbose);
                GenericResult res = new();

                using SafeOpenFileWithOplock fHandle = new(_FullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE);

                if (fHandle.IsInvalid)
                {
                    Debug.WriteLine("UpdatePlaceholder FAILED: Invalid Handle!", System.Diagnostics.TraceLevel.Warning);
                    return new GenericResult(NtStatus.STATUS_CLOUD_FILE_IN_USE);
                }

                int fileIDSize = FileId.Length * Marshal.SizeOf(FileId[0]);
                long usn = 0;

                unsafe
                {
                    fixed (void* fileID = FileId)
                    {
                        CF_FILE_RANGE[] dehydrateRanges = null;
                        uint dehydrateRangesCount = 0;
                        if (markDataInvalid)
                        {
                            dehydrateRanges = new CF_FILE_RANGE[1];
                            dehydrateRanges[0] = new CF_FILE_RANGE() { StartingOffset = 0, Length = (long)FileSize };
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
                Styletronix.Debug.WriteLine("Reload Placeholder Data: " + FullPath, System.Diagnostics.TraceLevel.Verbose);
                using Kernel32.SafeSearchHandle findHandle = Kernel32.FindFirstFile(@"\\?\" + _FullPath, out WIN32_FIND_DATA findData);
                SetValuesByFindData(findData);
            }

            private void SetValuesByFindData(WIN32_FIND_DATA findData)
            {
                PlaceholderState = CfGetPlaceholderStateFromFindData(findData);
                Attributes = findData.dwFileAttributes;
                LastWriteTime = findData.ftLastWriteTime.ToDateTime();
                _PlaceholderInfoStandard = null;
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
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
    }
}
