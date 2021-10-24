using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.ComponentModel;

namespace Styletronix
{
    public partial class CloudFilterApi
    {
        public enum CF_IN_SYNC_STATE
        {
            CF_IN_SYNC_STATE_NOT_IN_SYNC = 0,
            CF_IN_SYNC_STATE_IN_SYNC = 1
        }
        [Flags]
        public enum CF_SET_IN_SYNC_FLAGS
        {
            CF_SET_IN_SYNC_FLAG_NONE = 0x0
        }
        [Flags]
        public enum CF_SET_PIN_FLAGS
        {
            //
            // Zusammenfassung:
            //     No pin flag.
            CF_SET_PIN_FLAG_NONE = 0x0,
            //
            // Zusammenfassung:
            //     The platform applies the pin state to the placeholder FileHandle and every file
            //     recursively beneath it (relevant only if FileHandle is a handle to a directory).
            CF_SET_PIN_FLAG_RECURSE = 0x1,
            //
            // Zusammenfassung:
            //     The platform applies the pin state to every file recursively beneath the placeholder
            //     FileHandle, but not to FileHandle itself.
            CF_SET_PIN_FLAG_RECURSE_ONLY = 0x2,
            //
            // Zusammenfassung:
            //     The platform will stop the recursion when encountering the first error; otherwise
            //     the platform skips the error and continues the recursion.
            CF_SET_PIN_FLAG_RECURSE_STOP_ON_ERROR = 0x4
        }
        public enum CF_PIN_STATE
        {
            /// <summary>The platform can decide freely when the placeholder’s content needs to present or absent locally on the disk.</summary>
            CF_PIN_STATE_UNSPECIFIED = 0,
            /// <summary>The sync provider will be notified to fetch the placeholder’s content asynchronously after the pin request is received by the platform. There is no guarantee that the placeholders to be pinned will be fully available locally after a <a href="https://docs.microsoft.com/windows/desktop/api/cfapi/nf-cfapi-cfsetpinstate">CfSetPinState</a> call completes successfully. However, the platform will fail any dehydration request on pinned placeholders.</summary>
            CF_PIN_STATE_PINNED = 1,
            /// <summary>The sync provider will be notified to dehydrate/invalidate the placeholder’s content on-disk asynchronously after the unpin request is received by the platform. There is no guarantee that the placeholders to be unpinned will be fully dehydrated after the API call completes successfully.</summary>
            CF_PIN_STATE_UNPINNED = 2,
            /// <summary>the placeholder will never be synced to the cloud by the sync provider. This state can only be set by the sync provider.</summary>
            CF_PIN_STATE_EXCLUDED = 3,
            /// <summary>The platform treats it as if the caller performs a move operation on the placeholder and hence re-evaluates the placeholder’s pin state based on its parent’s pin state. See the Remarks section for an inheritance table.</summary>
            CF_PIN_STATE_INHERIT = 4,
        }



        public class PInvoke
        {
            //
            // Zusammenfassung:
            //     Sets the in-sync state for a placeholder file or folder.
            //
            // Parameter:
            //   FileHandle:
            //     A handle to the placeholder. The caller must have WRITE_DATA or WRITE_DAC access
            //     to the placeholder.
            //
            //   InSyncState:
            //     The in-sync state. See CF_IN_SYNC_STATE for more details.
            //
            //   InSyncFlags:
            //     The in-sync state flags. See CF_SET_IN_SYNC_FLAGS for more details.
            //
            //   InSyncUsn:
            //     When specified, this instructs the platform to only perform in-sync setting if
            //     the file still has the same USN value as the one passed in. Passing a pointer
            //     to a USN value of 0 on input is the same as passing a NULL pointer. On return,
            //     this is the final USN value after setting the in-sync state.
            //
            // Rückgabewerte:
            //     If this function succeeds, it returns S_OK. Otherwise, it returns an HRESULT
            //     error code.
            [DllImport("cldapi.dll", ExactSpelling = true)]
            public static extern HRESULT CfSetInSyncState(HFILE FileHandle, CF_IN_SYNC_STATE InSyncState, CF_SET_IN_SYNC_FLAGS InSyncFlags, ref long InSyncUsn);

            //
            // Zusammenfassung:
            //     Sets the in-sync state for a placeholder file or folder.
            //
            // Parameter:
            //   FileHandle:
            //     A handle to the placeholder. The caller must have WRITE_DATA or WRITE_DAC access
            //     to the placeholder.
            //
            //   InSyncState:
            //     The in-sync state. See CF_IN_SYNC_STATE for more details.
            //
            //   InSyncFlags:
            //     The in-sync state flags. See CF_SET_IN_SYNC_FLAGS for more details.
            //
            //   InSyncUsn:
            //     When specified, this instructs the platform to only perform in-sync setting if
            //     the file still has the same USN value as the one passed in. Passing a pointer
            //     to a USN value of 0 on input is the same as passing a NULL pointer. On return,
            //     this is the final USN value after setting the in-sync state.
            //
            // Rückgabewerte:
            //     If this function succeeds, it returns S_OK. Otherwise, it returns an HRESULT
            //     error code.
            [DllImport("cldapi.dll", ExactSpelling = true)]
            public static extern HRESULT CfSetInSyncState(HFILE FileHandle, CF_IN_SYNC_STATE InSyncState, CF_SET_IN_SYNC_FLAGS InSyncFlags, [Optional][In] IntPtr InSyncUsn);

            //
            // Zusammenfassung:
            //     This sets the pin state of a placeholder, used to represent a user’s intent.
            //     Any application (not just the sync provider) can call this function.
            //
            // Parameter:
            //   FileHandle:
            //     The handle of the placeholder file. The caller must have READ_DATA or WRITE_DAC
            //     access to the placeholder.
            //
            //   PinState:
            //     The pin state of the placeholder file.
            //
            //   PinFlags:
            //     The pin state flags.
            //
            //   Overlapped:
            //     Allows the call to be performed asynchronously. See the Remarks section for more
            //     details.
            //
            // Rückgabewerte:
            //     If this function succeeds, it returns S_OK. Otherwise, it returns an HRESULT
            //     error code.
            //
            // Hinweise:
            //     When specified and combined with an asynchronous FileHandle, Overlapped allows
            //     the platform to perform the call asynchronously.
            //     The caller must have initialized the overlapped structure with an event to wait
            //     on. If this returns HRESULT_FROM_WIN32(ERROR_IO_PENDING), the caller can then
            //     wait using GetOverlappedResult. If not specified, the platform will perform the
            //     API call synchronously, regardless of how the handle was created.
            [DllImport("cldapi.dll", ExactSpelling = true)]
            public static extern HRESULT CfSetPinState(HFILE FileHandle, CF_PIN_STATE PinState, CF_SET_PIN_FLAGS PinFlags, [Optional][In][Out] IntPtr Overlapped);

            //
            // Zusammenfassung:
            //     This sets the pin state of a placeholder, used to represent a user’s intent.
            //     Any application (not just the sync provider) can call this function.
            //
            // Parameter:
            //   FileHandle:
            //     The handle of the placeholder file. The caller must have READ_DATA or WRITE_DAC
            //     access to the placeholder.
            //
            //   PinState:
            //     The pin state of the placeholder file.
            //
            //   PinFlags:
            //     The pin state flags.
            //
            //   Overlapped:
            //     Allows the call to be performed asynchronously. See the Remarks section for more
            //     details.
            //
            // Rückgabewerte:
            //     If this function succeeds, it returns S_OK. Otherwise, it returns an HRESULT
            //     error code.
            //
            // Hinweise:
            //     When specified and combined with an asynchronous FileHandle, Overlapped allows
            //     the platform to perform the call asynchronously.
            //     The caller must have initialized the overlapped structure with an event to wait
            //     on. If this returns HRESULT_FROM_WIN32(ERROR_IO_PENDING), the caller can then
            //     wait using GetOverlappedResult. If not specified, the platform will perform the
            //     API call synchronously, regardless of how the handle was created.
            [DllImport("cldapi.dll", ExactSpelling = true)]
            public unsafe static extern HRESULT CfSetPinState(HFILE FileHandle, CF_PIN_STATE PinState, CF_SET_PIN_FLAGS PinFlags, [In][Out] NativeOverlapped* Overlapped);

            //
            // Zusammenfassung:
            //     Formal replacement for the Windows HRESULT definition. In windows.h, it is a
            //     defined UINT value. For .NET, this class strongly types the value.
            //     The 32-bit value is organized as follows:
            //     Bit – 31 30 29 28 27 26 - 16 15 - 0
            //     Field – Severity Severity Customer NT status MsgID Facility Code
            [TypeConverter(typeof(HRESULTTypeConverter))]
            public struct HRESULT : IComparable, IComparable<HRESULT>, IEquatable<HRESULT>, IEquatable<int>, IEquatable<uint>, IConvertible, IErrorProvider
            {
                //
                // Zusammenfassung:
                //     Enumeration of facility codes
                public enum FacilityCode
                {
                    //
                    // Zusammenfassung:
                    //     The default facility code.
                    FACILITY_NULL = 0,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is an RPC subsystem.
                    FACILITY_RPC = 1,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is a COM Dispatch.
                    FACILITY_DISPATCH = 2,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is OLE Storage.
                    FACILITY_STORAGE = 3,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is COM/OLE Interface management.
                    FACILITY_ITF = 4,
                    //
                    // Zusammenfassung:
                    //     This region is reserved to map undecorated error codes into HRESULTs.
                    FACILITY_WIN32 = 7,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the Windows subsystem.
                    FACILITY_WINDOWS = 8,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the Security API layer.
                    FACILITY_SECURITY = 9,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the Security API layer.
                    FACILITY_SSPI = 9,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the control mechanism.
                    FACILITY_CONTROL = 10,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is a certificate client or server?
                    FACILITY_CERT = 11,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is Wininet related.
                    FACILITY_INTERNET = 12,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the Windows Media Server.
                    FACILITY_MEDIASERVER = 13,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the Microsoft Message Queue.
                    FACILITY_MSMQ = 14,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the Setup API.
                    FACILITY_SETUPAPI = 0xF,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the Smart-card subsystem.
                    FACILITY_SCARD = 0x10,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is COM+.
                    FACILITY_COMPLUS = 17,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the Microsoft agent.
                    FACILITY_AAF = 18,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is .NET CLR.
                    FACILITY_URT = 19,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the audit collection service.
                    FACILITY_ACS = 20,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is Direct Play.
                    FACILITY_DPLAY = 21,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the ubiquitous memoryintrospection service.
                    FACILITY_UMI = 22,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is Side-by-side servicing.
                    FACILITY_SXS = 23,
                    //
                    // Zusammenfassung:
                    //     The error code is specific to Windows CE.
                    FACILITY_WINDOWS_CE = 24,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is HTTP support.
                    FACILITY_HTTP = 25,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is common Logging support.
                    FACILITY_USERMODE_COMMONLOG = 26,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the user mode filter manager.
                    FACILITY_USERMODE_FILTER_MANAGER = 0x1F,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is background copy control
                    FACILITY_BACKGROUNDCOPY = 0x20,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is configuration services.
                    FACILITY_CONFIGURATION = 33,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is state management services.
                    FACILITY_STATE_MANAGEMENT = 34,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the Microsoft Identity Server.
                    FACILITY_METADIRECTORY = 35,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is a Windows update.
                    FACILITY_WINDOWSUPDATE = 36,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is Active Directory.
                    FACILITY_DIRECTORYSERVICE = 37,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the graphics drivers.
                    FACILITY_GRAPHICS = 38,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the user Shell.
                    FACILITY_SHELL = 39,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the Trusted Platform Module services.
                    FACILITY_TPM_SERVICES = 40,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the Trusted Platform Module applications.
                    FACILITY_TPM_SOFTWARE = 41,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is Performance Logs and Alerts
                    FACILITY_PLA = 48,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is Full volume encryption.
                    FACILITY_FVE = 49,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the Firewall Platform.
                    FACILITY_FWP = 50,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the Windows Resource Manager.
                    FACILITY_WINRM = 51,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the Network Driver Interface.
                    FACILITY_NDIS = 52,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the Usermode Hypervisor components.
                    FACILITY_USERMODE_HYPERVISOR = 53,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the Configuration Management Infrastructure.
                    FACILITY_CMI = 54,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the user mode virtualization subsystem.
                    FACILITY_USERMODE_VIRTUALIZATION = 55,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the user mode volume manager
                    FACILITY_USERMODE_VOLMGR = 56,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the Boot Configuration Database.
                    FACILITY_BCD = 57,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is user mode virtual hard disk support.
                    FACILITY_USERMODE_VHD = 58,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is System Diagnostics.
                    FACILITY_SDIAG = 60,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the Web Services.
                    FACILITY_WEBSERVICES = 61,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is a Windows Defender component.
                    FACILITY_WINDOWS_DEFENDER = 80,
                    //
                    // Zusammenfassung:
                    //     The source of the error code is the open connectivity service.
                    FACILITY_OPC = 81,
                    FACILITY_XPS = 82,
                    FACILITY_MBN = 84,
                    FACILITY_POWERSHELL = 84,
                    FACILITY_RAS = 83,
                    FACILITY_P2P_INT = 98,
                    FACILITY_P2P = 99,
                    FACILITY_DAF = 100,
                    FACILITY_BLUETOOTH_ATT = 101,
                    FACILITY_AUDIO = 102,
                    FACILITY_STATEREPOSITORY = 103,
                    FACILITY_VISUALCPP = 109,
                    FACILITY_SCRIPT = 112,
                    FACILITY_PARSE = 113,
                    FACILITY_BLB = 120,
                    FACILITY_BLB_CLI = 121,
                    FACILITY_WSBAPP = 122,
                    FACILITY_BLBUI = 0x80,
                    FACILITY_USN = 129,
                    FACILITY_USERMODE_VOLSNAP = 130,
                    FACILITY_TIERING = 131,
                    FACILITY_WSB_ONLINE = 133,
                    FACILITY_ONLINE_ID = 134,
                    FACILITY_DEVICE_UPDATE_AGENT = 135,
                    FACILITY_DRVSERVICING = 136,
                    FACILITY_DLS = 153,
                    FACILITY_DELIVERY_OPTIMIZATION = 208,
                    FACILITY_USERMODE_SPACES = 231,
                    FACILITY_USER_MODE_SECURITY_CORE = 232,
                    FACILITY_USERMODE_LICENSING = 234,
                    FACILITY_SOS = 160,
                    FACILITY_DEBUGGERS = 176,
                    FACILITY_SPP = 0x100,
                    FACILITY_RESTORE = 0x100,
                    FACILITY_DMSERVER = 0x100,
                    FACILITY_DEPLOYMENT_SERVICES_SERVER = 257,
                    FACILITY_DEPLOYMENT_SERVICES_IMAGING = 258,
                    FACILITY_DEPLOYMENT_SERVICES_MANAGEMENT = 259,
                    FACILITY_DEPLOYMENT_SERVICES_UTIL = 260,
                    FACILITY_DEPLOYMENT_SERVICES_BINLSVC = 261,
                    FACILITY_DEPLOYMENT_SERVICES_PXE = 263,
                    FACILITY_DEPLOYMENT_SERVICES_TFTP = 264,
                    FACILITY_DEPLOYMENT_SERVICES_TRANSPORT_MANAGEMENT = 272,
                    FACILITY_DEPLOYMENT_SERVICES_DRIVER_PROVISIONING = 278,
                    FACILITY_DEPLOYMENT_SERVICES_MULTICAST_SERVER = 289,
                    FACILITY_DEPLOYMENT_SERVICES_MULTICAST_CLIENT = 290,
                    FACILITY_DEPLOYMENT_SERVICES_CONTENT_PROVIDER = 293,
                    FACILITY_LINGUISTIC_SERVICES = 305,
                    FACILITY_AUDIOSTREAMING = 1094,
                    FACILITY_ACCELERATOR = 1536,
                    FACILITY_WMAAECMA = 1996,
                    FACILITY_DIRECTMUSIC = 2168,
                    FACILITY_DIRECT3D10 = 2169,
                    FACILITY_DXGI = 2170,
                    FACILITY_DXGI_DDI = 2171,
                    FACILITY_DIRECT3D11 = 2172,
                    FACILITY_DIRECT3D11_DEBUG = 2173,
                    FACILITY_DIRECT3D12 = 2174,
                    FACILITY_DIRECT3D12_DEBUG = 2175,
                    FACILITY_LEAP = 2184,
                    FACILITY_AUDCLNT = 2185,
                    FACILITY_WINCODEC_DWRITE_DWM = 2200,
                    FACILITY_WINML = 2192,
                    FACILITY_DIRECT2D = 2201,
                    FACILITY_DEFRAG = 2304,
                    FACILITY_USERMODE_SDBUS = 2305,
                    FACILITY_JSCRIPT = 2306,
                    FACILITY_PIDGENX = 2561,
                    FACILITY_EAS = 85,
                    FACILITY_WEB = 885,
                    FACILITY_WEB_SOCKET = 886,
                    FACILITY_MOBILE = 1793,
                    FACILITY_SQLITE = 1967,
                    FACILITY_UTC = 1989,
                    FACILITY_WEP = 2049,
                    FACILITY_SYNCENGINE = 2050,
                    FACILITY_XBOX = 2339,
                    FACILITY_GAME = 2340,
                    FACILITY_PIX = 2748
                }

                //
                // Zusammenfassung:
                //     A value indicating whether an Vanara.PInvoke.HRESULT is a success (Severity bit
                //     31 equals 0).
                public enum SeverityLevel
                {
                    //
                    // Zusammenfassung:
                    //     Success
                    Success,
                    //
                    // Zusammenfassung:
                    //     Failure
                    Fail
                }

                internal readonly int _value;

                private const int codeMask = 65535;

                private const uint facilityMask = 134152192u;

                private const int facilityShift = 16;

                private const uint severityMask = 2147483648u;

                private const int severityShift = 31;

                //
                // Zusammenfassung:
                //     Success
                public const int S_OK = 0;

                //
                // Zusammenfassung:
                //     False
                public const int S_FALSE = 1;

                public const int COR_E_OBJECTDISPOSED = -2146232798;

                public const int DESTS_E_NO_MATCHING_ASSOC_HANDLER = -2147217661;

                public const int SCRIPT_E_REPORTED = -2147352319;

                public const int WC_E_GREATERTHAN = -1072894429;

                public const int WC_E_SYNTAX = -1072894419;

                //
                // Zusammenfassung:
                //     The underlying file was converted to compound file format.
                public const int STG_S_CONVERTED = 197120;

                //
                // Zusammenfassung:
                //     The storage operation should block until more data is available.
                public const int STG_S_BLOCK = 197121;

                //
                // Zusammenfassung:
                //     The storage operation should retry immediately.
                public const int STG_S_RETRYNOW = 197122;

                //
                // Zusammenfassung:
                //     The notified event sink will not influence the storage operation.
                public const int STG_S_MONITORING = 197123;

                //
                // Zusammenfassung:
                //     Multiple opens prevent consolidated (commit succeeded).
                public const int STG_S_MULTIPLEOPENS = 197124;

                //
                // Zusammenfassung:
                //     Consolidation of the storage file failed (commit succeeded).
                public const int STG_S_CONSOLIDATIONFAILED = 197125;

                //
                // Zusammenfassung:
                //     Consolidation of the storage file is inappropriate (commit succeeded).
                public const int STG_S_CANNOTCONSOLIDATE = 197126;

                //
                // Zusammenfassung:
                //     Use the registry database to provide the requested information.
                public const int OLE_S_USEREG = 262144;

                //
                // Zusammenfassung:
                //     Success, but static.
                public const int OLE_S_STATIC = 262145;

                //
                // Zusammenfassung:
                //     Macintosh clipboard format.
                public const int OLE_S_MAC_CLIPFORMAT = 262146;

                //
                // Zusammenfassung:
                //     Successful drop took place.
                public const int DRAGDROP_S_DROP = 262400;

                //
                // Zusammenfassung:
                //     Drag-drop operation canceled.
                public const int DRAGDROP_S_CANCEL = 262401;

                //
                // Zusammenfassung:
                //     Use the default cursor.
                public const int DRAGDROP_S_USEDEFAULTCURSORS = 262402;

                //
                // Zusammenfassung:
                //     Data has same FORMATETC.
                public const int DATA_S_SAMEFORMATETC = 262448;

                //
                // Zusammenfassung:
                //     View is already frozen.
                public const int VIEW_S_ALREADY_FROZEN = 262464;

                //
                // Zusammenfassung:
                //     FORMATETC not supported.
                public const int CACHE_S_FORMATETC_NOTSUPPORTED = 262512;

                //
                // Zusammenfassung:
                //     Same cache.
                public const int CACHE_S_SAMECACHE = 262513;

                //
                // Zusammenfassung:
                //     Some caches are not updated.
                public const int CACHE_S_SOMECACHES_NOTUPDATED = 262514;

                //
                // Zusammenfassung:
                //     Invalid verb for OLE object.
                public const int OLEOBJ_S_INVALIDVERB = 262528;

                //
                // Zusammenfassung:
                //     Verb number is valid but verb cannot be done now.
                public const int OLEOBJ_S_CANNOT_DOVERB_NOW = 262529;

                //
                // Zusammenfassung:
                //     Invalid window handle passed.
                public const int OLEOBJ_S_INVALIDHWND = 262530;

                //
                // Zusammenfassung:
                //     Message is too long; some of it had to be truncated before displaying.
                public const int INPLACE_S_TRUNCATED = 262560;

                //
                // Zusammenfassung:
                //     Unable to convert OLESTREAM to IStorage.
                public const int CONVERT10_S_NO_PRESENTATION = 262592;

                //
                // Zusammenfassung:
                //     Moniker reduced to itself.
                public const int MK_S_REDUCED_TO_SELF = 262626;

                //
                // Zusammenfassung:
                //     Common prefix is this moniker.
                public const int MK_S_ME = 262628;

                //
                // Zusammenfassung:
                //     Common prefix is input moniker.
                public const int MK_S_HIM = 262629;

                //
                // Zusammenfassung:
                //     Common prefix is both monikers.
                public const int MK_S_US = 262630;

                //
                // Zusammenfassung:
                //     Moniker is already registered in running object table.
                public const int MK_S_MONIKERALREADYREGISTERED = 262631;

                //
                // Zusammenfassung:
                //     An event was able to invoke some, but not all, of the subscribers.
                public const int EVENT_S_SOME_SUBSCRIBERS_FAILED = 262656;

                //
                // Zusammenfassung:
                //     An event was delivered, but there were no subscribers.
                public const int EVENT_S_NOSUBSCRIBERS = 262658;

                //
                // Zusammenfassung:
                //     The task is ready to run at its next scheduled time.
                public const int SCHED_S_TASK_READY = 267008;

                //
                // Zusammenfassung:
                //     The task is currently running.
                public const int SCHED_S_TASK_RUNNING = 267009;

                //
                // Zusammenfassung:
                //     The task will not run at the scheduled times because it has been disabled.
                public const int SCHED_S_TASK_DISABLED = 267010;

                //
                // Zusammenfassung:
                //     The task has not yet run.
                public const int SCHED_S_TASK_HAS_NOT_RUN = 267011;

                //
                // Zusammenfassung:
                //     There are no more runs scheduled for this task.
                public const int SCHED_S_TASK_NO_MORE_RUNS = 267012;

                //
                // Zusammenfassung:
                //     One or more of the properties that are needed to run this task on a schedule
                //     have not been set.
                public const int SCHED_S_TASK_NOT_SCHEDULED = 267013;

                //
                // Zusammenfassung:
                //     The last run of the task was terminated by the user.
                public const int SCHED_S_TASK_TERMINATED = 267014;

                //
                // Zusammenfassung:
                //     Either the task has no triggers, or the existing triggers are disabled or not
                //     set.
                public const int SCHED_S_TASK_NO_VALID_TRIGGERS = 267015;

                //
                // Zusammenfassung:
                //     Event triggers do not have set run times.
                public const int SCHED_S_EVENT_TRIGGER = 267016;

                //
                // Zusammenfassung:
                //     The task is registered, but not all specified triggers will start the task.
                public const int SCHED_S_SOME_TRIGGERS_FAILED = 267035;

                //
                // Zusammenfassung:
                //     The task is registered, but it might fail to start. Batch logon privilege needs
                //     to be enabled for the task principal.
                public const int SCHED_S_BATCH_LOGON_PROBLEM = 267036;

                //
                // Zusammenfassung:
                //     An asynchronous operation was specified. The operation has begun, but its outcome
                //     is not known yet.
                public const int XACT_S_ASYNC = 315392;

                //
                // Zusammenfassung:
                //     The method call succeeded because the transaction was read-only.
                public const int XACT_S_READONLY = 315394;

                //
                // Zusammenfassung:
                //     The transaction was successfully aborted. However, this is a coordinated transaction,
                //     and a number of enlisted resources were aborted outright because they could not
                //     support abort-retaining semantics.
                public const int XACT_S_SOMENORETAIN = 315395;

                //
                // Zusammenfassung:
                //     No changes were made during this call, but the sink wants another chance to look
                //     if any other sinks make further changes.
                public const int XACT_S_OKINFORM = 315396;

                //
                // Zusammenfassung:
                //     The sink is content and wants the transaction to proceed. Changes were made to
                //     one or more resources during this call.
                public const int XACT_S_MADECHANGESCONTENT = 315397;

                //
                // Zusammenfassung:
                //     The sink is for the moment and wants the transaction to proceed, but if other
                //     changes are made following this return by other event sinks, this sink wants
                //     another chance to look.
                public const int XACT_S_MADECHANGESINFORM = 315398;

                //
                // Zusammenfassung:
                //     The transaction was successfully aborted. However, the abort was nonretaining.
                public const int XACT_S_ALLNORETAIN = 315399;

                //
                // Zusammenfassung:
                //     An abort operation was already in progress.
                public const int XACT_S_ABORTING = 315400;

                //
                // Zusammenfassung:
                //     The resource manager has performed a single-phase commit of the transaction.
                public const int XACT_S_SINGLEPHASE = 315401;

                //
                // Zusammenfassung:
                //     The local transaction has not aborted.
                public const int XACT_S_LOCALLY_OK = 315402;

                //
                // Zusammenfassung:
                //     The resource manager has requested to be the coordinator (last resource manager)
                //     for the transaction.
                public const int XACT_S_LASTRESOURCEMANAGER = 315408;

                //
                // Zusammenfassung:
                //     Not all the requested interfaces were available.
                public const int CO_S_NOTALLINTERFACES = 524306;

                //
                // Zusammenfassung:
                //     The specified machine name was not found in the cache.
                public const int CO_S_MACHINENAMENOTFOUND = 524307;

                //
                // Zusammenfassung:
                //     The function completed successfully, but it must be called again to complete
                //     the context.
                public const int SEC_I_CONTINUE_NEEDED = 590610;

                //
                // Zusammenfassung:
                //     The function completed successfully, but CompleteToken must be called.
                public const int SEC_I_COMPLETE_NEEDED = 590611;

                //
                // Zusammenfassung:
                //     The function completed successfully, but both CompleteToken and this function
                //     must be called to complete the context.
                public const int SEC_I_COMPLETE_AND_CONTINUE = 590612;

                //
                // Zusammenfassung:
                //     The logon was completed, but no network authority was available. The logon was
                //     made using locally known information.
                public const int SEC_I_LOCAL_LOGON = 590613;

                //
                // Zusammenfassung:
                //     The context has expired and can no longer be used.
                public const int SEC_I_CONTEXT_EXPIRED = 590615;

                //
                // Zusammenfassung:
                //     The credentials supplied were not complete and could not be verified. Additional
                //     information can be returned from the context.
                public const int SEC_I_INCOMPLETE_CREDENTIALS = 590624;

                //
                // Zusammenfassung:
                //     The context data must be renegotiated with the peer.
                public const int SEC_I_RENEGOTIATE = 590625;

                //
                // Zusammenfassung:
                //     There is no LSA mode context associated with this context.
                public const int SEC_I_NO_LSA_CONTEXT = 590627;

                //
                // Zusammenfassung:
                //     A signature operation must be performed before the user can authenticate.
                public const int SEC_I_SIGNATURE_NEEDED = 590684;

                //
                // Zusammenfassung:
                //     The protected data needs to be reprotected.
                public const int CRYPT_I_NEW_PROTECTION_REQUIRED = 593938;

                //
                // Zusammenfassung:
                //     The requested operation is pending completion.
                public const int NS_S_CALLPENDING = 851968;

                //
                // Zusammenfassung:
                //     The requested operation was aborted by the client.
                public const int NS_S_CALLABORTED = 851969;

                //
                // Zusammenfassung:
                //     The stream was purposefully stopped before completion.
                public const int NS_S_STREAM_TRUNCATED = 851970;

                //
                // Zusammenfassung:
                //     The requested operation has caused the source to rebuffer.
                public const int NS_S_REBUFFERING = 854984;

                //
                // Zusammenfassung:
                //     The requested operation has caused the source to degrade codec quality.
                public const int NS_S_DEGRADING_QUALITY = 854985;

                //
                // Zusammenfassung:
                //     The transcryptor object has reached end of file.
                public const int NS_S_TRANSCRYPTOR_EOF = 855003;

                //
                // Zusammenfassung:
                //     An upgrade is needed for the theme manager to correctly show this skin. Skin
                //     reports version: %.1f.
                public const int NS_S_WMP_UI_VERSIONMISMATCH = 856040;

                //
                // Zusammenfassung:
                //     An error occurred in one of the UI components.
                public const int NS_S_WMP_EXCEPTION = 856041;

                //
                // Zusammenfassung:
                //     Successfully loaded a GIF file.
                public const int NS_S_WMP_LOADED_GIF_IMAGE = 856128;

                //
                // Zusammenfassung:
                //     Successfully loaded a PNG file.
                public const int NS_S_WMP_LOADED_PNG_IMAGE = 856129;

                //
                // Zusammenfassung:
                //     Successfully loaded a BMP file.
                public const int NS_S_WMP_LOADED_BMP_IMAGE = 856130;

                //
                // Zusammenfassung:
                //     Successfully loaded a JPG file.
                public const int NS_S_WMP_LOADED_JPG_IMAGE = 856131;

                //
                // Zusammenfassung:
                //     Drop this frame.
                public const int NS_S_WMG_FORCE_DROP_FRAME = 856143;

                //
                // Zusammenfassung:
                //     The specified stream has already been rendered.
                public const int NS_S_WMR_ALREADYRENDERED = 856159;

                //
                // Zusammenfassung:
                //     The specified type partially matches this pin type.
                public const int NS_S_WMR_PINTYPEPARTIALMATCH = 856160;

                //
                // Zusammenfassung:
                //     The specified type fully matches this pin type.
                public const int NS_S_WMR_PINTYPEFULLMATCH = 856161;

                //
                // Zusammenfassung:
                //     The timestamp is late compared to the current render position. Advise dropping
                //     this frame.
                public const int NS_S_WMG_ADVISE_DROP_FRAME = 856166;

                //
                // Zusammenfassung:
                //     The timestamp is severely late compared to the current render position. Advise
                //     dropping everything up to the next key frame.
                public const int NS_S_WMG_ADVISE_DROP_TO_KEYFRAME = 856167;

                //
                // Zusammenfassung:
                //     No burn rights. You will be prompted to buy burn rights when you try to burn
                //     this file to an audio CD.
                public const int NS_S_NEED_TO_BUY_BURN_RIGHTS = 856283;

                //
                // Zusammenfassung:
                //     Failed to clear playlist because it was aborted by user.
                public const int NS_S_WMPCORE_PLAYLISTCLEARABORT = 856318;

                //
                // Zusammenfassung:
                //     Failed to remove item in the playlist since it was aborted by user.
                public const int NS_S_WMPCORE_PLAYLISTREMOVEITEMABORT = 856319;

                //
                // Zusammenfassung:
                //     Playlist is being generated asynchronously.
                public const int NS_S_WMPCORE_PLAYLIST_CREATION_PENDING = 856322;

                //
                // Zusammenfassung:
                //     Validation of the media is pending.
                public const int NS_S_WMPCORE_MEDIA_VALIDATION_PENDING = 856323;

                //
                // Zusammenfassung:
                //     Encountered more than one Repeat block during ASX processing.
                public const int NS_S_WMPCORE_PLAYLIST_REPEAT_SECONDARY_SEGMENTS_IGNORED = 856324;

                //
                // Zusammenfassung:
                //     Current state of WMP disallows calling this method or property.
                public const int NS_S_WMPCORE_COMMAND_NOT_AVAILABLE = 856325;

                //
                // Zusammenfassung:
                //     Name for the playlist has been auto generated.
                public const int NS_S_WMPCORE_PLAYLIST_NAME_AUTO_GENERATED = 856326;

                //
                // Zusammenfassung:
                //     The imported playlist does not contain all items from the original.
                public const int NS_S_WMPCORE_PLAYLIST_IMPORT_MISSING_ITEMS = 856327;

                //
                // Zusammenfassung:
                //     The M3U playlist has been ignored because it only contains one item.
                public const int NS_S_WMPCORE_PLAYLIST_COLLAPSED_TO_SINGLE_MEDIA = 856328;

                //
                // Zusammenfassung:
                //     The open for the child playlist associated with this media is pending.
                public const int NS_S_WMPCORE_MEDIA_CHILD_PLAYLIST_OPEN_PENDING = 856329;

                //
                // Zusammenfassung:
                //     More nodes support the interface requested, but the array for returning them
                //     is full.
                public const int NS_S_WMPCORE_MORE_NODES_AVAIABLE = 856330;

                //
                // Zusammenfassung:
                //     Backup or Restore successful!.
                public const int NS_S_WMPBR_SUCCESS = 856373;

                //
                // Zusammenfassung:
                //     Transfer complete with limitations.
                public const int NS_S_WMPBR_PARTIALSUCCESS = 856374;

                //
                // Zusammenfassung:
                //     Request to the effects control to change transparency status to transparent.
                public const int NS_S_WMPEFFECT_TRANSPARENT = 856388;

                //
                // Zusammenfassung:
                //     Request to the effects control to change transparency status to opaque.
                public const int NS_S_WMPEFFECT_OPAQUE = 856389;

                //
                // Zusammenfassung:
                //     The requested application pane is performing an operation and will not be released.
                public const int NS_S_OPERATION_PENDING = 856398;

                //
                // Zusammenfassung:
                //     The file is only available for purchase when you buy the entire album.
                public const int NS_S_TRACK_BUY_REQUIRES_ALBUM_PURCHASE = 856921;

                //
                // Zusammenfassung:
                //     There were problems completing the requested navigation. There are identifiers
                //     missing in the catalog.
                public const int NS_S_NAVIGATION_COMPLETE_WITH_ERRORS = 856926;

                //
                // Zusammenfassung:
                //     Track already downloaded.
                public const int NS_S_TRACK_ALREADY_DOWNLOADED = 856929;

                //
                // Zusammenfassung:
                //     The publishing point successfully started, but one or more of the requested data
                //     writer plug-ins failed.
                public const int NS_S_PUBLISHING_POINT_STARTED_WITH_FAILED_SINKS = 857369;

                //
                // Zusammenfassung:
                //     Status message: The license was acquired.
                public const int NS_S_DRM_LICENSE_ACQUIRED = 861990;

                //
                // Zusammenfassung:
                //     Status message: The security upgrade has been completed.
                public const int NS_S_DRM_INDIVIDUALIZED = 861991;

                //
                // Zusammenfassung:
                //     Status message: License monitoring has been canceled.
                public const int NS_S_DRM_MONITOR_CANCELLED = 862022;

                //
                // Zusammenfassung:
                //     Status message: License acquisition has been canceled.
                public const int NS_S_DRM_ACQUIRE_CANCELLED = 862023;

                //
                // Zusammenfassung:
                //     The track is burnable and had no playlist burn limit.
                public const int NS_S_DRM_BURNABLE_TRACK = 862062;

                //
                // Zusammenfassung:
                //     The track is burnable but has a playlist burn limit.
                public const int NS_S_DRM_BURNABLE_TRACK_WITH_PLAYLIST_RESTRICTION = 862063;

                //
                // Zusammenfassung:
                //     A security upgrade is required to perform the operation on this media file.
                public const int NS_S_DRM_NEEDS_INDIVIDUALIZATION = 862174;

                //
                // Zusammenfassung:
                //     Installation was successful; however, some file cleanup is not complete. For
                //     best results, restart your computer.
                public const int NS_S_REBOOT_RECOMMENDED = 862968;

                //
                // Zusammenfassung:
                //     Installation was successful; however, some file cleanup is not complete. To continue,
                //     you must restart your computer.
                public const int NS_S_REBOOT_REQUIRED = 862969;

                //
                // Zusammenfassung:
                //     EOS hit during rewinding.
                public const int NS_S_EOSRECEDING = 864009;

                //
                // Zusammenfassung:
                //     Internal.
                public const int NS_S_CHANGENOTICE = 864013;

                //
                // Zusammenfassung:
                //     The IO was completed by a filter.
                public const int ERROR_FLT_IO_COMPLETE = 2031617;

                //
                // Zusammenfassung:
                //     No mode is pinned on the specified VidPN source or target.
                public const int ERROR_GRAPHICS_MODE_NOT_PINNED = 2499335;

                //
                // Zusammenfassung:
                //     Specified mode set does not specify preference for one of its modes.
                public const int ERROR_GRAPHICS_NO_PREFERRED_MODE = 2499358;

                //
                // Zusammenfassung:
                //     Specified data set (for example, mode set, frequency range set, descriptor set,
                //     and topology) is empty.
                public const int ERROR_GRAPHICS_DATASET_IS_EMPTY = 2499403;

                //
                // Zusammenfassung:
                //     Specified data set (for example, mode set, frequency range set, descriptor set,
                //     and topology) does not contain any more elements.
                public const int ERROR_GRAPHICS_NO_MORE_ELEMENTS_IN_DATASET = 2499404;

                //
                // Zusammenfassung:
                //     Specified content transformation is not pinned on the specified VidPN present
                //     path.
                public const int ERROR_GRAPHICS_PATH_CONTENT_GEOMETRY_TRANSFORMATION_NOT_PINNED = 2499409;

                //
                // Zusammenfassung:
                //     Property value will be ignored.
                public const int PLA_S_PROPERTY_IGNORED = 3145984;

                //
                // Zusammenfassung:
                //     The request will be completed later by a Network Driver Interface Specification
                //     (NDIS) status indication.
                public const int ERROR_NDIS_INDICATION_REQUIRED = 3407873;

                //
                // Zusammenfassung:
                //     The VolumeSequenceNumber of a MOVE_NOTIFICATION request is incorrect.
                public const int TRK_S_OUT_OF_SYNC = 233492736;

                //
                // Zusammenfassung:
                //     The VolumeID in a request was not found in the server's ServerVolumeTable.
                public const int TRK_VOLUME_NOT_FOUND = 233492738;

                //
                // Zusammenfassung:
                //     A notification was sent to the LnkSvrMessage method, but the RequestMachine for
                //     the request was not the VolumeOwner for a VolumeID in the request.
                public const int TRK_VOLUME_NOT_OWNED = 233492739;

                //
                // Zusammenfassung:
                //     The server received a MOVE_NOTIFICATION request, but the FileTable size limit
                //     has already been reached.
                public const int TRK_S_NOTIFICATION_QUOTA_EXCEEDED = 233492743;

                //
                // Zusammenfassung:
                //     The Title Server %1 is running.
                public const int NS_I_TIGER_START = 1074593871;

                //
                // Zusammenfassung:
                //     Content Server %1 (%2) is starting.
                public const int NS_I_CUB_START = 1074593873;

                //
                // Zusammenfassung:
                //     Content Server %1 (%2) is running.
                public const int NS_I_CUB_RUNNING = 1074593874;

                //
                // Zusammenfassung:
                //     Disk %1 ( %2 ) on Content Server %3, is running.
                public const int NS_I_DISK_START = 1074593876;

                //
                // Zusammenfassung:
                //     Started rebuilding disk %1 ( %2 ) on Content Server %3.
                public const int NS_I_DISK_REBUILD_STARTED = 1074593878;

                //
                // Zusammenfassung:
                //     Finished rebuilding disk %1 ( %2 ) on Content Server %3.
                public const int NS_I_DISK_REBUILD_FINISHED = 1074593879;

                //
                // Zusammenfassung:
                //     Aborted rebuilding disk %1 ( %2 ) on Content Server %3.
                public const int NS_I_DISK_REBUILD_ABORTED = 1074593880;

                //
                // Zusammenfassung:
                //     A NetShow administrator at network location %1 set the data stream limit to %2
                //     streams.
                public const int NS_I_LIMIT_FUNNELS = 1074593881;

                //
                // Zusammenfassung:
                //     A NetShow administrator at network location %1 started disk %2.
                public const int NS_I_START_DISK = 1074593882;

                //
                // Zusammenfassung:
                //     A NetShow administrator at network location %1 stopped disk %2.
                public const int NS_I_STOP_DISK = 1074593883;

                //
                // Zusammenfassung:
                //     A NetShow administrator at network location %1 stopped Content Server %2.
                public const int NS_I_STOP_CUB = 1074593884;

                //
                // Zusammenfassung:
                //     A NetShow administrator at network location %1 aborted user session %2 from the
                //     system.
                public const int NS_I_KILL_USERSESSION = 1074593885;

                //
                // Zusammenfassung:
                //     A NetShow administrator at network location %1 aborted obsolete connection %2
                //     from the system.
                public const int NS_I_KILL_CONNECTION = 1074593886;

                //
                // Zusammenfassung:
                //     A NetShow administrator at network location %1 started rebuilding disk %2.
                public const int NS_I_REBUILD_DISK = 1074593887;

                //
                // Zusammenfassung:
                //     Event initialization failed, there will be no MCM events.
                public const int MCMADM_I_NO_EVENTS = 1074593897;

                //
                // Zusammenfassung:
                //     The logging operation failed.
                public const int NS_I_LOGGING_FAILED = 1074593902;

                //
                // Zusammenfassung:
                //     A NetShow administrator at network location %1 set the maximum bandwidth limit
                //     to %2 bps.
                public const int NS_I_LIMIT_BANDWIDTH = 1074593904;

                //
                // Zusammenfassung:
                //     Content Server %1 (%2) has established its link to Content Server %3.
                public const int NS_I_CUB_UNFAIL_LINK = 1074594193;

                //
                // Zusammenfassung:
                //     Restripe operation has started.
                public const int NS_I_RESTRIPE_START = 1074594195;

                //
                // Zusammenfassung:
                //     Restripe operation has completed.
                public const int NS_I_RESTRIPE_DONE = 1074594196;

                //
                // Zusammenfassung:
                //     Content disk %1 (%2) on Content Server %3 has been restriped out.
                public const int NS_I_RESTRIPE_DISK_OUT = 1074594198;

                //
                // Zusammenfassung:
                //     Content server %1 (%2) has been restriped out.
                public const int NS_I_RESTRIPE_CUB_OUT = 1074594199;

                //
                // Zusammenfassung:
                //     Disk %1 ( %2 ) on Content Server %3, has been offlined.
                public const int NS_I_DISK_STOP = 1074594200;

                //
                // Zusammenfassung:
                //     The playlist change occurred while receding.
                public const int NS_I_PLAYLIST_CHANGE_RECEDING = 1074599102;

                //
                // Zusammenfassung:
                //     The client is reconnected.
                public const int NS_I_RECONNECTED = 1074605823;

                //
                // Zusammenfassung:
                //     Forcing a switch to a pending header on start.
                public const int NS_I_NOLOG_STOP = 1074605825;

                //
                // Zusammenfassung:
                //     There is already an existing packetizer plugin for the stream.
                public const int NS_I_EXISTING_PACKETIZER = 1074605827;

                //
                // Zusammenfassung:
                //     The proxy setting is manual.
                public const int NS_I_MANUAL_PROXY = 1074605828;

                //
                // Zusammenfassung:
                //     The kernel driver detected a version mismatch between it and the user mode driver.
                public const int ERROR_GRAPHICS_DRIVER_MISMATCH = 1076240393;

                //
                // Zusammenfassung:
                //     Child device presence was not reliably detected.
                public const int ERROR_GRAPHICS_UNKNOWN_CHILD_STATUS = 1076241455;

                //
                // Zusammenfassung:
                //     Starting the lead-link adapter has been deferred temporarily.
                public const int ERROR_GRAPHICS_LEADLINK_START_DEFERRED = 1076241463;

                //
                // Zusammenfassung:
                //     The display adapter is being polled for children too frequently at the same polling
                //     level.
                public const int ERROR_GRAPHICS_POLLING_TOO_FREQUENTLY = 1076241465;

                //
                // Zusammenfassung:
                //     Starting the adapter has been deferred temporarily.
                public const int ERROR_GRAPHICS_START_DEFERRED = 1076241466;

                //
                // Zusammenfassung:
                //     The data necessary to complete this operation is not yet available.
                public const int E_PENDING = -2147483638;

                //
                // Zusammenfassung:
                //     The operation attempted to access data outside the valid range
                public const int E_BOUNDS = -2147483637;

                //
                // Zusammenfassung:
                //     A concurrent or interleaved operation changed the state of the object, invalidating
                //     this operation.
                public const int E_CHANGED_STATE = -2147483636;

                //
                // Zusammenfassung:
                //     An illegal state change was requested.
                public const int E_ILLEGAL_STATE_CHANGE = -2147483635;

                //
                // Zusammenfassung:
                //     A method was called at an unexpected time.
                public const int E_ILLEGAL_METHOD_CALL = -2147483634;

                //
                // Zusammenfassung:
                //     Typename or Namespace was not found in metadata file.
                public const int RO_E_METADATA_NAME_NOT_FOUND = -2147483633;

                //
                // Zusammenfassung:
                //     Name is an existing namespace rather than a typename.
                public const int RO_E_METADATA_NAME_IS_NAMESPACE = -2147483632;

                //
                // Zusammenfassung:
                //     Typename has an invalid format.
                public const int RO_E_METADATA_INVALID_TYPE_FORMAT = -2147483631;

                //
                // Zusammenfassung:
                //     Metadata file is invalid or corrupted.
                public const int RO_E_INVALID_METADATA_FILE = -2147483630;

                //
                // Zusammenfassung:
                //     The object has been closed.
                public const int RO_E_CLOSED = -2147483629;

                //
                // Zusammenfassung:
                //     Only one thread may access the object during a write operation.
                public const int RO_E_EXCLUSIVE_WRITE = -2147483628;

                //
                // Zusammenfassung:
                //     Operation is prohibited during change notification.
                public const int RO_E_CHANGE_NOTIFICATION_IN_PROGRESS = -2147483627;

                //
                // Zusammenfassung:
                //     The text associated with this error code could not be found.
                public const int RO_E_ERROR_STRING_NOT_FOUND = -2147483626;

                //
                // Zusammenfassung:
                //     String not null terminated.
                public const int E_STRING_NOT_NULL_TERMINATED = -2147483625;

                //
                // Zusammenfassung:
                //     A delegate was assigned when not allowed.
                public const int E_ILLEGAL_DELEGATE_ASSIGNMENT = -2147483624;

                //
                // Zusammenfassung:
                //     An async operation was not properly started.
                public const int E_ASYNC_OPERATION_NOT_STARTED = -2147483623;

                //
                // Zusammenfassung:
                //     The application is exiting and cannot service this request.
                public const int E_APPLICATION_EXITING = -2147483622;

                //
                // Zusammenfassung:
                //     The application view is exiting and cannot service this request.
                public const int E_APPLICATION_VIEW_EXITING = -2147483621;

                //
                // Zusammenfassung:
                //     The object must support the IAgileObject interface.
                public const int RO_E_MUST_BE_AGILE = -2147483620;

                //
                // Zusammenfassung:
                //     Activating a single-threaded class from MTA is not supported.
                public const int RO_E_UNSUPPORTED_FROM_MTA = -2147483619;

                //
                // Zusammenfassung:
                //     The object has been committed.
                public const int RO_E_COMMITTED = -2147483618;

                //
                // Zusammenfassung:
                //     Not implemented.
                public const int E_NOTIMPL = -2147467263;

                //
                // Zusammenfassung:
                //     No such interface supported.
                public const int E_NOINTERFACE = -2147467262;

                //
                // Zusammenfassung:
                //     Invalid pointer.
                public const int E_POINTER = -2147467261;

                //
                // Zusammenfassung:
                //     Operation aborted.
                public const int E_ABORT = -2147467260;

                //
                // Zusammenfassung:
                //     Unspecified error.
                public const int E_FAIL = -2147467259;

                //
                // Zusammenfassung:
                //     Thread local storage failure.
                public const int CO_E_INIT_TLS = -2147467258;

                //
                // Zusammenfassung:
                //     Get shared memory allocator failure.
                public const int CO_E_INIT_SHARED_ALLOCATOR = -2147467257;

                //
                // Zusammenfassung:
                //     Get memory allocator failure.
                public const int CO_E_INIT_MEMORY_ALLOCATOR = -2147467256;

                //
                // Zusammenfassung:
                //     Unable to initialize class cache.
                public const int CO_E_INIT_CLASS_CACHE = -2147467255;

                //
                // Zusammenfassung:
                //     Unable to initialize remote procedure call (RPC) services.
                public const int CO_E_INIT_RPC_CHANNEL = -2147467254;

                //
                // Zusammenfassung:
                //     Cannot set thread local storage channel control.
                public const int CO_E_INIT_TLS_SET_CHANNEL_CONTROL = -2147467253;

                //
                // Zusammenfassung:
                //     Could not allocate thread local storage channel control.
                public const int CO_E_INIT_TLS_CHANNEL_CONTROL = -2147467252;

                //
                // Zusammenfassung:
                //     The user-supplied memory allocator is unacceptable.
                public const int CO_E_INIT_UNACCEPTED_USER_ALLOCATOR = -2147467251;

                //
                // Zusammenfassung:
                //     The OLE service mutex already exists.
                public const int CO_E_INIT_SCM_MUTEX_EXISTS = -2147467250;

                //
                // Zusammenfassung:
                //     The OLE service file mapping already exists.
                public const int CO_E_INIT_SCM_FILE_MAPPING_EXISTS = -2147467249;

                //
                // Zusammenfassung:
                //     Unable to map view of file for OLE service.
                public const int CO_E_INIT_SCM_MAP_VIEW_OF_FILE = -2147467248;

                //
                // Zusammenfassung:
                //     Failure attempting to launch OLE service.
                public const int CO_E_INIT_SCM_EXEC_FAILURE = -2147467247;

                //
                // Zusammenfassung:
                //     There was an attempt to call CoInitialize a second time while single-threaded.
                public const int CO_E_INIT_ONLY_SINGLE_THREADED = -2147467246;

                //
                // Zusammenfassung:
                //     A Remote activation was necessary but was not allowed.
                public const int CO_E_CANT_REMOTE = -2147467245;

                //
                // Zusammenfassung:
                //     A Remote activation was necessary, but the server name provided was invalid.
                public const int CO_E_BAD_SERVER_NAME = -2147467244;

                //
                // Zusammenfassung:
                //     The class is configured to run as a security ID different from the caller.
                public const int CO_E_WRONG_SERVER_IDENTITY = -2147467243;

                //
                // Zusammenfassung:
                //     Use of OLE1 services requiring Dynamic Data Exchange (DDE) Windows is disabled.
                public const int CO_E_OLE1DDE_DISABLED = -2147467242;

                //
                // Zusammenfassung:
                //     A RunAs specification must be <domain name>\<user name> or simply <user name>.
                public const int CO_E_RUNAS_SYNTAX = -2147467241;

                //
                // Zusammenfassung:
                //     The server process could not be started. The path name might be incorrect.
                public const int CO_E_CREATEPROCESS_FAILURE = -2147467240;

                //
                // Zusammenfassung:
                //     The server process could not be started as the configured identity. The path
                //     name might be incorrect or unavailable.
                public const int CO_E_RUNAS_CREATEPROCESS_FAILURE = -2147467239;

                //
                // Zusammenfassung:
                //     The server process could not be started because the configured identity is incorrect.
                //     Check the user name and password.
                public const int CO_E_RUNAS_LOGON_FAILURE = -2147467238;

                //
                // Zusammenfassung:
                //     The client is not allowed to launch this server.
                public const int CO_E_LAUNCH_PERMSSION_DENIED = -2147467237;

                //
                // Zusammenfassung:
                //     The service providing this server could not be started.
                public const int CO_E_START_SERVICE_FAILURE = -2147467236;

                //
                // Zusammenfassung:
                //     This computer was unable to communicate with the computer providing the server.
                public const int CO_E_REMOTE_COMMUNICATION_FAILURE = -2147467235;

                //
                // Zusammenfassung:
                //     The server did not respond after being launched.
                public const int CO_E_SERVER_START_TIMEOUT = -2147467234;

                //
                // Zusammenfassung:
                //     The registration information for this server is inconsistent or incomplete.
                public const int CO_E_CLSREG_INCONSISTENT = -2147467233;

                //
                // Zusammenfassung:
                //     The registration information for this interface is inconsistent or incomplete.
                public const int CO_E_IIDREG_INCONSISTENT = -2147467232;

                //
                // Zusammenfassung:
                //     The operation attempted is not supported.
                public const int CO_E_NOT_SUPPORTED = -2147467231;

                //
                // Zusammenfassung:
                //     A DLL must be loaded.
                public const int CO_E_RELOAD_DLL = -2147467230;

                //
                // Zusammenfassung:
                //     A Microsoft Software Installer error was encountered.
                public const int CO_E_MSI_ERROR = -2147467229;

                //
                // Zusammenfassung:
                //     The specified activation could not occur in the client context as specified.
                public const int CO_E_ATTEMPT_TO_CREATE_OUTSIDE_CLIENT_CONTEXT = -2147467228;

                //
                // Zusammenfassung:
                //     Activations on the server are paused.
                public const int CO_E_SERVER_PAUSED = -2147467227;

                //
                // Zusammenfassung:
                //     Activations on the server are not paused.
                public const int CO_E_SERVER_NOT_PAUSED = -2147467226;

                //
                // Zusammenfassung:
                //     The component or application containing the component has been disabled.
                public const int CO_E_CLASS_DISABLED = -2147467225;

                //
                // Zusammenfassung:
                //     The common language runtime is not available.
                public const int CO_E_CLRNOTAVAILABLE = -2147467224;

                //
                // Zusammenfassung:
                //     The thread-pool rejected the submitted asynchronous work.
                public const int CO_E_ASYNC_WORK_REJECTED = -2147467223;

                //
                // Zusammenfassung:
                //     The server started, but it did not finish initializing in a timely fashion.
                public const int CO_E_SERVER_INIT_TIMEOUT = -2147467222;

                //
                // Zusammenfassung:
                //     Unable to complete the call because there is no COM+ security context inside
                //     IObjectControl.Activate.
                public const int CO_E_NO_SECCTX_IN_ACTIVATE = -2147467221;

                //
                // Zusammenfassung:
                //     The provided tracker configuration is invalid.
                public const int CO_E_TRACKER_CONFIG = -2147467216;

                //
                // Zusammenfassung:
                //     The provided thread pool configuration is invalid.
                public const int CO_E_THREADPOOL_CONFIG = -2147467215;

                //
                // Zusammenfassung:
                //     The provided side-by-side configuration is invalid.
                public const int CO_E_SXS_CONFIG = -2147467214;

                //
                // Zusammenfassung:
                //     The server principal name (SPN) obtained during security negotiation is malformed.
                public const int CO_E_MALFORMED_SPN = -2147467213;

                //
                // Zusammenfassung:
                //     Catastrophic failure.
                public const int E_UNEXPECTED = -2147418113;

                //
                // Zusammenfassung:
                //     Call was rejected by callee.
                public const int RPC_E_CALL_REJECTED = -2147418111;

                //
                // Zusammenfassung:
                //     Call was canceled by the message filter.
                public const int RPC_E_CALL_CANCELED = -2147418110;

                //
                // Zusammenfassung:
                //     The caller is dispatching an intertask SendMessage call and cannot call out via
                //     PostMessage.
                public const int RPC_E_CANTPOST_INSENDCALL = -2147418109;

                //
                // Zusammenfassung:
                //     The caller is dispatching an asynchronous call and cannot make an outgoing call
                //     on behalf of this call.
                public const int RPC_E_CANTCALLOUT_INASYNCCALL = -2147418108;

                //
                // Zusammenfassung:
                //     It is illegal to call out while inside message filter.
                public const int RPC_E_CANTCALLOUT_INEXTERNALCALL = -2147418107;

                //
                // Zusammenfassung:
                //     The connection terminated or is in a bogus state and can no longer be used. Other
                //     connections are still valid.
                public const int RPC_E_CONNECTION_TERMINATED = -2147418106;

                //
                // Zusammenfassung:
                //     The callee (the server, not the server application) is not available and disappeared;
                //     all connections are invalid. The call might have executed.
                public const int RPC_E_SERVER_DIED = -2147418105;

                //
                // Zusammenfassung:
                //     The caller (client) disappeared while the callee (server) was processing a call.
                public const int RPC_E_CLIENT_DIED = -2147418104;

                //
                // Zusammenfassung:
                //     The data packet with the marshaled parameter data is incorrect.
                public const int RPC_E_INVALID_DATAPACKET = -2147418103;

                //
                // Zusammenfassung:
                //     The call was not transmitted properly; the message queue was full and was not
                //     emptied after yielding.
                public const int RPC_E_CANTTRANSMIT_CALL = -2147418102;

                //
                // Zusammenfassung:
                //     The client RPC caller cannot marshal the parameter data due to errors (such as
                //     low memory).
                public const int RPC_E_CLIENT_CANTMARSHAL_DATA = -2147418101;

                //
                // Zusammenfassung:
                //     The client RPC caller cannot unmarshal the return data due to errors (such as
                //     low memory).
                public const int RPC_E_CLIENT_CANTUNMARSHAL_DATA = -2147418100;

                //
                // Zusammenfassung:
                //     The server RPC callee cannot marshal the return data due to errors (such as low
                //     memory).
                public const int RPC_E_SERVER_CANTMARSHAL_DATA = -2147418099;

                //
                // Zusammenfassung:
                //     The server RPC callee cannot unmarshal the parameter data due to errors (such
                //     as low memory).
                public const int RPC_E_SERVER_CANTUNMARSHAL_DATA = -2147418098;

                //
                // Zusammenfassung:
                //     Received data is invalid. The data might be server or client data.
                public const int RPC_E_INVALID_DATA = -2147418097;

                //
                // Zusammenfassung:
                //     A particular parameter is invalid and cannot be (un)marshaled.
                public const int RPC_E_INVALID_PARAMETER = -2147418096;

                //
                // Zusammenfassung:
                //     There is no second outgoing call on same channel in DDE conversation.
                public const int RPC_E_CANTCALLOUT_AGAIN = -2147418095;

                //
                // Zusammenfassung:
                //     The callee (the server, not the server application) is not available and disappeared;
                //     all connections are invalid. The call did not execute.
                public const int RPC_E_SERVER_DIED_DNE = -2147418094;

                //
                // Zusammenfassung:
                //     System call failed.
                public const int RPC_E_SYS_CALL_FAILED = -2147417856;

                //
                // Zusammenfassung:
                //     Could not allocate some required resource (such as memory or events)
                public const int RPC_E_OUT_OF_RESOURCES = -2147417855;

                //
                // Zusammenfassung:
                //     Attempted to make calls on more than one thread in single-threaded mode.
                public const int RPC_E_ATTEMPTED_MULTITHREAD = -2147417854;

                //
                // Zusammenfassung:
                //     The requested interface is not registered on the server object.
                public const int RPC_E_NOT_REGISTERED = -2147417853;

                //
                // Zusammenfassung:
                //     RPC could not call the server or could not return the results of calling the
                //     server.
                public const int RPC_E_FAULT = -2147417852;

                //
                // Zusammenfassung:
                //     The server threw an exception.
                public const int RPC_E_SERVERFAULT = -2147417851;

                //
                // Zusammenfassung:
                //     Cannot change thread mode after it is set.
                public const int RPC_E_CHANGED_MODE = -2147417850;

                //
                // Zusammenfassung:
                //     The method called does not exist on the server.
                public const int RPC_E_INVALIDMETHOD = -2147417849;

                //
                // Zusammenfassung:
                //     The object invoked has disconnected from its clients.
                public const int RPC_E_DISCONNECTED = -2147417848;

                //
                // Zusammenfassung:
                //     The object invoked chose not to process the call now. Try again later.
                public const int RPC_E_RETRY = -2147417847;

                //
                // Zusammenfassung:
                //     The message filter indicated that the application is busy.
                public const int RPC_E_SERVERCALL_RETRYLATER = -2147417846;

                //
                // Zusammenfassung:
                //     The message filter rejected the call.
                public const int RPC_E_SERVERCALL_REJECTED = -2147417845;

                //
                // Zusammenfassung:
                //     A call control interface was called with invalid data.
                public const int RPC_E_INVALID_CALLDATA = -2147417844;

                //
                // Zusammenfassung:
                //     An outgoing call cannot be made because the application is dispatching an input-synchronous
                //     call.
                public const int RPC_E_CANTCALLOUT_ININPUTSYNCCALL = -2147417843;

                //
                // Zusammenfassung:
                //     The application called an interface that was marshaled for a different thread.
                public const int RPC_E_WRONG_THREAD = -2147417842;

                //
                // Zusammenfassung:
                //     CoInitialize has not been called on the current thread.
                public const int RPC_E_THREAD_NOT_INIT = -2147417841;

                //
                // Zusammenfassung:
                //     The version of OLE on the client and server machines does not match.
                public const int RPC_E_VERSION_MISMATCH = -2147417840;

                //
                // Zusammenfassung:
                //     OLE received a packet with an invalid header.
                public const int RPC_E_INVALID_HEADER = -2147417839;

                //
                // Zusammenfassung:
                //     OLE received a packet with an invalid extension.
                public const int RPC_E_INVALID_EXTENSION = -2147417838;

                //
                // Zusammenfassung:
                //     The requested object or interface does not exist.
                public const int RPC_E_INVALID_IPID = -2147417837;

                //
                // Zusammenfassung:
                //     The requested object does not exist.
                public const int RPC_E_INVALID_OBJECT = -2147417836;

                //
                // Zusammenfassung:
                //     OLE has sent a request and is waiting for a reply.
                public const int RPC_S_CALLPENDING = -2147417835;

                //
                // Zusammenfassung:
                //     OLE is waiting before retrying a request.
                public const int RPC_S_WAITONTIMER = -2147417834;

                //
                // Zusammenfassung:
                //     Call context cannot be accessed after call completed.
                public const int RPC_E_CALL_COMPLETE = -2147417833;

                //
                // Zusammenfassung:
                //     Impersonate on unsecure calls is not supported.
                public const int RPC_E_UNSECURE_CALL = -2147417832;

                //
                // Zusammenfassung:
                //     Security must be initialized before any interfaces are marshaled or unmarshaled.
                //     It cannot be changed after initialized.
                public const int RPC_E_TOO_LATE = -2147417831;

                //
                // Zusammenfassung:
                //     No security packages are installed on this machine, the user is not logged on,
                //     or there are no compatible security packages between the client and server.
                public const int RPC_E_NO_GOOD_SECURITY_PACKAGES = -2147417830;

                //
                // Zusammenfassung:
                //     Access is denied.
                public const int RPC_E_ACCESS_DENIED = -2147417829;

                //
                // Zusammenfassung:
                //     Remote calls are not allowed for this process.
                public const int RPC_E_REMOTE_DISABLED = -2147417828;

                //
                // Zusammenfassung:
                //     The marshaled interface data packet (OBJREF) has an invalid or unknown format.
                public const int RPC_E_INVALID_OBJREF = -2147417827;

                //
                // Zusammenfassung:
                //     No context is associated with this call. This happens for some custom marshaled
                //     calls and on the client side of the call.
                public const int RPC_E_NO_CONTEXT = -2147417826;

                //
                // Zusammenfassung:
                //     This operation returned because the time-out period expired.
                public const int RPC_E_TIMEOUT = -2147417825;

                //
                // Zusammenfassung:
                //     There are no synchronize objects to wait on.
                public const int RPC_E_NO_SYNC = -2147417824;

                //
                // Zusammenfassung:
                //     Full subject issuer chain Secure Sockets Layer (SSL) principal name expected
                //     from the server.
                public const int RPC_E_FULLSIC_REQUIRED = -2147417823;

                //
                // Zusammenfassung:
                //     Principal name is not a valid Microsoft standard (msstd) name.
                public const int RPC_E_INVALID_STD_NAME = -2147417822;

                //
                // Zusammenfassung:
                //     Unable to impersonate DCOM client.
                public const int CO_E_FAILEDTOIMPERSONATE = -2147417821;

                //
                // Zusammenfassung:
                //     Unable to obtain server's security context.
                public const int CO_E_FAILEDTOGETSECCTX = -2147417820;

                //
                // Zusammenfassung:
                //     Unable to open the access token of the current thread.
                public const int CO_E_FAILEDTOOPENTHREADTOKEN = -2147417819;

                //
                // Zusammenfassung:
                //     Unable to obtain user information from an access token.
                public const int CO_E_FAILEDTOGETTOKENINFO = -2147417818;

                //
                // Zusammenfassung:
                //     The client who called IAccessControl::IsAccessPermitted was not the trustee provided
                //     to the method.
                public const int CO_E_TRUSTEEDOESNTMATCHCLIENT = -2147417817;

                //
                // Zusammenfassung:
                //     Unable to obtain the client's security blanket.
                public const int CO_E_FAILEDTOQUERYCLIENTBLANKET = -2147417816;

                //
                // Zusammenfassung:
                //     Unable to set a discretionary access control list (ACL) into a security descriptor.
                public const int CO_E_FAILEDTOSETDACL = -2147417815;

                //
                // Zusammenfassung:
                //     The system function AccessCheck returned false.
                public const int CO_E_ACCESSCHECKFAILED = -2147417814;

                //
                // Zusammenfassung:
                //     Either NetAccessDel or NetAccessAdd returned an error code.
                public const int CO_E_NETACCESSAPIFAILED = -2147417813;

                //
                // Zusammenfassung:
                //     One of the trustee strings provided by the user did not conform to the <Domain>\<Name>
                //     syntax and it was not the *" string".
                public const int CO_E_WRONGTRUSTEENAMESYNTAX = -2147417812;

                //
                // Zusammenfassung:
                //     One of the security identifiers provided by the user was invalid.
                public const int CO_E_INVALIDSID = -2147417811;

                //
                // Zusammenfassung:
                //     Unable to convert a wide character trustee string to a multiple-byte trustee
                //     string.
                public const int CO_E_CONVERSIONFAILED = -2147417810;

                //
                // Zusammenfassung:
                //     Unable to find a security identifier that corresponds to a trustee string provided
                //     by the user.
                public const int CO_E_NOMATCHINGSIDFOUND = -2147417809;

                //
                // Zusammenfassung:
                //     The system function LookupAccountSID failed.
                public const int CO_E_LOOKUPACCSIDFAILED = -2147417808;

                //
                // Zusammenfassung:
                //     Unable to find a trustee name that corresponds to a security identifier provided
                //     by the user.
                public const int CO_E_NOMATCHINGNAMEFOUND = -2147417807;

                //
                // Zusammenfassung:
                //     The system function LookupAccountName failed.
                public const int CO_E_LOOKUPACCNAMEFAILED = -2147417806;

                //
                // Zusammenfassung:
                //     Unable to set or reset a serialization handle.
                public const int CO_E_SETSERLHNDLFAILED = -2147417805;

                //
                // Zusammenfassung:
                //     Unable to obtain the Windows directory.
                public const int CO_E_FAILEDTOGETWINDIR = -2147417804;

                //
                // Zusammenfassung:
                //     Path too long.
                public const int CO_E_PATHTOOLONG = -2147417803;

                //
                // Zusammenfassung:
                //     Unable to generate a UUID.
                public const int CO_E_FAILEDTOGENUUID = -2147417802;

                //
                // Zusammenfassung:
                //     Unable to create file.
                public const int CO_E_FAILEDTOCREATEFILE = -2147417801;

                //
                // Zusammenfassung:
                //     Unable to close a serialization handle or a file handle.
                public const int CO_E_FAILEDTOCLOSEHANDLE = -2147417800;

                //
                // Zusammenfassung:
                //     The number of access control entries (ACEs) in an ACL exceeds the system limit.
                public const int CO_E_EXCEEDSYSACLLIMIT = -2147417799;

                //
                // Zusammenfassung:
                //     Not all the DENY_ACCESS ACEs are arranged in front of the GRANT_ACCESS ACEs in
                //     the stream.
                public const int CO_E_ACESINWRONGORDER = -2147417798;

                //
                // Zusammenfassung:
                //     The version of ACL format in the stream is not supported by this implementation
                //     of IAccessControl.
                public const int CO_E_INCOMPATIBLESTREAMVERSION = -2147417797;

                //
                // Zusammenfassung:
                //     Unable to open the access token of the server process.
                public const int CO_E_FAILEDTOOPENPROCESSTOKEN = -2147417796;

                //
                // Zusammenfassung:
                //     Unable to decode the ACL in the stream provided by the user.
                public const int CO_E_DECODEFAILED = -2147417795;

                //
                // Zusammenfassung:
                //     The COM IAccessControl object is not initialized.
                public const int CO_E_ACNOTINITIALIZED = -2147417793;

                //
                // Zusammenfassung:
                //     Call Cancellation is disabled.
                public const int CO_E_CANCEL_DISABLED = -2147417792;

                //
                // Zusammenfassung:
                //     An internal error occurred.
                public const int RPC_E_UNEXPECTED = -2147352577;

                //
                // Zusammenfassung:
                //     Unknown interface.
                public const int DISP_E_UNKNOWNINTERFACE = -2147352575;

                //
                // Zusammenfassung:
                //     Member not found.
                public const int DISP_E_MEMBERNOTFOUND = -2147352573;

                //
                // Zusammenfassung:
                //     Parameter not found.
                public const int DISP_E_PARAMNOTFOUND = -2147352572;

                //
                // Zusammenfassung:
                //     Type mismatch.
                public const int DISP_E_TYPEMISMATCH = -2147352571;

                //
                // Zusammenfassung:
                //     Unknown name.
                public const int DISP_E_UNKNOWNNAME = -2147352570;

                //
                // Zusammenfassung:
                //     No named arguments.
                public const int DISP_E_NONAMEDARGS = -2147352569;

                //
                // Zusammenfassung:
                //     Bad variable type.
                public const int DISP_E_BADVARTYPE = -2147352568;

                //
                // Zusammenfassung:
                //     Exception occurred.
                public const int DISP_E_EXCEPTION = -2147352567;

                //
                // Zusammenfassung:
                //     Out of present range.
                public const int DISP_E_OVERFLOW = -2147352566;

                //
                // Zusammenfassung:
                //     Invalid index.
                public const int DISP_E_BADINDEX = -2147352565;

                //
                // Zusammenfassung:
                //     Unknown language.
                public const int DISP_E_UNKNOWNLCID = -2147352564;

                //
                // Zusammenfassung:
                //     Memory is locked.
                public const int DISP_E_ARRAYISLOCKED = -2147352563;

                //
                // Zusammenfassung:
                //     Invalid number of parameters.
                public const int DISP_E_BADPARAMCOUNT = -2147352562;

                //
                // Zusammenfassung:
                //     Parameter not optional.
                public const int DISP_E_PARAMNOTOPTIONAL = -2147352561;

                //
                // Zusammenfassung:
                //     Invalid callee.
                public const int DISP_E_BADCALLEE = -2147352560;

                //
                // Zusammenfassung:
                //     Does not support a collection.
                public const int DISP_E_NOTACOLLECTION = -2147352559;

                //
                // Zusammenfassung:
                //     Division by zero.
                public const int DISP_E_DIVBYZERO = -2147352558;

                //
                // Zusammenfassung:
                //     Buffer too small.
                public const int DISP_E_BUFFERTOOSMALL = -2147352557;

                //
                // Zusammenfassung:
                //     Buffer too small.
                public const int TYPE_E_BUFFERTOOSMALL = -2147319786;

                //
                // Zusammenfassung:
                //     Field name not defined in the record.
                public const int TYPE_E_FIELDNOTFOUND = -2147319785;

                //
                // Zusammenfassung:
                //     Old format or invalid type library.
                public const int TYPE_E_INVDATAREAD = -2147319784;

                //
                // Zusammenfassung:
                //     Old format or invalid type library.
                public const int TYPE_E_UNSUPFORMAT = -2147319783;

                //
                // Zusammenfassung:
                //     Error accessing the OLE registry.
                public const int TYPE_E_REGISTRYACCESS = -2147319780;

                //
                // Zusammenfassung:
                //     Library not registered.
                public const int TYPE_E_LIBNOTREGISTERED = -2147319779;

                //
                // Zusammenfassung:
                //     Bound to unknown type.
                public const int TYPE_E_UNDEFINEDTYPE = -2147319769;

                //
                // Zusammenfassung:
                //     Qualified name disallowed.
                public const int TYPE_E_QUALIFIEDNAMEDISALLOWED = -2147319768;

                //
                // Zusammenfassung:
                //     Invalid forward reference, or reference to uncompiled type.
                public const int TYPE_E_INVALIDSTATE = -2147319767;

                //
                // Zusammenfassung:
                //     Type mismatch.
                public const int TYPE_E_WRONGTYPEKIND = -2147319766;

                //
                // Zusammenfassung:
                //     Element not found.
                public const int TYPE_E_ELEMENTNOTFOUND = -2147319765;

                //
                // Zusammenfassung:
                //     Ambiguous name.
                public const int TYPE_E_AMBIGUOUSNAME = -2147319764;

                //
                // Zusammenfassung:
                //     Name already exists in the library.
                public const int TYPE_E_NAMECONFLICT = -2147319763;

                //
                // Zusammenfassung:
                //     Unknown language code identifier (LCID).
                public const int TYPE_E_UNKNOWNLCID = -2147319762;

                //
                // Zusammenfassung:
                //     Function not defined in specified DLL.
                public const int TYPE_E_DLLFUNCTIONNOTFOUND = -2147319761;

                //
                // Zusammenfassung:
                //     Wrong module kind for the operation.
                public const int TYPE_E_BADMODULEKIND = -2147317571;

                //
                // Zusammenfassung:
                //     Size cannot exceed 64 KB.
                public const int TYPE_E_SIZETOOBIG = -2147317563;

                //
                // Zusammenfassung:
                //     Duplicate ID in inheritance hierarchy.
                public const int TYPE_E_DUPLICATEID = -2147317562;

                //
                // Zusammenfassung:
                //     Incorrect inheritance depth in standard OLE hmember.
                public const int TYPE_E_INVALIDID = -2147317553;

                //
                // Zusammenfassung:
                //     Type mismatch.
                public const int TYPE_E_TYPEMISMATCH = -2147316576;

                //
                // Zusammenfassung:
                //     Invalid number of arguments.
                public const int TYPE_E_OUTOFBOUNDS = -2147316575;

                //
                // Zusammenfassung:
                //     I/O error.
                public const int TYPE_E_IOERROR = -2147316574;

                //
                // Zusammenfassung:
                //     Error creating unique .tmp file.
                public const int TYPE_E_CANTCREATETMPFILE = -2147316573;

                //
                // Zusammenfassung:
                //     Error loading type library or DLL.
                public const int TYPE_E_CANTLOADLIBRARY = -2147312566;

                //
                // Zusammenfassung:
                //     Inconsistent property functions.
                public const int TYPE_E_INCONSISTENTPROPFUNCS = -2147312509;

                //
                // Zusammenfassung:
                //     Circular dependency between types and modules.
                public const int TYPE_E_CIRCULARTYPE = -2147312508;

                //
                // Zusammenfassung:
                //     Unable to perform requested operation.
                public const int STG_E_INVALIDFUNCTION = -2147287039;

                //
                // Zusammenfassung:
                //     %1 could not be found.
                public const int STG_E_FILENOTFOUND = -2147287038;

                //
                // Zusammenfassung:
                //     The path %1 could not be found.
                public const int STG_E_PATHNOTFOUND = -2147287037;

                //
                // Zusammenfassung:
                //     There are insufficient resources to open another file.
                public const int STG_E_TOOMANYOPENFILES = -2147287036;

                //
                // Zusammenfassung:
                //     Access denied.
                public const int STG_E_ACCESSDENIED = -2147287035;

                //
                // Zusammenfassung:
                //     Attempted an operation on an invalid object.
                public const int STG_E_INVALIDHANDLE = -2147287034;

                //
                // Zusammenfassung:
                //     There is insufficient memory available to complete operation.
                public const int STG_E_INSUFFICIENTMEMORY = -2147287032;

                //
                // Zusammenfassung:
                //     Invalid pointer error.
                public const int STG_E_INVALIDPOINTER = -2147287031;

                //
                // Zusammenfassung:
                //     There are no more entries to return.
                public const int STG_E_NOMOREFILES = -2147287022;

                //
                // Zusammenfassung:
                //     Disk is write-protected.
                public const int STG_E_DISKISWRITEPROTECTED = -2147287021;

                //
                // Zusammenfassung:
                //     An error occurred during a seek operation.
                public const int STG_E_SEEKERROR = -2147287015;

                //
                // Zusammenfassung:
                //     A disk error occurred during a write operation.
                public const int STG_E_WRITEFAULT = -2147287011;

                //
                // Zusammenfassung:
                //     A disk error occurred during a read operation.
                public const int STG_E_READFAULT = -2147287010;

                //
                // Zusammenfassung:
                //     A share violation has occurred.
                public const int STG_E_SHAREVIOLATION = -2147287008;

                //
                // Zusammenfassung:
                //     A lock violation has occurred.
                public const int STG_E_LOCKVIOLATION = -2147287007;

                //
                // Zusammenfassung:
                //     %1 already exists.
                public const int STG_E_FILEALREADYEXISTS = -2147286960;

                //
                // Zusammenfassung:
                //     Invalid parameter error.
                public const int STG_E_INVALIDPARAMETER = -2147286953;

                //
                // Zusammenfassung:
                //     There is insufficient disk space to complete operation.
                public const int STG_E_MEDIUMFULL = -2147286928;

                //
                // Zusammenfassung:
                //     Illegal write of non-simple property to simple property set.
                public const int STG_E_PROPSETMISMATCHED = -2147286800;

                //
                // Zusammenfassung:
                //     An application programming interface (API) call exited abnormally.
                public const int STG_E_ABNORMALAPIEXIT = -2147286790;

                //
                // Zusammenfassung:
                //     The file %1 is not a valid compound file.
                public const int STG_E_INVALIDHEADER = -2147286789;

                //
                // Zusammenfassung:
                //     The name %1 is not valid.
                public const int STG_E_INVALIDNAME = -2147286788;

                //
                // Zusammenfassung:
                //     An unexpected error occurred.
                public const int STG_E_UNKNOWN = -2147286787;

                //
                // Zusammenfassung:
                //     That function is not implemented.
                public const int STG_E_UNIMPLEMENTEDFUNCTION = -2147286786;

                //
                // Zusammenfassung:
                //     Invalid flag error.
                public const int STG_E_INVALIDFLAG = -2147286785;

                //
                // Zusammenfassung:
                //     Attempted to use an object that is busy.
                public const int STG_E_INUSE = -2147286784;

                //
                // Zusammenfassung:
                //     The storage has been changed since the last commit.
                public const int STG_E_NOTCURRENT = -2147286783;

                //
                // Zusammenfassung:
                //     Attempted to use an object that has ceased to exist.
                public const int STG_E_REVERTED = -2147286782;

                //
                // Zusammenfassung:
                //     Cannot save.
                public const int STG_E_CANTSAVE = -2147286781;

                //
                // Zusammenfassung:
                //     The compound file %1 was produced with an incompatible version of storage.
                public const int STG_E_OLDFORMAT = -2147286780;

                //
                // Zusammenfassung:
                //     The compound file %1 was produced with a newer version of storage.
                public const int STG_E_OLDDLL = -2147286779;

                //
                // Zusammenfassung:
                //     Share.exe or equivalent is required for operation.
                public const int STG_E_SHAREREQUIRED = -2147286778;

                //
                // Zusammenfassung:
                //     Illegal operation called on non-file based storage.
                public const int STG_E_NOTFILEBASEDSTORAGE = -2147286777;

                //
                // Zusammenfassung:
                //     Illegal operation called on object with extant marshalings.
                public const int STG_E_EXTANTMARSHALLINGS = -2147286776;

                //
                // Zusammenfassung:
                //     The docfile has been corrupted.
                public const int STG_E_DOCFILECORRUPT = -2147286775;

                //
                // Zusammenfassung:
                //     OLE32.DLL has been loaded at the wrong address.
                public const int STG_E_BADBASEADDRESS = -2147286768;

                //
                // Zusammenfassung:
                //     The compound file is too large for the current implementation.
                public const int STG_E_DOCFILETOOLARGE = -2147286767;

                //
                // Zusammenfassung:
                //     The compound file was not created with the STGM_SIMPLE flag.
                public const int STG_E_NOTSIMPLEFORMAT = -2147286766;

                //
                // Zusammenfassung:
                //     The file download was aborted abnormally. The file is incomplete.
                public const int STG_E_INCOMPLETE = -2147286527;

                //
                // Zusammenfassung:
                //     The file download has been terminated.
                public const int STG_E_TERMINATED = -2147286526;

                //
                // Zusammenfassung:
                //     Generic Copy Protection Error.
                public const int STG_E_STATUS_COPY_PROTECTION_FAILURE = -2147286267;

                //
                // Zusammenfassung:
                //     Copy Protection Error—DVD CSS Authentication failed.
                public const int STG_E_CSS_AUTHENTICATION_FAILURE = -2147286266;

                //
                // Zusammenfassung:
                //     Copy Protection Error—The given sector does not have a valid CSS key.
                public const int STG_E_CSS_KEY_NOT_PRESENT = -2147286265;

                //
                // Zusammenfassung:
                //     Copy Protection Error—DVD session key not established.
                public const int STG_E_CSS_KEY_NOT_ESTABLISHED = -2147286264;

                //
                // Zusammenfassung:
                //     Copy Protection Error—The read failed because the sector is encrypted.
                public const int STG_E_CSS_SCRAMBLED_SECTOR = -2147286263;

                //
                // Zusammenfassung:
                //     Copy Protection Error—The current DVD's region does not correspond to the region
                //     setting of the drive.
                public const int STG_E_CSS_REGION_MISMATCH = -2147286262;

                //
                // Zusammenfassung:
                //     Copy Protection Error—The drive's region setting might be permanent or the number
                //     of user resets has been exhausted.
                public const int STG_E_RESETS_EXHAUSTED = -2147286261;

                //
                // Zusammenfassung:
                //     Invalid OLEVERB structure.
                public const int OLE_E_OLEVERB = -2147221504;

                //
                // Zusammenfassung:
                //     Invalid advise flags.
                public const int OLE_E_ADVF = -2147221503;

                //
                // Zusammenfassung:
                //     Cannot enumerate any more because the associated data is missing.
                public const int OLE_E_ENUM_NOMORE = -2147221502;

                //
                // Zusammenfassung:
                //     This implementation does not take advises.
                public const int OLE_E_ADVISENOTSUPPORTED = -2147221501;

                //
                // Zusammenfassung:
                //     There is no connection for this connection ID.
                public const int OLE_E_NOCONNECTION = -2147221500;

                //
                // Zusammenfassung:
                //     Need to run the object to perform this operation.
                public const int OLE_E_NOTRUNNING = -2147221499;

                //
                // Zusammenfassung:
                //     There is no cache to operate on.
                public const int OLE_E_NOCACHE = -2147221498;

                //
                // Zusammenfassung:
                //     Uninitialized object.
                public const int OLE_E_BLANK = -2147221497;

                //
                // Zusammenfassung:
                //     Linked object's source class has changed.
                public const int OLE_E_CLASSDIFF = -2147221496;

                //
                // Zusammenfassung:
                //     Not able to get the moniker of the object.
                public const int OLE_E_CANT_GETMONIKER = -2147221495;

                //
                // Zusammenfassung:
                //     Not able to bind to the source.
                public const int OLE_E_CANT_BINDTOSOURCE = -2147221494;

                //
                // Zusammenfassung:
                //     Object is static; operation not allowed.
                public const int OLE_E_STATIC = -2147221493;

                //
                // Zusammenfassung:
                //     User canceled out of the Save dialog box.
                public const int OLE_E_PROMPTSAVECANCELLED = -2147221492;

                //
                // Zusammenfassung:
                //     Invalid rectangle.
                public const int OLE_E_INVALIDRECT = -2147221491;

                //
                // Zusammenfassung:
                //     compobj.dll is too old for the ole2.dll initialized.
                public const int OLE_E_WRONGCOMPOBJ = -2147221490;

                //
                // Zusammenfassung:
                //     Invalid window handle.
                public const int OLE_E_INVALIDHWND = -2147221489;

                //
                // Zusammenfassung:
                //     Object is not in any of the inplace active states.
                public const int OLE_E_NOT_INPLACEACTIVE = -2147221488;

                //
                // Zusammenfassung:
                //     Not able to convert object.
                public const int OLE_E_CANTCONVERT = -2147221487;

                //
                // Zusammenfassung:
                //     Not able to perform the operation because object is not given storage yet.
                public const int OLE_E_NOSTORAGE = -2147221486;

                //
                // Zusammenfassung:
                //     Invalid FORMATETC structure.
                public const int DV_E_FORMATETC = -2147221404;

                //
                // Zusammenfassung:
                //     Invalid DVTARGETDEVICE structure.
                public const int DV_E_DVTARGETDEVICE = -2147221403;

                //
                // Zusammenfassung:
                //     Invalid STDGMEDIUM structure.
                public const int DV_E_STGMEDIUM = -2147221402;

                //
                // Zusammenfassung:
                //     Invalid STATDATA structure.
                public const int DV_E_STATDATA = -2147221401;

                //
                // Zusammenfassung:
                //     Invalid lindex.
                public const int DV_E_LINDEX = -2147221400;

                //
                // Zusammenfassung:
                //     Invalid TYMED structure.
                public const int DV_E_TYMED = -2147221399;

                //
                // Zusammenfassung:
                //     Invalid clipboard format.
                public const int DV_E_CLIPFORMAT = -2147221398;

                //
                // Zusammenfassung:
                //     Invalid aspects.
                public const int DV_E_DVASPECT = -2147221397;

                //
                // Zusammenfassung:
                //     The tdSize parameter of the DVTARGETDEVICE structure is invalid.
                public const int DV_E_DVTARGETDEVICE_SIZE = -2147221396;

                //
                // Zusammenfassung:
                //     Object does not support IViewObject interface.
                public const int DV_E_NOIVIEWOBJECT = -2147221395;

                //
                // Zusammenfassung:
                //     Trying to revoke a drop target that has not been registered.
                public const int DRAGDROP_E_NOTREGISTERED = -2147221248;

                //
                // Zusammenfassung:
                //     This window has already been registered as a drop target.
                public const int DRAGDROP_E_ALREADYREGISTERED = -2147221247;

                //
                // Zusammenfassung:
                //     Invalid window handle.
                public const int DRAGDROP_E_INVALIDHWND = -2147221246;

                //
                // Zusammenfassung:
                //     Class does not support aggregation (or class object is remote).
                public const int CLASS_E_NOAGGREGATION = -2147221232;

                //
                // Zusammenfassung:
                //     ClassFactory cannot supply requested class.
                public const int CLASS_E_CLASSNOTAVAILABLE = -2147221231;

                //
                // Zusammenfassung:
                //     Class is not licensed for use.
                public const int CLASS_E_NOTLICENSED = -2147221230;

                //
                // Zusammenfassung:
                //     Error drawing view.
                public const int VIEW_E_DRAW = -2147221184;

                //
                // Zusammenfassung:
                //     Could not read key from registry.
                public const int REGDB_E_READREGDB = -2147221168;

                //
                // Zusammenfassung:
                //     Could not write key to registry.
                public const int REGDB_E_WRITEREGDB = -2147221167;

                //
                // Zusammenfassung:
                //     Could not find the key in the registry.
                public const int REGDB_E_KEYMISSING = -2147221166;

                //
                // Zusammenfassung:
                //     Invalid value for registry.
                public const int REGDB_E_INVALIDVALUE = -2147221165;

                //
                // Zusammenfassung:
                //     Class not registered.
                public const int REGDB_E_CLASSNOTREG = -2147221164;

                //
                // Zusammenfassung:
                //     Interface not registered.
                public const int REGDB_E_IIDNOTREG = -2147221163;

                //
                // Zusammenfassung:
                //     Threading model entry is not valid.
                public const int REGDB_E_BADTHREADINGMODEL = -2147221162;

                //
                // Zusammenfassung:
                //     CATID does not exist.
                public const int CAT_E_CATIDNOEXIST = -2147221152;

                //
                // Zusammenfassung:
                //     Description not found.
                public const int CAT_E_NODESCRIPTION = -2147221151;

                //
                // Zusammenfassung:
                //     No package in the software installation data in Active Directory meets this criteria.
                public const int CS_E_PACKAGE_NOTFOUND = -2147221148;

                //
                // Zusammenfassung:
                //     Deleting this will break the referential integrity of the software installation
                //     data in Active Directory.
                public const int CS_E_NOT_DELETABLE = -2147221147;

                //
                // Zusammenfassung:
                //     The CLSID was not found in the software installation data in Active Directory.
                public const int CS_E_CLASS_NOTFOUND = -2147221146;

                //
                // Zusammenfassung:
                //     The software installation data in Active Directory is corrupt.
                public const int CS_E_INVALID_VERSION = -2147221145;

                //
                // Zusammenfassung:
                //     There is no software installation data in Active Directory.
                public const int CS_E_NO_CLASSSTORE = -2147221144;

                //
                // Zusammenfassung:
                //     There is no software installation data object in Active Directory.
                public const int CS_E_OBJECT_NOTFOUND = -2147221143;

                //
                // Zusammenfassung:
                //     The software installation data object in Active Directory already exists.
                public const int CS_E_OBJECT_ALREADY_EXISTS = -2147221142;

                //
                // Zusammenfassung:
                //     The path to the software installation data in Active Directory is not correct.
                public const int CS_E_INVALID_PATH = -2147221141;

                //
                // Zusammenfassung:
                //     A network error interrupted the operation.
                public const int CS_E_NETWORK_ERROR = -2147221140;

                //
                // Zusammenfassung:
                //     The size of this object exceeds the maximum size set by the administrator.
                public const int CS_E_ADMIN_LIMIT_EXCEEDED = -2147221139;

                //
                // Zusammenfassung:
                //     The schema for the software installation data in Active Directory does not match
                //     the required schema.
                public const int CS_E_SCHEMA_MISMATCH = -2147221138;

                //
                // Zusammenfassung:
                //     An error occurred in the software installation data in Active Directory.
                public const int CS_E_INTERNAL_ERROR = -2147221137;

                //
                // Zusammenfassung:
                //     Cache not updated.
                public const int CACHE_E_NOCACHE_UPDATED = -2147221136;

                //
                // Zusammenfassung:
                //     No verbs for OLE object.
                public const int OLEOBJ_E_NOVERBS = -2147221120;

                //
                // Zusammenfassung:
                //     Invalid verb for OLE object.
                public const int OLEOBJ_E_INVALIDVERB = -2147221119;

                //
                // Zusammenfassung:
                //     Undo is not available.
                public const int INPLACE_E_NOTUNDOABLE = -2147221088;

                //
                // Zusammenfassung:
                //     Space for tools is not available.
                public const int INPLACE_E_NOTOOLSPACE = -2147221087;

                //
                // Zusammenfassung:
                //     OLESTREAM Get method failed.
                public const int CONVERT10_E_OLESTREAM_GET = -2147221056;

                //
                // Zusammenfassung:
                //     OLESTREAM Put method failed.
                public const int CONVERT10_E_OLESTREAM_PUT = -2147221055;

                //
                // Zusammenfassung:
                //     Contents of the OLESTREAM not in correct format.
                public const int CONVERT10_E_OLESTREAM_FMT = -2147221054;

                //
                // Zusammenfassung:
                //     There was an error in a Windows GDI call while converting the bitmap to a device-independent
                //     bitmap (DIB).
                public const int CONVERT10_E_OLESTREAM_BITMAP_TO_DIB = -2147221053;

                //
                // Zusammenfassung:
                //     Contents of the IStorage not in correct format.
                public const int CONVERT10_E_STG_FMT = -2147221052;

                //
                // Zusammenfassung:
                //     Contents of IStorage is missing one of the standard streams.
                public const int CONVERT10_E_STG_NO_STD_STREAM = -2147221051;

                //
                // Zusammenfassung:
                //     There was an error in a Windows Graphics Device Interface (GDI) call while converting
                //     the DIB to a bitmap.
                public const int CONVERT10_E_STG_DIB_TO_BITMAP = -2147221050;

                //
                // Zusammenfassung:
                //     OpenClipboard failed.
                public const int CLIPBRD_E_CANT_OPEN = -2147221040;

                //
                // Zusammenfassung:
                //     EmptyClipboard failed.
                public const int CLIPBRD_E_CANT_EMPTY = -2147221039;

                //
                // Zusammenfassung:
                //     SetClipboard failed.
                public const int CLIPBRD_E_CANT_SET = -2147221038;

                //
                // Zusammenfassung:
                //     Data on clipboard is invalid.
                public const int CLIPBRD_E_BAD_DATA = -2147221037;

                //
                // Zusammenfassung:
                //     CloseClipboard failed.
                public const int CLIPBRD_E_CANT_CLOSE = -2147221036;

                //
                // Zusammenfassung:
                //     Moniker needs to be connected manually.
                public const int MK_E_CONNECTMANUALLY = -2147221024;

                //
                // Zusammenfassung:
                //     Operation exceeded deadline.
                public const int MK_E_EXCEEDEDDEADLINE = -2147221023;

                //
                // Zusammenfassung:
                //     Moniker needs to be generic.
                public const int MK_E_NEEDGENERIC = -2147221022;

                //
                // Zusammenfassung:
                //     Operation unavailable.
                public const int MK_E_UNAVAILABLE = -2147221021;

                //
                // Zusammenfassung:
                //     Invalid syntax.
                public const int MK_E_SYNTAX = -2147221020;

                //
                // Zusammenfassung:
                //     No object for moniker.
                public const int MK_E_NOOBJECT = -2147221019;

                //
                // Zusammenfassung:
                //     Bad extension for file.
                public const int MK_E_INVALIDEXTENSION = -2147221018;

                //
                // Zusammenfassung:
                //     Intermediate operation failed.
                public const int MK_E_INTERMEDIATEINTERFACENOTSUPPORTED = -2147221017;

                //
                // Zusammenfassung:
                //     Moniker is not bindable.
                public const int MK_E_NOTBINDABLE = -2147221016;

                //
                // Zusammenfassung:
                //     Moniker is not bound.
                public const int MK_E_NOTBOUND = -2147221015;

                //
                // Zusammenfassung:
                //     Moniker cannot open file.
                public const int MK_E_CANTOPENFILE = -2147221014;

                //
                // Zusammenfassung:
                //     User input required for operation to succeed.
                public const int MK_E_MUSTBOTHERUSER = -2147221013;

                //
                // Zusammenfassung:
                //     Moniker class has no inverse.
                public const int MK_E_NOINVERSE = -2147221012;

                //
                // Zusammenfassung:
                //     Moniker does not refer to storage.
                public const int MK_E_NOSTORAGE = -2147221011;

                //
                // Zusammenfassung:
                //     No common prefix.
                public const int MK_E_NOPREFIX = -2147221010;

                //
                // Zusammenfassung:
                //     Moniker could not be enumerated.
                public const int MK_E_ENUMERATION_FAILED = -2147221009;

                //
                // Zusammenfassung:
                //     CoInitialize has not been called.
                public const int CO_E_NOTINITIALIZED = -2147221008;

                //
                // Zusammenfassung:
                //     CoInitialize has already been called.
                public const int CO_E_ALREADYINITIALIZED = -2147221007;

                //
                // Zusammenfassung:
                //     Class of object cannot be determined.
                public const int CO_E_CANTDETERMINECLASS = -2147221006;

                //
                // Zusammenfassung:
                //     Invalid class string.
                public const int CO_E_CLASSSTRING = -2147221005;

                //
                // Zusammenfassung:
                //     Invalid interface string.
                public const int CO_E_IIDSTRING = -2147221004;

                //
                // Zusammenfassung:
                //     Application not found.
                public const int CO_E_APPNOTFOUND = -2147221003;

                //
                // Zusammenfassung:
                //     Application cannot be run more than once.
                public const int CO_E_APPSINGLEUSE = -2147221002;

                //
                // Zusammenfassung:
                //     Some error in application.
                public const int CO_E_ERRORINAPP = -2147221001;

                //
                // Zusammenfassung:
                //     DLL for class not found.
                public const int CO_E_DLLNOTFOUND = -2147221000;

                //
                // Zusammenfassung:
                //     Error in the DLL.
                public const int CO_E_ERRORINDLL = -2147220999;

                //
                // Zusammenfassung:
                //     Wrong operating system or operating system version for application.
                public const int CO_E_WRONGOSFORAPP = -2147220998;

                //
                // Zusammenfassung:
                //     Object is not registered.
                public const int CO_E_OBJNOTREG = -2147220997;

                //
                // Zusammenfassung:
                //     Object is already registered.
                public const int CO_E_OBJISREG = -2147220996;

                //
                // Zusammenfassung:
                //     Object is not connected to server.
                public const int CO_E_OBJNOTCONNECTED = -2147220995;

                //
                // Zusammenfassung:
                //     Application was launched, but it did not register a class factory.
                public const int CO_E_APPDIDNTREG = -2147220994;

                //
                // Zusammenfassung:
                //     Object has been released.
                public const int CO_E_RELEASED = -2147220993;

                //
                // Zusammenfassung:
                //     An event was unable to invoke any of the subscribers.
                public const int EVENT_E_ALL_SUBSCRIBERS_FAILED = -2147220991;

                //
                // Zusammenfassung:
                //     A syntax error occurred trying to evaluate a query string.
                public const int EVENT_E_QUERYSYNTAX = -2147220989;

                //
                // Zusammenfassung:
                //     An invalid field name was used in a query string.
                public const int EVENT_E_QUERYFIELD = -2147220988;

                //
                // Zusammenfassung:
                //     An unexpected exception was raised.
                public const int EVENT_E_INTERNALEXCEPTION = -2147220987;

                //
                // Zusammenfassung:
                //     An unexpected internal error was detected.
                public const int EVENT_E_INTERNALERROR = -2147220986;

                //
                // Zusammenfassung:
                //     The owner security identifier (SID) on a per-user subscription does not exist.
                public const int EVENT_E_INVALID_PER_USER_SID = -2147220985;

                //
                // Zusammenfassung:
                //     A user-supplied component or subscriber raised an exception.
                public const int EVENT_E_USER_EXCEPTION = -2147220984;

                //
                // Zusammenfassung:
                //     An interface has too many methods to fire events from.
                public const int EVENT_E_TOO_MANY_METHODS = -2147220983;

                //
                // Zusammenfassung:
                //     A subscription cannot be stored unless its event class already exists.
                public const int EVENT_E_MISSING_EVENTCLASS = -2147220982;

                //
                // Zusammenfassung:
                //     Not all the objects requested could be removed.
                public const int EVENT_E_NOT_ALL_REMOVED = -2147220981;

                //
                // Zusammenfassung:
                //     COM+ is required for this operation, but it is not installed.
                public const int EVENT_E_COMPLUS_NOT_INSTALLED = -2147220980;

                //
                // Zusammenfassung:
                //     Cannot modify or delete an object that was not added using the COM+ Administrative
                //     SDK.
                public const int EVENT_E_CANT_MODIFY_OR_DELETE_UNCONFIGURED_OBJECT = -2147220979;

                //
                // Zusammenfassung:
                //     Cannot modify or delete an object that was added using the COM+ Administrative
                //     SDK.
                public const int EVENT_E_CANT_MODIFY_OR_DELETE_CONFIGURED_OBJECT = -2147220978;

                //
                // Zusammenfassung:
                //     The event class for this subscription is in an invalid partition.
                public const int EVENT_E_INVALID_EVENT_CLASS_PARTITION = -2147220977;

                //
                // Zusammenfassung:
                //     The owner of the PerUser subscription is not logged on to the system specified.
                public const int EVENT_E_PER_USER_SID_NOT_LOGGED_ON = -2147220976;

                //
                // Zusammenfassung:
                //     Trigger not found.
                public const int SCHED_E_TRIGGER_NOT_FOUND = -2147216631;

                //
                // Zusammenfassung:
                //     One or more of the properties that are needed to run this task have not been
                //     set.
                public const int SCHED_E_TASK_NOT_READY = -2147216630;

                //
                // Zusammenfassung:
                //     There is no running instance of the task.
                public const int SCHED_E_TASK_NOT_RUNNING = -2147216629;

                //
                // Zusammenfassung:
                //     The Task Scheduler service is not installed on this computer.
                public const int SCHED_E_SERVICE_NOT_INSTALLED = -2147216628;

                //
                // Zusammenfassung:
                //     The task object could not be opened.
                public const int SCHED_E_CANNOT_OPEN_TASK = -2147216627;

                //
                // Zusammenfassung:
                //     The object is either an invalid task object or is not a task object.
                public const int SCHED_E_INVALID_TASK = -2147216626;

                //
                // Zusammenfassung:
                //     No account information could be found in the Task Scheduler security database
                //     for the task indicated.
                public const int SCHED_E_ACCOUNT_INFORMATION_NOT_SET = -2147216625;

                //
                // Zusammenfassung:
                //     Unable to establish existence of the account specified.
                public const int SCHED_E_ACCOUNT_NAME_NOT_FOUND = -2147216624;

                //
                // Zusammenfassung:
                //     Corruption was detected in the Task Scheduler security database; the database
                //     has been reset.
                public const int SCHED_E_ACCOUNT_DBASE_CORRUPT = -2147216623;

                //
                // Zusammenfassung:
                //     Task Scheduler security services are available only on Windows NT operating system.
                public const int SCHED_E_NO_SECURITY_SERVICES = -2147216622;

                //
                // Zusammenfassung:
                //     The task object version is either unsupported or invalid.
                public const int SCHED_E_UNKNOWN_OBJECT_VERSION = -2147216621;

                //
                // Zusammenfassung:
                //     The task has been configured with an unsupported combination of account settings
                //     and run-time options.
                public const int SCHED_E_UNSUPPORTED_ACCOUNT_OPTION = -2147216620;

                //
                // Zusammenfassung:
                //     The Task Scheduler service is not running.
                public const int SCHED_E_SERVICE_NOT_RUNNING = -2147216619;

                //
                // Zusammenfassung:
                //     The task XML contains an unexpected node.
                public const int SCHED_E_UNEXPECTEDNODE = -2147216618;

                //
                // Zusammenfassung:
                //     The task XML contains an element or attribute from an unexpected namespace.
                public const int SCHED_E_NAMESPACE = -2147216617;

                //
                // Zusammenfassung:
                //     The task XML contains a value that is incorrectly formatted or out of range.
                public const int SCHED_E_INVALIDVALUE = -2147216616;

                //
                // Zusammenfassung:
                //     The task XML is missing a required element or attribute.
                public const int SCHED_E_MISSINGNODE = -2147216615;

                //
                // Zusammenfassung:
                //     The task XML is malformed.
                public const int SCHED_E_MALFORMEDXML = -2147216614;

                //
                // Zusammenfassung:
                //     The task XML contains too many nodes of the same type.
                public const int SCHED_E_TOO_MANY_NODES = -2147216611;

                //
                // Zusammenfassung:
                //     The task cannot be started after the trigger's end boundary.
                public const int SCHED_E_PAST_END_BOUNDARY = -2147216610;

                //
                // Zusammenfassung:
                //     An instance of this task is already running.
                public const int SCHED_E_ALREADY_RUNNING = -2147216609;

                //
                // Zusammenfassung:
                //     The task will not run because the user is not logged on.
                public const int SCHED_E_USER_NOT_LOGGED_ON = -2147216608;

                //
                // Zusammenfassung:
                //     The task image is corrupt or has been tampered with.
                public const int SCHED_E_INVALID_TASK_HASH = -2147216607;

                //
                // Zusammenfassung:
                //     The Task Scheduler service is not available.
                public const int SCHED_E_SERVICE_NOT_AVAILABLE = -2147216606;

                //
                // Zusammenfassung:
                //     The Task Scheduler service is too busy to handle your request. Try again later.
                public const int SCHED_E_SERVICE_TOO_BUSY = -2147216605;

                //
                // Zusammenfassung:
                //     The Task Scheduler service attempted to run the task, but the task did not run
                //     due to one of the constraints in the task definition.
                public const int SCHED_E_TASK_ATTEMPTED = -2147216604;

                //
                // Zusammenfassung:
                //     Another single phase resource manager has already been enlisted in this transaction.
                public const int XACT_E_ALREADYOTHERSINGLEPHASE = -2147168256;

                //
                // Zusammenfassung:
                //     A retaining commit or abort is not supported.
                public const int XACT_E_CANTRETAIN = -2147168255;

                //
                // Zusammenfassung:
                //     The transaction failed to commit for an unknown reason. The transaction was aborted.
                public const int XACT_E_COMMITFAILED = -2147168254;

                //
                // Zusammenfassung:
                //     Cannot call commit on this transaction object because the calling application
                //     did not initiate the transaction.
                public const int XACT_E_COMMITPREVENTED = -2147168253;

                //
                // Zusammenfassung:
                //     Instead of committing, the resource heuristically aborted.
                public const int XACT_E_HEURISTICABORT = -2147168252;

                //
                // Zusammenfassung:
                //     Instead of aborting, the resource heuristically committed.
                public const int XACT_E_HEURISTICCOMMIT = -2147168251;

                //
                // Zusammenfassung:
                //     Some of the states of the resource were committed while others were aborted,
                //     likely because of heuristic decisions.
                public const int XACT_E_HEURISTICDAMAGE = -2147168250;

                //
                // Zusammenfassung:
                //     Some of the states of the resource might have been committed while others were
                //     aborted, likely because of heuristic decisions.
                public const int XACT_E_HEURISTICDANGER = -2147168249;

                //
                // Zusammenfassung:
                //     The requested isolation level is not valid or supported.
                public const int XACT_E_ISOLATIONLEVEL = -2147168248;

                //
                // Zusammenfassung:
                //     The transaction manager does not support an asynchronous operation for this method.
                public const int XACT_E_NOASYNC = -2147168247;

                //
                // Zusammenfassung:
                //     Unable to enlist in the transaction.
                public const int XACT_E_NOENLIST = -2147168246;

                //
                // Zusammenfassung:
                //     The requested semantics of retention of isolation across retaining commit and
                //     abort boundaries cannot be supported by this transaction implementation, or isoFlags
                //     was not equal to 0.
                public const int XACT_E_NOISORETAIN = -2147168245;

                //
                // Zusammenfassung:
                //     There is no resource presently associated with this enlistment.
                public const int XACT_E_NORESOURCE = -2147168244;

                //
                // Zusammenfassung:
                //     The transaction failed to commit due to the failure of optimistic concurrency
                //     control in at least one of the resource managers.
                public const int XACT_E_NOTCURRENT = -2147168243;

                //
                // Zusammenfassung:
                //     The transaction has already been implicitly or explicitly committed or aborted.
                public const int XACT_E_NOTRANSACTION = -2147168242;

                //
                // Zusammenfassung:
                //     An invalid combination of flags was specified.
                public const int XACT_E_NOTSUPPORTED = -2147168241;

                //
                // Zusammenfassung:
                //     The resource manager ID is not associated with this transaction or the transaction
                //     manager.
                public const int XACT_E_UNKNOWNRMGRID = -2147168240;

                //
                // Zusammenfassung:
                //     This method was called in the wrong state.
                public const int XACT_E_WRONGSTATE = -2147168239;

                //
                // Zusammenfassung:
                //     The indicated unit of work does not match the unit of work expected by the resource
                //     manager.
                public const int XACT_E_WRONGUOW = -2147168238;

                //
                // Zusammenfassung:
                //     An enlistment in a transaction already exists.
                public const int XACT_E_XTIONEXISTS = -2147168237;

                //
                // Zusammenfassung:
                //     An import object for the transaction could not be found.
                public const int XACT_E_NOIMPORTOBJECT = -2147168236;

                //
                // Zusammenfassung:
                //     The transaction cookie is invalid.
                public const int XACT_E_INVALIDCOOKIE = -2147168235;

                //
                // Zusammenfassung:
                //     The transaction status is in doubt. A communication failure occurred, or a transaction
                //     manager or resource manager has failed.
                public const int XACT_E_INDOUBT = -2147168234;

                //
                // Zusammenfassung:
                //     A time-out was specified, but time-outs are not supported.
                public const int XACT_E_NOTIMEOUT = -2147168233;

                //
                // Zusammenfassung:
                //     The requested operation is already in progress for the transaction.
                public const int XACT_E_ALREADYINPROGRESS = -2147168232;

                //
                // Zusammenfassung:
                //     The transaction has already been aborted.
                public const int XACT_E_ABORTED = -2147168231;

                //
                // Zusammenfassung:
                //     The Transaction Manager returned a log full error.
                public const int XACT_E_LOGFULL = -2147168230;

                //
                // Zusammenfassung:
                //     The transaction manager is not available.
                public const int XACT_E_TMNOTAVAILABLE = -2147168229;

                //
                // Zusammenfassung:
                //     A connection with the transaction manager was lost.
                public const int XACT_E_CONNECTION_DOWN = -2147168228;

                //
                // Zusammenfassung:
                //     A request to establish a connection with the transaction manager was denied.
                public const int XACT_E_CONNECTION_DENIED = -2147168227;

                //
                // Zusammenfassung:
                //     Resource manager reenlistment to determine transaction status timed out.
                public const int XACT_E_REENLISTTIMEOUT = -2147168226;

                //
                // Zusammenfassung:
                //     The transaction manager failed to establish a connection with another Transaction
                //     Internet Protocol (TIP) transaction manager.
                public const int XACT_E_TIP_CONNECT_FAILED = -2147168225;

                //
                // Zusammenfassung:
                //     The transaction manager encountered a protocol error with another TIP transaction
                //     manager.
                public const int XACT_E_TIP_PROTOCOL_ERROR = -2147168224;

                //
                // Zusammenfassung:
                //     The transaction manager could not propagate a transaction from another TIP transaction
                //     manager.
                public const int XACT_E_TIP_PULL_FAILED = -2147168223;

                //
                // Zusammenfassung:
                //     The transaction manager on the destination machine is not available.
                public const int XACT_E_DEST_TMNOTAVAILABLE = -2147168222;

                //
                // Zusammenfassung:
                //     The transaction manager has disabled its support for TIP.
                public const int XACT_E_TIP_DISABLED = -2147168221;

                //
                // Zusammenfassung:
                //     The transaction manager has disabled its support for remote or network transactions.
                public const int XACT_E_NETWORK_TX_DISABLED = -2147168220;

                //
                // Zusammenfassung:
                //     The partner transaction manager has disabled its support for remote or network
                //     transactions.
                public const int XACT_E_PARTNER_NETWORK_TX_DISABLED = -2147168219;

                //
                // Zusammenfassung:
                //     The transaction manager has disabled its support for XA transactions.
                public const int XACT_E_XA_TX_DISABLED = -2147168218;

                //
                // Zusammenfassung:
                //     Microsoft Distributed Transaction Coordinator (MSDTC) was unable to read its
                //     configuration information.
                public const int XACT_E_UNABLE_TO_READ_DTC_CONFIG = -2147168217;

                //
                // Zusammenfassung:
                //     MSDTC was unable to load the DTC proxy DLL.
                public const int XACT_E_UNABLE_TO_LOAD_DTC_PROXY = -2147168216;

                //
                // Zusammenfassung:
                //     The local transaction has aborted.
                public const int XACT_E_ABORTING = -2147168215;

                //
                // Zusammenfassung:
                //     The specified CRM clerk was not found. It might have completed before it could
                //     be held.
                public const int XACT_E_CLERKNOTFOUND = -2147168128;

                //
                // Zusammenfassung:
                //     The specified CRM clerk does not exist.
                public const int XACT_E_CLERKEXISTS = -2147168127;

                //
                // Zusammenfassung:
                //     Recovery of the CRM log file is still in progress.
                public const int XACT_E_RECOVERYINPROGRESS = -2147168126;

                //
                // Zusammenfassung:
                //     The transaction has completed, and the log records have been discarded from the
                //     log file. They are no longer available.
                public const int XACT_E_TRANSACTIONCLOSED = -2147168125;

                //
                // Zusammenfassung:
                //     lsnToRead is outside of the current limits of the log
                public const int XACT_E_INVALIDLSN = -2147168124;

                //
                // Zusammenfassung:
                //     The COM+ Compensating Resource Manager has records it wishes to replay.
                public const int XACT_E_REPLAYREQUEST = -2147168123;

                //
                // Zusammenfassung:
                //     The request to connect to the specified transaction coordinator was denied.
                public const int XACT_E_CONNECTION_REQUEST_DENIED = -2147168000;

                //
                // Zusammenfassung:
                //     The maximum number of enlistments for the specified transaction has been reached.
                public const int XACT_E_TOOMANY_ENLISTMENTS = -2147167999;

                //
                // Zusammenfassung:
                //     A resource manager with the same identifier is already registered with the specified
                //     transaction coordinator.
                public const int XACT_E_DUPLICATE_GUID = -2147167998;

                //
                // Zusammenfassung:
                //     The prepare request given was not eligible for single-phase optimizations.
                public const int XACT_E_NOTSINGLEPHASE = -2147167997;

                //
                // Zusammenfassung:
                //     RecoveryComplete has already been called for the given resource manager.
                public const int XACT_E_RECOVERYALREADYDONE = -2147167996;

                //
                // Zusammenfassung:
                //     The interface call made was incorrect for the current state of the protocol.
                public const int XACT_E_PROTOCOL = -2147167995;

                //
                // Zusammenfassung:
                //     The xa_open call failed for the XA resource.
                public const int XACT_E_RM_FAILURE = -2147167994;

                //
                // Zusammenfassung:
                //     The xa_recover call failed for the XA resource.
                public const int XACT_E_RECOVERY_FAILED = -2147167993;

                //
                // Zusammenfassung:
                //     The logical unit of work specified cannot be found.
                public const int XACT_E_LU_NOT_FOUND = -2147167992;

                //
                // Zusammenfassung:
                //     The specified logical unit of work already exists.
                public const int XACT_E_DUPLICATE_LU = -2147167991;

                //
                // Zusammenfassung:
                //     Subordinate creation failed. The specified logical unit of work was not connected.
                public const int XACT_E_LU_NOT_CONNECTED = -2147167990;

                //
                // Zusammenfassung:
                //     A transaction with the given identifier already exists.
                public const int XACT_E_DUPLICATE_TRANSID = -2147167989;

                //
                // Zusammenfassung:
                //     The resource is in use.
                public const int XACT_E_LU_BUSY = -2147167988;

                //
                // Zusammenfassung:
                //     The LU Recovery process is down.
                public const int XACT_E_LU_NO_RECOVERY_PROCESS = -2147167987;

                //
                // Zusammenfassung:
                //     The remote session was lost.
                public const int XACT_E_LU_DOWN = -2147167986;

                //
                // Zusammenfassung:
                //     The resource is currently recovering.
                public const int XACT_E_LU_RECOVERING = -2147167985;

                //
                // Zusammenfassung:
                //     There was a mismatch in driving recovery.
                public const int XACT_E_LU_RECOVERY_MISMATCH = -2147167984;

                //
                // Zusammenfassung:
                //     An error occurred with the XA resource.
                public const int XACT_E_RM_UNAVAILABLE = -2147167983;

                //
                // Zusammenfassung:
                //     The root transaction wanted to commit, but the transaction aborted.
                public const int CONTEXT_E_ABORTED = -2147164158;

                //
                // Zusammenfassung:
                //     The COM+ component on which the method call was made has a transaction that has
                //     already aborted or is in the process of aborting.
                public const int CONTEXT_E_ABORTING = -2147164157;

                //
                // Zusammenfassung:
                //     There is no Microsoft Transaction Server (MTS) object context.
                public const int CONTEXT_E_NOCONTEXT = -2147164156;

                //
                // Zusammenfassung:
                //     The component is configured to use synchronization, and this method call would
                //     cause a deadlock to occur.
                public const int CONTEXT_E_WOULD_DEADLOCK = -2147164155;

                //
                // Zusammenfassung:
                //     The component is configured to use synchronization, and a thread has timed out
                //     waiting to enter the context.
                public const int CONTEXT_E_SYNCH_TIMEOUT = -2147164154;

                //
                // Zusammenfassung:
                //     You made a method call on a COM+ component that has a transaction that has already
                //     committed or aborted.
                public const int CONTEXT_E_OLDREF = -2147164153;

                //
                // Zusammenfassung:
                //     The specified role was not configured for the application.
                public const int CONTEXT_E_ROLENOTFOUND = -2147164148;

                //
                // Zusammenfassung:
                //     COM+ was unable to talk to the MSDTC.
                public const int CONTEXT_E_TMNOTAVAILABLE = -2147164145;

                //
                // Zusammenfassung:
                //     An unexpected error occurred during COM+ activation.
                public const int CO_E_ACTIVATIONFAILED = -2147164127;

                //
                // Zusammenfassung:
                //     COM+ activation failed. Check the event log for more information.
                public const int CO_E_ACTIVATIONFAILED_EVENTLOGGED = -2147164126;

                //
                // Zusammenfassung:
                //     COM+ activation failed due to a catalog or configuration error.
                public const int CO_E_ACTIVATIONFAILED_CATALOGERROR = -2147164125;

                //
                // Zusammenfassung:
                //     COM+ activation failed because the activation could not be completed in the specified
                //     amount of time.
                public const int CO_E_ACTIVATIONFAILED_TIMEOUT = -2147164124;

                //
                // Zusammenfassung:
                //     COM+ activation failed because an initialization function failed. Check the event
                //     log for more information.
                public const int CO_E_INITIALIZATIONFAILED = -2147164123;

                //
                // Zusammenfassung:
                //     The requested operation requires that just-in-time (JIT) be in the current context,
                //     and it is not.
                public const int CONTEXT_E_NOJIT = -2147164122;

                //
                // Zusammenfassung:
                //     The requested operation requires that the current context have a transaction,
                //     and it does not.
                public const int CONTEXT_E_NOTRANSACTION = -2147164121;

                //
                // Zusammenfassung:
                //     The components threading model has changed after install into a COM+ application.
                //     Re-install component.
                public const int CO_E_THREADINGMODEL_CHANGED = -2147164120;

                //
                // Zusammenfassung:
                //     Internet Information Services (IIS) intrinsics not available. Start your work
                //     with IIS.
                public const int CO_E_NOIISINTRINSICS = -2147164119;

                //
                // Zusammenfassung:
                //     An attempt to write a cookie failed.
                public const int CO_E_NOCOOKIES = -2147164118;

                //
                // Zusammenfassung:
                //     An attempt to use a database generated a database-specific error.
                public const int CO_E_DBERROR = -2147164117;

                //
                // Zusammenfassung:
                //     The COM+ component you created must use object pooling to work.
                public const int CO_E_NOTPOOLED = -2147164116;

                //
                // Zusammenfassung:
                //     The COM+ component you created must use object construction to work correctly.
                public const int CO_E_NOTCONSTRUCTED = -2147164115;

                //
                // Zusammenfassung:
                //     The COM+ component requires synchronization, and it is not configured for it.
                public const int CO_E_NOSYNCHRONIZATION = -2147164114;

                //
                // Zusammenfassung:
                //     The TxIsolation Level property for the COM+ component being created is stronger
                //     than the TxIsolationLevel for the root.
                public const int CO_E_ISOLEVELMISMATCH = -2147164113;

                //
                // Zusammenfassung:
                //     The component attempted to make a cross-context call between invocations of EnterTransactionScope
                //     and ExitTransactionScope. This is not allowed. Cross-context calls cannot be
                //     made while inside a transaction scope.
                public const int CO_E_CALL_OUT_OF_TX_SCOPE_NOT_ALLOWED = -2147164112;

                //
                // Zusammenfassung:
                //     The component made a call to EnterTransactionScope, but did not make a corresponding
                //     call to ExitTransactionScope before returning.
                public const int CO_E_EXIT_TRANSACTION_SCOPE_NOT_CALLED = -2147164111;

                //
                // Zusammenfassung:
                //     General access denied error.
                public const int E_ACCESSDENIED = -2147024891;

                //
                // Zusammenfassung:
                //     The server does not have enough memory for the new channel.
                public const int E_OUTOFMEMORY = -2147024882;

                //
                // Zusammenfassung:
                //     The server cannot support a client request for a dynamic virtual channel.
                public const int ERROR_NOT_SUPPORTED = -2147024846;

                //
                // Zusammenfassung:
                //     One or more arguments are invalid.
                public const int E_INVALIDARG = -2147024809;

                //
                // Zusammenfassung:
                //     There is not enough space on the disk.
                public const int ERROR_DISK_FULL = -2147024784;

                //
                // Zusammenfassung:
                //     Attempt to create a class object failed.
                public const int CO_E_CLASS_CREATE_FAILED = -2146959359;

                //
                // Zusammenfassung:
                //     OLE service could not bind object.
                public const int CO_E_SCM_ERROR = -2146959358;

                //
                // Zusammenfassung:
                //     RPC communication failed with OLE service.
                public const int CO_E_SCM_RPC_FAILURE = -2146959357;

                //
                // Zusammenfassung:
                //     Bad path to object.
                public const int CO_E_BAD_PATH = -2146959356;

                //
                // Zusammenfassung:
                //     Server execution failed.
                public const int CO_E_SERVER_EXEC_FAILURE = -2146959355;

                //
                // Zusammenfassung:
                //     OLE service could not communicate with the object server.
                public const int CO_E_OBJSRV_RPC_FAILURE = -2146959354;

                //
                // Zusammenfassung:
                //     Moniker path could not be normalized.
                public const int MK_E_NO_NORMALIZED = -2146959353;

                //
                // Zusammenfassung:
                //     Object server is stopping when OLE service contacts it.
                public const int CO_E_SERVER_STOPPING = -2146959352;

                //
                // Zusammenfassung:
                //     An invalid root block pointer was specified.
                public const int MEM_E_INVALID_ROOT = -2146959351;

                //
                // Zusammenfassung:
                //     An allocation chain contained an invalid link pointer.
                public const int MEM_E_INVALID_LINK = -2146959344;

                //
                // Zusammenfassung:
                //     The requested allocation size was too large.
                public const int MEM_E_INVALID_SIZE = -2146959343;

                //
                // Zusammenfassung:
                //     The activation requires a display name to be present under the class identifier
                //     (CLSID) key.
                public const int CO_E_MISSING_DISPLAYNAME = -2146959339;

                //
                // Zusammenfassung:
                //     The activation requires that the RunAs value for the application is Activate
                //     As Activator.
                public const int CO_E_RUNAS_VALUE_MUST_BE_AAA = -2146959338;

                //
                // Zusammenfassung:
                //     The class is not configured to support elevated activation.
                public const int CO_E_ELEVATION_DISABLED = -2146959337;

                //
                // Zusammenfassung:
                //     Bad UID.
                public const int NTE_BAD_UID = -2146893823;

                //
                // Zusammenfassung:
                //     Bad hash.
                public const int NTE_BAD_HASH = -2146893822;

                //
                // Zusammenfassung:
                //     Bad key.
                public const int NTE_BAD_KEY = -2146893821;

                //
                // Zusammenfassung:
                //     Bad length.
                public const int NTE_BAD_LEN = -2146893820;

                //
                // Zusammenfassung:
                //     Bad data.
                public const int NTE_BAD_DATA = -2146893819;

                //
                // Zusammenfassung:
                //     Invalid signature.
                public const int NTE_BAD_SIGNATURE = -2146893818;

                //
                // Zusammenfassung:
                //     Bad version of provider.
                public const int NTE_BAD_VER = -2146893817;

                //
                // Zusammenfassung:
                //     Invalid algorithm specified.
                public const int NTE_BAD_ALGID = -2146893816;

                //
                // Zusammenfassung:
                //     Invalid flags specified.
                public const int NTE_BAD_FLAGS = -2146893815;

                //
                // Zusammenfassung:
                //     Invalid type specified.
                public const int NTE_BAD_TYPE = -2146893814;

                //
                // Zusammenfassung:
                //     Key not valid for use in specified state.
                public const int NTE_BAD_KEY_STATE = -2146893813;

                //
                // Zusammenfassung:
                //     Hash not valid for use in specified state.
                public const int NTE_BAD_HASH_STATE = -2146893812;

                //
                // Zusammenfassung:
                //     Key does not exist.
                public const int NTE_NO_KEY = -2146893811;

                //
                // Zusammenfassung:
                //     Insufficient memory available for the operation.
                public const int NTE_NO_MEMORY = -2146893810;

                //
                // Zusammenfassung:
                //     Object already exists.
                public const int NTE_EXISTS = -2146893809;

                //
                // Zusammenfassung:
                //     Access denied.
                public const int NTE_PERM = -2146893808;

                //
                // Zusammenfassung:
                //     Object was not found.
                public const int NTE_NOT_FOUND = -2146893807;

                //
                // Zusammenfassung:
                //     Data already encrypted.
                public const int NTE_DOUBLE_ENCRYPT = -2146893806;

                //
                // Zusammenfassung:
                //     Invalid provider specified.
                public const int NTE_BAD_PROVIDER = -2146893805;

                //
                // Zusammenfassung:
                //     Invalid provider type specified.
                public const int NTE_BAD_PROV_TYPE = -2146893804;

                //
                // Zusammenfassung:
                //     Provider's public key is invalid.
                public const int NTE_BAD_PUBLIC_KEY = -2146893803;

                //
                // Zusammenfassung:
                //     Key set does not exist.
                public const int NTE_BAD_KEYSET = -2146893802;

                //
                // Zusammenfassung:
                //     Provider type not defined.
                public const int NTE_PROV_TYPE_NOT_DEF = -2146893801;

                //
                // Zusammenfassung:
                //     The provider type, as registered, is invalid.
                public const int NTE_PROV_TYPE_ENTRY_BAD = -2146893800;

                //
                // Zusammenfassung:
                //     The key set is not defined.
                public const int NTE_KEYSET_NOT_DEF = -2146893799;

                //
                // Zusammenfassung:
                //     The key set, as registered, is invalid.
                public const int NTE_KEYSET_ENTRY_BAD = -2146893798;

                //
                // Zusammenfassung:
                //     Provider type does not match registered value.
                public const int NTE_PROV_TYPE_NO_MATCH = -2146893797;

                //
                // Zusammenfassung:
                //     The digital signature file is corrupt.
                public const int NTE_SIGNATURE_FILE_BAD = -2146893796;

                //
                // Zusammenfassung:
                //     Provider DLL failed to initialize correctly.
                public const int NTE_PROVIDER_DLL_FAIL = -2146893795;

                //
                // Zusammenfassung:
                //     Provider DLL could not be found.
                public const int NTE_PROV_DLL_NOT_FOUND = -2146893794;

                //
                // Zusammenfassung:
                //     The keyset parameter is invalid.
                public const int NTE_BAD_KEYSET_PARAM = -2146893793;

                //
                // Zusammenfassung:
                //     An internal error occurred.
                public const int NTE_FAIL = -2146893792;

                //
                // Zusammenfassung:
                //     A base error occurred.
                public const int NTE_SYS_ERR = -2146893791;

                //
                // Zusammenfassung:
                //     Provider could not perform the action because the context was acquired as silent.
                public const int NTE_SILENT_CONTEXT = -2146893790;

                //
                // Zusammenfassung:
                //     The security token does not have storage space available for an additional container.
                public const int NTE_TOKEN_KEYSET_STORAGE_FULL = -2146893789;

                //
                // Zusammenfassung:
                //     The profile for the user is a temporary profile.
                public const int NTE_TEMPORARY_PROFILE = -2146893788;

                //
                // Zusammenfassung:
                //     The key parameters could not be set because the configuration service provider
                //     (CSP) uses fixed parameters.
                public const int NTE_FIXEDPARAMETER = -2146893787;

                //
                // Zusammenfassung:
                //     The supplied handle is invalid.
                public const int NTE_INVALID_HANDLE = -2146893786;

                //
                // Zusammenfassung:
                //     The parameter is incorrect.
                public const int NTE_INVALID_PARAMETER = -2146893785;

                //
                // Zusammenfassung:
                //     The buffer supplied to a function was too small.
                public const int NTE_BUFFER_TOO_SMALL = -2146893784;

                //
                // Zusammenfassung:
                //     The requested operation is not supported.
                public const int NTE_NOT_SUPPORTED = -2146893783;

                //
                // Zusammenfassung:
                //     No more data is available.
                public const int NTE_NO_MORE_ITEMS = -2146893782;

                //
                // Zusammenfassung:
                //     The supplied buffers overlap incorrectly.
                public const int NTE_BUFFERS_OVERLAP = -2146893781;

                //
                // Zusammenfassung:
                //     The specified data could not be decrypted.
                public const int NTE_DECRYPTION_FAILURE = -2146893780;

                //
                // Zusammenfassung:
                //     An internal consistency check failed.
                public const int NTE_INTERNAL_ERROR = -2146893779;

                //
                // Zusammenfassung:
                //     This operation requires input from the user.
                public const int NTE_UI_REQUIRED = -2146893778;

                //
                // Zusammenfassung:
                //     The cryptographic provider does not support Hash Message Authentication Code
                //     (HMAC).
                public const int NTE_HMAC_NOT_SUPPORTED = -2146893777;

                //
                // Zusammenfassung:
                //     Not enough memory is available to complete this request.
                public const int SEC_E_INSUFFICIENT_MEMORY = -2146893056;

                //
                // Zusammenfassung:
                //     The handle specified is invalid.
                public const int SEC_E_INVALID_HANDLE = -2146893055;

                //
                // Zusammenfassung:
                //     The function requested is not supported.
                public const int SEC_E_UNSUPPORTED_FUNCTION = -2146893054;

                //
                // Zusammenfassung:
                //     The specified target is unknown or unreachable.
                public const int SEC_E_TARGET_UNKNOWN = -2146893053;

                //
                // Zusammenfassung:
                //     The Local Security Authority (LSA) cannot be contacted.
                public const int SEC_E_INTERNAL_ERROR = -2146893052;

                //
                // Zusammenfassung:
                //     The requested security package does not exist.
                public const int SEC_E_SECPKG_NOT_FOUND = -2146893051;

                //
                // Zusammenfassung:
                //     The caller is not the owner of the desired credentials.
                public const int SEC_E_NOT_OWNER = -2146893050;

                //
                // Zusammenfassung:
                //     The security package failed to initialize and cannot be installed.
                public const int SEC_E_CANNOT_INSTALL = -2146893049;

                //
                // Zusammenfassung:
                //     The token supplied to the function is invalid.
                public const int SEC_E_INVALID_TOKEN = -2146893048;

                //
                // Zusammenfassung:
                //     The security package is not able to marshal the logon buffer, so the logon attempt
                //     has failed.
                public const int SEC_E_CANNOT_PACK = -2146893047;

                //
                // Zusammenfassung:
                //     The per-message quality of protection is not supported by the security package.
                public const int SEC_E_QOP_NOT_SUPPORTED = -2146893046;

                //
                // Zusammenfassung:
                //     The security context does not allow impersonation of the client.
                public const int SEC_E_NO_IMPERSONATION = -2146893045;

                //
                // Zusammenfassung:
                //     The logon attempt failed.
                public const int SEC_E_LOGON_DENIED = -2146893044;

                //
                // Zusammenfassung:
                //     The credentials supplied to the package were not recognized.
                public const int SEC_E_UNKNOWN_CREDENTIALS = -2146893043;

                //
                // Zusammenfassung:
                //     No credentials are available in the security package.
                public const int SEC_E_NO_CREDENTIALS = -2146893042;

                //
                // Zusammenfassung:
                //     The message or signature supplied for verification has been altered.
                public const int SEC_E_MESSAGE_ALTERED = -2146893041;

                //
                // Zusammenfassung:
                //     The message supplied for verification is out of sequence.
                public const int SEC_E_OUT_OF_SEQUENCE = -2146893040;

                //
                // Zusammenfassung:
                //     No authority could be contacted for authentication.
                public const int SEC_E_NO_AUTHENTICATING_AUTHORITY = -2146893039;

                //
                // Zusammenfassung:
                //     The requested security package does not exist.
                public const int SEC_E_BAD_PKGID = -2146893034;

                //
                // Zusammenfassung:
                //     The context has expired and can no longer be used.
                public const int SEC_E_CONTEXT_EXPIRED = -2146893033;

                //
                // Zusammenfassung:
                //     The supplied message is incomplete. The signature was not verified.
                public const int SEC_E_INCOMPLETE_MESSAGE = -2146893032;

                //
                // Zusammenfassung:
                //     The credentials supplied were not complete and could not be verified. The context
                //     could not be initialized.
                public const int SEC_E_INCOMPLETE_CREDENTIALS = -2146893024;

                //
                // Zusammenfassung:
                //     The buffers supplied to a function was too small.
                public const int SEC_E_BUFFER_TOO_SMALL = -2146893023;

                //
                // Zusammenfassung:
                //     The target principal name is incorrect.
                public const int SEC_E_WRONG_PRINCIPAL = -2146893022;

                //
                // Zusammenfassung:
                //     The clocks on the client and server machines are skewed.
                public const int SEC_E_TIME_SKEW = -2146893020;

                //
                // Zusammenfassung:
                //     The certificate chain was issued by an authority that is not trusted.
                public const int SEC_E_UNTRUSTED_ROOT = -2146893019;

                //
                // Zusammenfassung:
                //     The message received was unexpected or badly formatted.
                public const int SEC_E_ILLEGAL_MESSAGE = -2146893018;

                //
                // Zusammenfassung:
                //     An unknown error occurred while processing the certificate.
                public const int SEC_E_CERT_UNKNOWN = -2146893017;

                //
                // Zusammenfassung:
                //     The received certificate has expired.
                public const int SEC_E_CERT_EXPIRED = -2146893016;

                //
                // Zusammenfassung:
                //     The specified data could not be encrypted.
                public const int SEC_E_ENCRYPT_FAILURE = -2146893015;

                //
                // Zusammenfassung:
                //     The specified data could not be decrypted.
                public const int SEC_E_DECRYPT_FAILURE = -2146893008;

                //
                // Zusammenfassung:
                //     The client and server cannot communicate because they do not possess a common
                //     algorithm.
                public const int SEC_E_ALGORITHM_MISMATCH = -2146893007;

                //
                // Zusammenfassung:
                //     The security context could not be established due to a failure in the requested
                //     quality of service (for example, mutual authentication or delegation).
                public const int SEC_E_SECURITY_QOS_FAILED = -2146893006;

                //
                // Zusammenfassung:
                //     A security context was deleted before the context was completed. This is considered
                //     a logon failure.
                public const int SEC_E_UNFINISHED_CONTEXT_DELETED = -2146893005;

                //
                // Zusammenfassung:
                //     The client is trying to negotiate a context and the server requires user-to-user
                //     but did not send a ticket granting ticket (TGT) reply.
                public const int SEC_E_NO_TGT_REPLY = -2146893004;

                //
                // Zusammenfassung:
                //     Unable to accomplish the requested task because the local machine does not have
                //     an IP addresses.
                public const int SEC_E_NO_IP_ADDRESSES = -2146893003;

                //
                // Zusammenfassung:
                //     The supplied credential handle does not match the credential associated with
                //     the security context.
                public const int SEC_E_WRONG_CREDENTIAL_HANDLE = -2146893002;

                //
                // Zusammenfassung:
                //     The cryptographic system or checksum function is invalid because a required function
                //     is unavailable.
                public const int SEC_E_CRYPTO_SYSTEM_INVALID = -2146893001;

                //
                // Zusammenfassung:
                //     The number of maximum ticket referrals has been exceeded.
                public const int SEC_E_MAX_REFERRALS_EXCEEDED = -2146893000;

                //
                // Zusammenfassung:
                //     The local machine must be a Kerberos domain controller (KDC), and it is not.
                public const int SEC_E_MUST_BE_KDC = -2146892999;

                //
                // Zusammenfassung:
                //     The other end of the security negotiation requires strong cryptographics, but
                //     it is not supported on the local machine.
                public const int SEC_E_STRONG_CRYPTO_NOT_SUPPORTED = -2146892998;

                //
                // Zusammenfassung:
                //     The KDC reply contained more than one principal name.
                public const int SEC_E_TOO_MANY_PRINCIPALS = -2146892997;

                //
                // Zusammenfassung:
                //     Expected to find PA data for a hint of what etype to use, but it was not found.
                public const int SEC_E_NO_PA_DATA = -2146892996;

                //
                // Zusammenfassung:
                //     The client certificate does not contain a valid user principal name (UPN), or
                //     does not match the client name in the logon request. Contact your administrator.
                public const int SEC_E_PKINIT_NAME_MISMATCH = -2146892995;

                //
                // Zusammenfassung:
                //     Smart card logon is required and was not used.
                public const int SEC_E_SMARTCARD_LOGON_REQUIRED = -2146892994;

                //
                // Zusammenfassung:
                //     A system shutdown is in progress.
                public const int SEC_E_SHUTDOWN_IN_PROGRESS = -2146892993;

                //
                // Zusammenfassung:
                //     An invalid request was sent to the KDC.
                public const int SEC_E_KDC_INVALID_REQUEST = -2146892992;

                //
                // Zusammenfassung:
                //     The KDC was unable to generate a referral for the service requested.
                public const int SEC_E_KDC_UNABLE_TO_REFER = -2146892991;

                //
                // Zusammenfassung:
                //     The encryption type requested is not supported by the KDC.
                public const int SEC_E_KDC_UNKNOWN_ETYPE = -2146892990;

                //
                // Zusammenfassung:
                //     An unsupported pre-authentication mechanism was presented to the Kerberos package.
                public const int SEC_E_UNSUPPORTED_PREAUTH = -2146892989;

                //
                // Zusammenfassung:
                //     The requested operation cannot be completed. The computer must be trusted for
                //     delegation, and the current user account must be configured to allow delegation.
                public const int SEC_E_DELEGATION_REQUIRED = -2146892987;

                //
                // Zusammenfassung:
                //     Client's supplied Security Support Provider Interface (SSPI) channel bindings
                //     were incorrect.
                public const int SEC_E_BAD_BINDINGS = -2146892986;

                //
                // Zusammenfassung:
                //     The received certificate was mapped to multiple accounts.
                public const int SEC_E_MULTIPLE_ACCOUNTS = -2146892985;

                //
                // Zusammenfassung:
                //     No Kerberos key was found.
                public const int SEC_E_NO_KERB_KEY = -2146892984;

                //
                // Zusammenfassung:
                //     The certificate is not valid for the requested usage.
                public const int SEC_E_CERT_WRONG_USAGE = -2146892983;

                //
                // Zusammenfassung:
                //     The system detected a possible attempt to compromise security. Ensure that you
                //     can contact the server that authenticated you.
                public const int SEC_E_DOWNGRADE_DETECTED = -2146892976;

                //
                // Zusammenfassung:
                //     The smart card certificate used for authentication has been revoked. Contact
                //     your system administrator. The event log might contain additional information.
                public const int SEC_E_SMARTCARD_CERT_REVOKED = -2146892975;

                //
                // Zusammenfassung:
                //     An untrusted certification authority (CA) was detected while processing the smart
                //     card certificate used for authentication. Contact your system administrator.
                public const int SEC_E_ISSUING_CA_UNTRUSTED = -2146892974;

                //
                // Zusammenfassung:
                //     The revocation status of the smart card certificate used for authentication could
                //     not be determined. Contact your system administrator.
                public const int SEC_E_REVOCATION_OFFLINE_C = -2146892973;

                //
                // Zusammenfassung:
                //     The smart card certificate used for authentication was not trusted. Contact your
                //     system administrator.
                public const int SEC_E_PKINIT_CLIENT_FAILURE = -2146892972;

                //
                // Zusammenfassung:
                //     The smart card certificate used for authentication has expired. Contact your
                //     system administrator.
                public const int SEC_E_SMARTCARD_CERT_EXPIRED = -2146892971;

                //
                // Zusammenfassung:
                //     The Kerberos subsystem encountered an error. A service for user protocol requests
                //     was made against a domain controller that does not support services for users.
                public const int SEC_E_NO_S4U_PROT_SUPPORT = -2146892970;

                //
                // Zusammenfassung:
                //     An attempt was made by this server to make a Kerberos-constrained delegation
                //     request for a target outside the server's realm. This is not supported and indicates
                //     a misconfiguration on this server's allowed-to-delegate-to list. Contact your
                //     administrator.
                public const int SEC_E_CROSSREALM_DELEGATION_FAILURE = -2146892969;

                //
                // Zusammenfassung:
                //     The revocation status of the domain controller certificate used for smart card
                //     authentication could not be determined. The system event log contains additional
                //     information. Contact your system administrator.
                public const int SEC_E_REVOCATION_OFFLINE_KDC = -2146892968;

                //
                // Zusammenfassung:
                //     An untrusted CA was detected while processing the domain controller certificate
                //     used for authentication. The system event log contains additional information.
                //     Contact your system administrator.
                public const int SEC_E_ISSUING_CA_UNTRUSTED_KDC = -2146892967;

                //
                // Zusammenfassung:
                //     The domain controller certificate used for smart card logon has expired. Contact
                //     your system administrator with the contents of your system event log.
                public const int SEC_E_KDC_CERT_EXPIRED = -2146892966;

                //
                // Zusammenfassung:
                //     The domain controller certificate used for smart card logon has been revoked.
                //     Contact your system administrator with the contents of your system event log.
                public const int SEC_E_KDC_CERT_REVOKED = -2146892965;

                //
                // Zusammenfassung:
                //     One or more of the parameters passed to the function were invalid.
                public const int SEC_E_INVALID_PARAMETER = -2146892963;

                //
                // Zusammenfassung:
                //     The client policy does not allow credential delegation to the target server.
                public const int SEC_E_DELEGATION_POLICY = -2146892962;

                //
                // Zusammenfassung:
                //     The client policy does not allow credential delegation to the target server with
                //     NLTM only authentication.
                public const int SEC_E_POLICY_NLTM_ONLY = -2146892961;

                //
                // Zusammenfassung:
                //     An error occurred while performing an operation on a cryptographic message.
                public const int CRYPT_E_MSG_ERROR = -2146889727;

                //
                // Zusammenfassung:
                //     Unknown cryptographic algorithm.
                public const int CRYPT_E_UNKNOWN_ALGO = -2146889726;

                //
                // Zusammenfassung:
                //     The object identifier is poorly formatted.
                public const int CRYPT_E_OID_FORMAT = -2146889725;

                //
                // Zusammenfassung:
                //     Invalid cryptographic message type.
                public const int CRYPT_E_INVALID_MSG_TYPE = -2146889724;

                //
                // Zusammenfassung:
                //     Unexpected cryptographic message encoding.
                public const int CRYPT_E_UNEXPECTED_ENCODING = -2146889723;

                //
                // Zusammenfassung:
                //     The cryptographic message does not contain an expected authenticated attribute.
                public const int CRYPT_E_AUTH_ATTR_MISSING = -2146889722;

                //
                // Zusammenfassung:
                //     The hash value is not correct.
                public const int CRYPT_E_HASH_VALUE = -2146889721;

                //
                // Zusammenfassung:
                //     The index value is not valid.
                public const int CRYPT_E_INVALID_INDEX = -2146889720;

                //
                // Zusammenfassung:
                //     The content of the cryptographic message has already been decrypted.
                public const int CRYPT_E_ALREADY_DECRYPTED = -2146889719;

                //
                // Zusammenfassung:
                //     The content of the cryptographic message has not been decrypted yet.
                public const int CRYPT_E_NOT_DECRYPTED = -2146889718;

                //
                // Zusammenfassung:
                //     The enveloped-data message does not contain the specified recipient.
                public const int CRYPT_E_RECIPIENT_NOT_FOUND = -2146889717;

                //
                // Zusammenfassung:
                //     Invalid control type.
                public const int CRYPT_E_CONTROL_TYPE = -2146889716;

                //
                // Zusammenfassung:
                //     Invalid issuer or serial number.
                public const int CRYPT_E_ISSUER_SERIALNUMBER = -2146889715;

                //
                // Zusammenfassung:
                //     Cannot find the original signer.
                public const int CRYPT_E_SIGNER_NOT_FOUND = -2146889714;

                //
                // Zusammenfassung:
                //     The cryptographic message does not contain all of the requested attributes.
                public const int CRYPT_E_ATTRIBUTES_MISSING = -2146889713;

                //
                // Zusammenfassung:
                //     The streamed cryptographic message is not ready to return data.
                public const int CRYPT_E_STREAM_MSG_NOT_READY = -2146889712;

                //
                // Zusammenfassung:
                //     The streamed cryptographic message requires more data to complete the decode
                //     operation.
                public const int CRYPT_E_STREAM_INSUFFICIENT_DATA = -2146889711;

                //
                // Zusammenfassung:
                //     The length specified for the output data was insufficient.
                public const int CRYPT_E_BAD_LEN = -2146885631;

                //
                // Zusammenfassung:
                //     An error occurred during the encode or decode operation.
                public const int CRYPT_E_BAD_ENCODE = -2146885630;

                //
                // Zusammenfassung:
                //     An error occurred while reading or writing to a file.
                public const int CRYPT_E_FILE_ERROR = -2146885629;

                //
                // Zusammenfassung:
                //     Cannot find object or property.
                public const int CRYPT_E_NOT_FOUND = -2146885628;

                //
                // Zusammenfassung:
                //     The object or property already exists.
                public const int CRYPT_E_EXISTS = -2146885627;

                //
                // Zusammenfassung:
                //     No provider was specified for the store or object.
                public const int CRYPT_E_NO_PROVIDER = -2146885626;

                //
                // Zusammenfassung:
                //     The specified certificate is self-signed.
                public const int CRYPT_E_SELF_SIGNED = -2146885625;

                //
                // Zusammenfassung:
                //     The previous certificate or certificate revocation list (CRL) context was deleted.
                public const int CRYPT_E_DELETED_PREV = -2146885624;

                //
                // Zusammenfassung:
                //     Cannot find the requested object.
                public const int CRYPT_E_NO_MATCH = -2146885623;

                //
                // Zusammenfassung:
                //     The certificate does not have a property that references a private key.
                public const int CRYPT_E_UNEXPECTED_MSG_TYPE = -2146885622;

                //
                // Zusammenfassung:
                //     Cannot find the certificate and private key for decryption.
                public const int CRYPT_E_NO_KEY_PROPERTY = -2146885621;

                //
                // Zusammenfassung:
                //     Cannot find the certificate and private key to use for decryption.
                public const int CRYPT_E_NO_DECRYPT_CERT = -2146885620;

                //
                // Zusammenfassung:
                //     Not a cryptographic message or the cryptographic message is not formatted correctly.
                public const int CRYPT_E_BAD_MSG = -2146885619;

                //
                // Zusammenfassung:
                //     The signed cryptographic message does not have a signer for the specified signer
                //     index.
                public const int CRYPT_E_NO_SIGNER = -2146885618;

                //
                // Zusammenfassung:
                //     Final closure is pending until additional frees or closes.
                public const int CRYPT_E_PENDING_CLOSE = -2146885617;

                //
                // Zusammenfassung:
                //     The certificate is revoked.
                public const int CRYPT_E_REVOKED = -2146885616;

                //
                // Zusammenfassung:
                //     No DLL or exported function was found to verify revocation.
                public const int CRYPT_E_NO_REVOCATION_DLL = -2146885615;

                //
                // Zusammenfassung:
                //     The revocation function was unable to check revocation for the certificate.
                public const int CRYPT_E_NO_REVOCATION_CHECK = -2146885614;

                //
                // Zusammenfassung:
                //     The revocation function was unable to check revocation because the revocation
                //     server was offline.
                public const int CRYPT_E_REVOCATION_OFFLINE = -2146885613;

                //
                // Zusammenfassung:
                //     The certificate is not in the revocation server's database.
                public const int CRYPT_E_NOT_IN_REVOCATION_DATABASE = -2146885612;

                //
                // Zusammenfassung:
                //     The string contains a non-numeric character.
                public const int CRYPT_E_INVALID_NUMERIC_STRING = -2146885600;

                //
                // Zusammenfassung:
                //     The string contains a nonprintable character.
                public const int CRYPT_E_INVALID_PRINTABLE_STRING = -2146885599;

                //
                // Zusammenfassung:
                //     The string contains a character not in the 7-bit ASCII character set.
                public const int CRYPT_E_INVALID_IA5_STRING = -2146885598;

                //
                // Zusammenfassung:
                //     The string contains an invalid X500 name attribute key, object identifier (OID),
                //     value, or delimiter.
                public const int CRYPT_E_INVALID_X500_STRING = -2146885597;

                //
                // Zusammenfassung:
                //     The dwValueType for the CERT_NAME_VALUE is not one of the character strings.
                //     Most likely it is either a CERT_RDN_ENCODED_BLOB or CERT_TDN_OCTED_STRING.
                public const int CRYPT_E_NOT_CHAR_STRING = -2146885596;

                //
                // Zusammenfassung:
                //     The Put operation cannot continue. The file needs to be resized. However, there
                //     is already a signature present. A complete signing operation must be done.
                public const int CRYPT_E_FILERESIZED = -2146885595;

                //
                // Zusammenfassung:
                //     The cryptographic operation failed due to a local security option setting.
                public const int CRYPT_E_SECURITY_SETTINGS = -2146885594;

                //
                // Zusammenfassung:
                //     No DLL or exported function was found to verify subject usage.
                public const int CRYPT_E_NO_VERIFY_USAGE_DLL = -2146885593;

                //
                // Zusammenfassung:
                //     The called function was unable to perform a usage check on the subject.
                public const int CRYPT_E_NO_VERIFY_USAGE_CHECK = -2146885592;

                //
                // Zusammenfassung:
                //     The called function was unable to complete the usage check because the server
                //     was offline.
                public const int CRYPT_E_VERIFY_USAGE_OFFLINE = -2146885591;

                //
                // Zusammenfassung:
                //     The subject was not found in a certificate trust list (CTL).
                public const int CRYPT_E_NOT_IN_CTL = -2146885590;

                //
                // Zusammenfassung:
                //     None of the signers of the cryptographic message or certificate trust list is
                //     trusted.
                public const int CRYPT_E_NO_TRUSTED_SIGNER = -2146885589;

                //
                // Zusammenfassung:
                //     The public key's algorithm parameters are missing.
                public const int CRYPT_E_MISSING_PUBKEY_PARA = -2146885588;

                //
                // Zusammenfassung:
                //     OSS Certificate encode/decode error code base.
                public const int CRYPT_E_OSS_ERROR = -2146881536;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Output Buffer is too small.
                public const int OSS_MORE_BUF = -2146881535;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Signed integer is encoded as a unsigned integer.
                public const int OSS_NEGATIVE_UINTEGER = -2146881534;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Unknown ASN.1 data type.
                public const int OSS_PDU_RANGE = -2146881533;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Output buffer is too small; the decoded data has been truncated.
                public const int OSS_MORE_INPUT = -2146881532;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Invalid data.
                public const int OSS_DATA_ERROR = -2146881531;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Invalid argument.
                public const int OSS_BAD_ARG = -2146881530;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Encode/Decode version mismatch.
                public const int OSS_BAD_VERSION = -2146881529;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Out of memory.
                public const int OSS_OUT_MEMORY = -2146881528;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Encode/Decode error.
                public const int OSS_PDU_MISMATCH = -2146881527;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Internal error.
                public const int OSS_LIMITED = -2146881526;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Invalid data.
                public const int OSS_BAD_PTR = -2146881525;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Invalid data.
                public const int OSS_BAD_TIME = -2146881524;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Unsupported BER indefinite-length encoding.
                public const int OSS_INDEFINITE_NOT_SUPPORTED = -2146881523;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Access violation.
                public const int OSS_MEM_ERROR = -2146881522;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Invalid data.
                public const int OSS_BAD_TABLE = -2146881521;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Invalid data.
                public const int OSS_TOO_LONG = -2146881520;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Invalid data.
                public const int OSS_CONSTRAINT_VIOLATED = -2146881519;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Internal error.
                public const int OSS_FATAL_ERROR = -2146881518;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Multithreading conflict.
                public const int OSS_ACCESS_SERIALIZATION_ERROR = -2146881517;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Invalid data.
                public const int OSS_NULL_TBL = -2146881516;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Invalid data.
                public const int OSS_NULL_FCN = -2146881515;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Invalid data.
                public const int OSS_BAD_ENCRULES = -2146881514;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Encode/Decode function not implemented.
                public const int OSS_UNAVAIL_ENCRULES = -2146881513;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Trace file error.
                public const int OSS_CANT_OPEN_TRACE_WINDOW = -2146881512;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Function not implemented.
                public const int OSS_UNIMPLEMENTED = -2146881511;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Program link error.
                public const int OSS_OID_DLL_NOT_LINKED = -2146881510;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Trace file error.
                public const int OSS_CANT_OPEN_TRACE_FILE = -2146881509;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Trace file error.
                public const int OSS_TRACE_FILE_ALREADY_OPEN = -2146881508;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Invalid data.
                public const int OSS_TABLE_MISMATCH = -2146881507;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Invalid data.
                public const int OSS_TYPE_NOT_SUPPORTED = -2146881506;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Program link error.
                public const int OSS_REAL_DLL_NOT_LINKED = -2146881505;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Program link error.
                public const int OSS_REAL_CODE_NOT_LINKED = -2146881504;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Program link error.
                public const int OSS_OUT_OF_RANGE = -2146881503;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Program link error.
                public const int OSS_COPIER_DLL_NOT_LINKED = -2146881502;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Program link error.
                public const int OSS_CONSTRAINT_DLL_NOT_LINKED = -2146881501;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Program link error.
                public const int OSS_COMPARATOR_DLL_NOT_LINKED = -2146881500;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Program link error.
                public const int OSS_COMPARATOR_CODE_NOT_LINKED = -2146881499;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Program link error.
                public const int OSS_MEM_MGR_DLL_NOT_LINKED = -2146881498;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Program link error.
                public const int OSS_PDV_DLL_NOT_LINKED = -2146881497;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Program link error.
                public const int OSS_PDV_CODE_NOT_LINKED = -2146881496;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Program link error.
                public const int OSS_API_DLL_NOT_LINKED = -2146881495;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Program link error.
                public const int OSS_BERDER_DLL_NOT_LINKED = -2146881494;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Program link error.
                public const int OSS_PER_DLL_NOT_LINKED = -2146881493;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Program link error.
                public const int OSS_OPEN_TYPE_ERROR = -2146881492;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: System resource error.
                public const int OSS_MUTEX_NOT_CREATED = -2146881491;

                //
                // Zusammenfassung:
                //     OSS ASN.1 Error: Trace file error.
                public const int OSS_CANT_CLOSE_TRACE_FILE = -2146881490;

                //
                // Zusammenfassung:
                //     ASN1 Certificate encode/decode error code base.
                public const int CRYPT_E_ASN1_ERROR = -2146881280;

                //
                // Zusammenfassung:
                //     ASN1 internal encode or decode error.
                public const int CRYPT_E_ASN1_INTERNAL = -2146881279;

                //
                // Zusammenfassung:
                //     ASN1 unexpected end of data.
                public const int CRYPT_E_ASN1_EOD = -2146881278;

                //
                // Zusammenfassung:
                //     ASN1 corrupted data.
                public const int CRYPT_E_ASN1_CORRUPT = -2146881277;

                //
                // Zusammenfassung:
                //     ASN1 value too large.
                public const int CRYPT_E_ASN1_LARGE = -2146881276;

                //
                // Zusammenfassung:
                //     ASN1 constraint violated.
                public const int CRYPT_E_ASN1_CONSTRAINT = -2146881275;

                //
                // Zusammenfassung:
                //     ASN1 out of memory.
                public const int CRYPT_E_ASN1_MEMORY = -2146881274;

                //
                // Zusammenfassung:
                //     ASN1 buffer overflow.
                public const int CRYPT_E_ASN1_OVERFLOW = -2146881273;

                //
                // Zusammenfassung:
                //     ASN1 function not supported for this protocol data unit (PDU).
                public const int CRYPT_E_ASN1_BADPDU = -2146881272;

                //
                // Zusammenfassung:
                //     ASN1 bad arguments to function call.
                public const int CRYPT_E_ASN1_BADARGS = -2146881271;

                //
                // Zusammenfassung:
                //     ASN1 bad real value.
                public const int CRYPT_E_ASN1_BADREAL = -2146881270;

                //
                // Zusammenfassung:
                //     ASN1 bad tag value met.
                public const int CRYPT_E_ASN1_BADTAG = -2146881269;

                //
                // Zusammenfassung:
                //     ASN1 bad choice value.
                public const int CRYPT_E_ASN1_CHOICE = -2146881268;

                //
                // Zusammenfassung:
                //     ASN1 bad encoding rule.
                public const int CRYPT_E_ASN1_RULE = -2146881267;

                //
                // Zusammenfassung:
                //     ASN1 bad Unicode (UTF8).
                public const int CRYPT_E_ASN1_UTF8 = -2146881266;

                //
                // Zusammenfassung:
                //     ASN1 bad PDU type.
                public const int CRYPT_E_ASN1_PDU_TYPE = -2146881229;

                //
                // Zusammenfassung:
                //     ASN1 not yet implemented.
                public const int CRYPT_E_ASN1_NYI = -2146881228;

                //
                // Zusammenfassung:
                //     ASN1 skipped unknown extensions.
                public const int CRYPT_E_ASN1_EXTENDED = -2146881023;

                //
                // Zusammenfassung:
                //     ASN1 end of data expected.
                public const int CRYPT_E_ASN1_NOEOD = -2146881022;

                //
                // Zusammenfassung:
                //     The request subject name is invalid or too long.
                public const int CERTSRV_E_BAD_REQUESTSUBJECT = -2146877439;

                //
                // Zusammenfassung:
                //     The request does not exist.
                public const int CERTSRV_E_NO_REQUEST = -2146877438;

                //
                // Zusammenfassung:
                //     The request's current status does not allow this operation.
                public const int CERTSRV_E_BAD_REQUESTSTATUS = -2146877437;

                //
                // Zusammenfassung:
                //     The requested property value is empty.
                public const int CERTSRV_E_PROPERTY_EMPTY = -2146877436;

                //
                // Zusammenfassung:
                //     The CA's certificate contains invalid data.
                public const int CERTSRV_E_INVALID_CA_CERTIFICATE = -2146877435;

                //
                // Zusammenfassung:
                //     Certificate service has been suspended for a database restore operation.
                public const int CERTSRV_E_SERVER_SUSPENDED = -2146877434;

                //
                // Zusammenfassung:
                //     The certificate contains an encoded length that is potentially incompatible with
                //     older enrollment software.
                public const int CERTSRV_E_ENCODING_LENGTH = -2146877433;

                //
                // Zusammenfassung:
                //     The operation is denied. The user has multiple roles assigned, and the CA is
                //     configured to enforce role separation.
                public const int CERTSRV_E_ROLECONFLICT = -2146877432;

                //
                // Zusammenfassung:
                //     The operation is denied. It can only be performed by a certificate manager that
                //     is allowed to manage certificates for the current requester.
                public const int CERTSRV_E_RESTRICTEDOFFICER = -2146877431;

                //
                // Zusammenfassung:
                //     Cannot archive private key. The CA is not configured for key archival.
                public const int CERTSRV_E_KEY_ARCHIVAL_NOT_CONFIGURED = -2146877430;

                //
                // Zusammenfassung:
                //     Cannot archive private key. The CA could not verify one or more key recovery
                //     certificates.
                public const int CERTSRV_E_NO_VALID_KRA = -2146877429;

                //
                // Zusammenfassung:
                //     The request is incorrectly formatted. The encrypted private key must be in an
                //     unauthenticated attribute in an outermost signature.
                public const int CERTSRV_E_BAD_REQUEST_KEY_ARCHIVAL = -2146877428;

                //
                // Zusammenfassung:
                //     At least one security principal must have the permission to manage this CA.
                public const int CERTSRV_E_NO_CAADMIN_DEFINED = -2146877427;

                //
                // Zusammenfassung:
                //     The request contains an invalid renewal certificate attribute.
                public const int CERTSRV_E_BAD_RENEWAL_CERT_ATTRIBUTE = -2146877426;

                //
                // Zusammenfassung:
                //     An attempt was made to open a CA database session, but there are already too
                //     many active sessions. The server needs to be configured to allow additional sessions.
                public const int CERTSRV_E_NO_DB_SESSIONS = -2146877425;

                //
                // Zusammenfassung:
                //     A memory reference caused a data alignment fault.
                public const int CERTSRV_E_ALIGNMENT_FAULT = -2146877424;

                //
                // Zusammenfassung:
                //     The permissions on this CA do not allow the current user to enroll for certificates.
                public const int CERTSRV_E_ENROLL_DENIED = -2146877423;

                //
                // Zusammenfassung:
                //     The permissions on the certificate template do not allow the current user to
                //     enroll for this type of certificate.
                public const int CERTSRV_E_TEMPLATE_DENIED = -2146877422;

                //
                // Zusammenfassung:
                //     The contacted domain controller cannot support signed Lightweight Directory Access
                //     Protocol (LDAP) traffic. Update the domain controller or configure Certificate
                //     Services to use SSL for Active Directory access.
                public const int CERTSRV_E_DOWNLEVEL_DC_SSL_OR_UPGRADE = -2146877421;

                //
                // Zusammenfassung:
                //     The requested certificate template is not supported by this CA.
                public const int CERTSRV_E_UNSUPPORTED_CERT_TYPE = -2146875392;

                //
                // Zusammenfassung:
                //     The request contains no certificate template information.
                public const int CERTSRV_E_NO_CERT_TYPE = -2146875391;

                //
                // Zusammenfassung:
                //     The request contains conflicting template information.
                public const int CERTSRV_E_TEMPLATE_CONFLICT = -2146875390;

                //
                // Zusammenfassung:
                //     The request is missing a required Subject Alternate name extension.
                public const int CERTSRV_E_SUBJECT_ALT_NAME_REQUIRED = -2146875389;

                //
                // Zusammenfassung:
                //     The request is missing a required private key for archival by the server.
                public const int CERTSRV_E_ARCHIVED_KEY_REQUIRED = -2146875388;

                //
                // Zusammenfassung:
                //     The request is missing a required SMIME capabilities extension.
                public const int CERTSRV_E_SMIME_REQUIRED = -2146875387;

                //
                // Zusammenfassung:
                //     The request was made on behalf of a subject other than the caller. The certificate
                //     template must be configured to require at least one signature to authorize the
                //     request.
                public const int CERTSRV_E_BAD_RENEWAL_SUBJECT = -2146875386;

                //
                // Zusammenfassung:
                //     The request template version is newer than the supported template version.
                public const int CERTSRV_E_BAD_TEMPLATE_VERSION = -2146875385;

                //
                // Zusammenfassung:
                //     The template is missing a required signature policy attribute.
                public const int CERTSRV_E_TEMPLATE_POLICY_REQUIRED = -2146875384;

                //
                // Zusammenfassung:
                //     The request is missing required signature policy information.
                public const int CERTSRV_E_SIGNATURE_POLICY_REQUIRED = -2146875383;

                //
                // Zusammenfassung:
                //     The request is missing one or more required signatures.
                public const int CERTSRV_E_SIGNATURE_COUNT = -2146875382;

                //
                // Zusammenfassung:
                //     One or more signatures did not include the required application or issuance policies.
                //     The request is missing one or more required valid signatures.
                public const int CERTSRV_E_SIGNATURE_REJECTED = -2146875381;

                //
                // Zusammenfassung:
                //     The request is missing one or more required signature issuance policies.
                public const int CERTSRV_E_ISSUANCE_POLICY_REQUIRED = -2146875380;

                //
                // Zusammenfassung:
                //     The UPN is unavailable and cannot be added to the Subject Alternate name.
                public const int CERTSRV_E_SUBJECT_UPN_REQUIRED = -2146875379;

                //
                // Zusammenfassung:
                //     The Active Directory GUID is unavailable and cannot be added to the Subject Alternate
                //     name.
                public const int CERTSRV_E_SUBJECT_DIRECTORY_GUID_REQUIRED = -2146875378;

                //
                // Zusammenfassung:
                //     The Domain Name System (DNS) name is unavailable and cannot be added to the Subject
                //     Alternate name.
                public const int CERTSRV_E_SUBJECT_DNS_REQUIRED = -2146875377;

                //
                // Zusammenfassung:
                //     The request includes a private key for archival by the server, but key archival
                //     is not enabled for the specified certificate template.
                public const int CERTSRV_E_ARCHIVED_KEY_UNEXPECTED = -2146875376;

                //
                // Zusammenfassung:
                //     The public key does not meet the minimum size required by the specified certificate
                //     template.
                public const int CERTSRV_E_KEY_LENGTH = -2146875375;

                //
                // Zusammenfassung:
                //     The email name is unavailable and cannot be added to the Subject or Subject Alternate
                //     name.
                public const int CERTSRV_E_SUBJECT_EMAIL_REQUIRED = -2146875374;

                //
                // Zusammenfassung:
                //     One or more certificate templates to be enabled on this CA could not be found.
                public const int CERTSRV_E_UNKNOWN_CERT_TYPE = -2146875373;

                //
                // Zusammenfassung:
                //     The certificate template renewal period is longer than the certificate validity
                //     period. The template should be reconfigured or the CA certificate renewed.
                public const int CERTSRV_E_CERT_TYPE_OVERLAP = -2146875372;

                //
                // Zusammenfassung:
                //     The certificate template requires too many return authorization (RA) signatures.
                //     Only one RA signature is allowed.
                public const int CERTSRV_E_TOO_MANY_SIGNATURES = -2146875371;

                //
                // Zusammenfassung:
                //     The key used in a renewal request does not match one of the certificates being
                //     renewed.
                public const int CERTSRV_E_RENEWAL_BAD_PUBLIC_KEY = -2146875370;

                //
                // Zusammenfassung:
                //     The endorsement key certificate is not valid.
                public const int CERTSRV_E_INVALID_EK = -2146875369;

                //
                // Zusammenfassung:
                //     Key attestation did not succeed.
                public const int CERTSRV_E_KEY_ATTESTATION = -2146875366;

                //
                // Zusammenfassung:
                //     The key is not exportable.
                public const int XENROLL_E_KEY_NOT_EXPORTABLE = -2146873344;

                //
                // Zusammenfassung:
                //     You cannot add the root CA certificate into your local store.
                public const int XENROLL_E_CANNOT_ADD_ROOT_CERT = -2146873343;

                //
                // Zusammenfassung:
                //     The key archival hash attribute was not found in the response.
                public const int XENROLL_E_RESPONSE_KA_HASH_NOT_FOUND = -2146873342;

                //
                // Zusammenfassung:
                //     An unexpected key archival hash attribute was found in the response.
                public const int XENROLL_E_RESPONSE_UNEXPECTED_KA_HASH = -2146873341;

                //
                // Zusammenfassung:
                //     There is a key archival hash mismatch between the request and the response.
                public const int XENROLL_E_RESPONSE_KA_HASH_MISMATCH = -2146873340;

                //
                // Zusammenfassung:
                //     Signing certificate cannot include SMIME extension.
                public const int XENROLL_E_KEYSPEC_SMIME_MISMATCH = -2146873339;

                //
                // Zusammenfassung:
                //     A system-level error occurred while verifying trust.
                public const int TRUST_E_SYSTEM_ERROR = -2146869247;

                //
                // Zusammenfassung:
                //     The certificate for the signer of the message is invalid or not found.
                public const int TRUST_E_NO_SIGNER_CERT = -2146869246;

                //
                // Zusammenfassung:
                //     One of the counter signatures was invalid.
                public const int TRUST_E_COUNTER_SIGNER = -2146869245;

                //
                // Zusammenfassung:
                //     The signature of the certificate cannot be verified.
                public const int TRUST_E_CERT_SIGNATURE = -2146869244;

                //
                // Zusammenfassung:
                //     The time-stamp signature or certificate could not be verified or is malformed.
                public const int TRUST_E_TIME_STAMP = -2146869243;

                //
                // Zusammenfassung:
                //     The digital signature of the object did not verify.
                public const int TRUST_E_BAD_DIGEST = -2146869232;

                //
                // Zusammenfassung:
                //     A certificate's basic constraint extension has not been observed.
                public const int TRUST_E_BASIC_CONSTRAINTS = -2146869223;

                //
                // Zusammenfassung:
                //     The certificate does not meet or contain the Authenticode financial extensions.
                public const int TRUST_E_FINANCIAL_CRITERIA = -2146869218;

                //
                // Zusammenfassung:
                //     Tried to reference a part of the file outside the proper range.
                public const int MSSIPOTF_E_OUTOFMEMRANGE = -2146865151;

                //
                // Zusammenfassung:
                //     Could not retrieve an object from the file.
                public const int MSSIPOTF_E_CANTGETOBJECT = -2146865150;

                //
                // Zusammenfassung:
                //     Could not find the head table in the file.
                public const int MSSIPOTF_E_NOHEADTABLE = -2146865149;

                //
                // Zusammenfassung:
                //     The magic number in the head table is incorrect.
                public const int MSSIPOTF_E_BAD_MAGICNUMBER = -2146865148;

                //
                // Zusammenfassung:
                //     The offset table has incorrect values.
                public const int MSSIPOTF_E_BAD_OFFSET_TABLE = -2146865147;

                //
                // Zusammenfassung:
                //     Duplicate table tags or the tags are out of alphabetical order.
                public const int MSSIPOTF_E_TABLE_TAGORDER = -2146865146;

                //
                // Zusammenfassung:
                //     A table does not start on a long word boundary.
                public const int MSSIPOTF_E_TABLE_LONGWORD = -2146865145;

                //
                // Zusammenfassung:
                //     First table does not appear after header information.
                public const int MSSIPOTF_E_BAD_FIRST_TABLE_PLACEMENT = -2146865144;

                //
                // Zusammenfassung:
                //     Two or more tables overlap.
                public const int MSSIPOTF_E_TABLES_OVERLAP = -2146865143;

                //
                // Zusammenfassung:
                //     Too many pad bytes between tables, or pad bytes are not 0.
                public const int MSSIPOTF_E_TABLE_PADBYTES = -2146865142;

                //
                // Zusammenfassung:
                //     File is too small to contain the last table.
                public const int MSSIPOTF_E_FILETOOSMALL = -2146865141;

                //
                // Zusammenfassung:
                //     A table checksum is incorrect.
                public const int MSSIPOTF_E_TABLE_CHECKSUM = -2146865140;

                //
                // Zusammenfassung:
                //     The file checksum is incorrect.
                public const int MSSIPOTF_E_FILE_CHECKSUM = -2146865139;

                //
                // Zusammenfassung:
                //     The signature does not have the correct attributes for the policy.
                public const int MSSIPOTF_E_FAILED_POLICY = -2146865136;

                //
                // Zusammenfassung:
                //     The file did not pass the hints check.
                public const int MSSIPOTF_E_FAILED_HINTS_CHECK = -2146865135;

                //
                // Zusammenfassung:
                //     The file is not an OpenType file.
                public const int MSSIPOTF_E_NOT_OPENTYPE = -2146865134;

                //
                // Zusammenfassung:
                //     Failed on a file operation (such as open, map, read, or write).
                public const int MSSIPOTF_E_FILE = -2146865133;

                //
                // Zusammenfassung:
                //     A call to a CryptoAPI function failed.
                public const int MSSIPOTF_E_CRYPT = -2146865132;

                //
                // Zusammenfassung:
                //     There is a bad version number in the file.
                public const int MSSIPOTF_E_BADVERSION = -2146865131;

                //
                // Zusammenfassung:
                //     The structure of the DSIG table is incorrect.
                public const int MSSIPOTF_E_DSIG_STRUCTURE = -2146865130;

                //
                // Zusammenfassung:
                //     A check failed in a partially constant table.
                public const int MSSIPOTF_E_PCONST_CHECK = -2146865129;

                //
                // Zusammenfassung:
                //     Some kind of structural error.
                public const int MSSIPOTF_E_STRUCTURE = -2146865128;

                //
                // Zusammenfassung:
                //     The requested credential requires confirmation.
                public const int ERROR_CRED_REQUIRES_CONFIRMATION = -2146865127;

                //
                // Zusammenfassung:
                //     Unknown trust provider.
                public const int TRUST_E_PROVIDER_UNKNOWN = -2146762751;

                //
                // Zusammenfassung:
                //     The trust verification action specified is not supported by the specified trust
                //     provider.
                public const int TRUST_E_ACTION_UNKNOWN = -2146762750;

                //
                // Zusammenfassung:
                //     The form specified for the subject is not one supported or known by the specified
                //     trust provider.
                public const int TRUST_E_SUBJECT_FORM_UNKNOWN = -2146762749;

                //
                // Zusammenfassung:
                //     The subject is not trusted for the specified action.
                public const int TRUST_E_SUBJECT_NOT_TRUSTED = -2146762748;

                //
                // Zusammenfassung:
                //     Error due to problem in ASN.1 encoding process.
                public const int DIGSIG_E_ENCODE = -2146762747;

                //
                // Zusammenfassung:
                //     Error due to problem in ASN.1 decoding process.
                public const int DIGSIG_E_DECODE = -2146762746;

                //
                // Zusammenfassung:
                //     Reading/writing extensions where attributes are appropriate, and vice versa.
                public const int DIGSIG_E_EXTENSIBILITY = -2146762745;

                //
                // Zusammenfassung:
                //     Unspecified cryptographic failure.
                public const int DIGSIG_E_CRYPTO = -2146762744;

                //
                // Zusammenfassung:
                //     The size of the data could not be determined.
                public const int PERSIST_E_SIZEDEFINITE = -2146762743;

                //
                // Zusammenfassung:
                //     The size of the indefinite-sized data could not be determined.
                public const int PERSIST_E_SIZEINDEFINITE = -2146762742;

                //
                // Zusammenfassung:
                //     This object does not read and write self-sizing data.
                public const int PERSIST_E_NOTSELFSIZING = -2146762741;

                //
                // Zusammenfassung:
                //     No signature was present in the subject.
                public const int TRUST_E_NOSIGNATURE = -2146762496;

                //
                // Zusammenfassung:
                //     A required certificate is not within its validity period when verifying against
                //     the current system clock or the time stamp in the signed file.
                public const int CERT_E_EXPIRED = -2146762495;

                //
                // Zusammenfassung:
                //     The validity periods of the certification chain do not nest correctly.
                public const int CERT_E_VALIDITYPERIODNESTING = -2146762494;

                //
                // Zusammenfassung:
                //     A certificate that can only be used as an end entity is being used as a CA or
                //     vice versa.
                public const int CERT_E_ROLE = -2146762493;

                //
                // Zusammenfassung:
                //     A path length constraint in the certification chain has been violated.
                public const int CERT_E_PATHLENCONST = -2146762492;

                //
                // Zusammenfassung:
                //     A certificate contains an unknown extension that is marked "critical".
                public const int CERT_E_CRITICAL = -2146762491;

                //
                // Zusammenfassung:
                //     A certificate is being used for a purpose other than the ones specified by its
                //     CA.
                public const int CERT_E_PURPOSE = -2146762490;

                //
                // Zusammenfassung:
                //     A parent of a given certificate did not issue that child certificate.
                public const int CERT_E_ISSUERCHAINING = -2146762489;

                //
                // Zusammenfassung:
                //     A certificate is missing or has an empty value for an important field, such as
                //     a subject or issuer name.
                public const int CERT_E_MALFORMED = -2146762488;

                //
                // Zusammenfassung:
                //     A certificate chain processed, but terminated in a root certificate that is not
                //     trusted by the trust provider.
                public const int CERT_E_UNTRUSTEDROOT = -2146762487;

                //
                // Zusammenfassung:
                //     A certificate chain could not be built to a trusted root authority.
                public const int CERT_E_CHAINING = -2146762486;

                //
                // Zusammenfassung:
                //     Generic trust failure.
                public const int TRUST_E_FAIL = -2146762485;

                //
                // Zusammenfassung:
                //     A certificate was explicitly revoked by its issuer.
                public const int CERT_E_REVOKED = -2146762484;

                //
                // Zusammenfassung:
                //     The certification path terminates with the test root that is not trusted with
                //     the current policy settings.
                public const int CERT_E_UNTRUSTEDTESTROOT = -2146762483;

                //
                // Zusammenfassung:
                //     The revocation process could not continue—the certificates could not be checked.
                public const int CERT_E_REVOCATION_FAILURE = -2146762482;

                //
                // Zusammenfassung:
                //     The certificate's CN name does not match the passed value.
                public const int CERT_E_CN_NO_MATCH = -2146762481;

                //
                // Zusammenfassung:
                //     The certificate is not valid for the requested usage.
                public const int CERT_E_WRONG_USAGE = -2146762480;

                //
                // Zusammenfassung:
                //     The certificate was explicitly marked as untrusted by the user.
                public const int TRUST_E_EXPLICIT_DISTRUST = -2146762479;

                //
                // Zusammenfassung:
                //     A certification chain processed correctly, but one of the CA certificates is
                //     not trusted by the policy provider.
                public const int CERT_E_UNTRUSTEDCA = -2146762478;

                //
                // Zusammenfassung:
                //     The certificate has invalid policy.
                public const int CERT_E_INVALID_POLICY = -2146762477;

                //
                // Zusammenfassung:
                //     The certificate has an invalid name. The name is not included in the permitted
                //     list or is explicitly excluded.
                public const int CERT_E_INVALID_NAME = -2146762476;

                //
                // Zusammenfassung:
                //     The maximum filebitrate value specified is greater than the server's configured
                //     maximum bandwidth.
                public const int NS_W_SERVER_BANDWIDTH_LIMIT = -2146631677;

                //
                // Zusammenfassung:
                //     The maximum bandwidth value specified is less than the maximum filebitrate.
                public const int NS_W_FILE_BANDWIDTH_LIMIT = -2146631676;

                //
                // Zusammenfassung:
                //     Unknown %1 event encountered.
                public const int NS_W_UNKNOWN_EVENT = -2146631584;

                //
                // Zusammenfassung:
                //     Disk %1 ( %2 ) on Content Server %3, will be failed because it is catatonic.
                public const int NS_I_CATATONIC_FAILURE = -2146631271;

                //
                // Zusammenfassung:
                //     Disk %1 ( %2 ) on Content Server %3, auto online from catatonic state.
                public const int NS_I_CATATONIC_AUTO_UNFAIL = -2146631270;

                //
                // Zusammenfassung:
                //     A non-empty line was encountered in the INF before the start of a section.
                public const int SPAPI_E_EXPECTED_SECTION_NAME = -2146500608;

                //
                // Zusammenfassung:
                //     A section name marker in the information file (INF) is not complete or does not
                //     exist on a line by itself.
                public const int SPAPI_E_BAD_SECTION_NAME_LINE = -2146500607;

                //
                // Zusammenfassung:
                //     An INF section was encountered whose name exceeds the maximum section name length.
                public const int SPAPI_E_SECTION_NAME_TOO_LONG = -2146500606;

                //
                // Zusammenfassung:
                //     The syntax of the INF is invalid.
                public const int SPAPI_E_GENERAL_SYNTAX = -2146500605;

                //
                // Zusammenfassung:
                //     The style of the INF is different than what was requested.
                public const int SPAPI_E_WRONG_INF_STYLE = -2146500352;

                //
                // Zusammenfassung:
                //     The required section was not found in the INF.
                public const int SPAPI_E_SECTION_NOT_FOUND = -2146500351;

                //
                // Zusammenfassung:
                //     The required line was not found in the INF.
                public const int SPAPI_E_LINE_NOT_FOUND = -2146500350;

                //
                // Zusammenfassung:
                //     The files affected by the installation of this file queue have not been backed
                //     up for uninstall.
                public const int SPAPI_E_NO_BACKUP = -2146500349;

                //
                // Zusammenfassung:
                //     The INF or the device information set or element does not have an associated
                //     install class.
                public const int SPAPI_E_NO_ASSOCIATED_CLASS = -2146500096;

                //
                // Zusammenfassung:
                //     The INF or the device information set or element does not match the specified
                //     install class.
                public const int SPAPI_E_CLASS_MISMATCH = -2146500095;

                //
                // Zusammenfassung:
                //     An existing device was found that is a duplicate of the device being manually
                //     installed.
                public const int SPAPI_E_DUPLICATE_FOUND = -2146500094;

                //
                // Zusammenfassung:
                //     There is no driver selected for the device information set or element.
                public const int SPAPI_E_NO_DRIVER_SELECTED = -2146500093;

                //
                // Zusammenfassung:
                //     The requested device registry key does not exist.
                public const int SPAPI_E_KEY_DOES_NOT_EXIST = -2146500092;

                //
                // Zusammenfassung:
                //     The device instance name is invalid.
                public const int SPAPI_E_INVALID_DEVINST_NAME = -2146500091;

                //
                // Zusammenfassung:
                //     The install class is not present or is invalid.
                public const int SPAPI_E_INVALID_CLASS = -2146500090;

                //
                // Zusammenfassung:
                //     The device instance cannot be created because it already exists.
                public const int SPAPI_E_DEVINST_ALREADY_EXISTS = -2146500089;

                //
                // Zusammenfassung:
                //     The operation cannot be performed on a device information element that has not
                //     been registered.
                public const int SPAPI_E_DEVINFO_NOT_REGISTERED = -2146500088;

                //
                // Zusammenfassung:
                //     The device property code is invalid.
                public const int SPAPI_E_INVALID_REG_PROPERTY = -2146500087;

                //
                // Zusammenfassung:
                //     The INF from which a driver list is to be built does not exist.
                public const int SPAPI_E_NO_INF = -2146500086;

                //
                // Zusammenfassung:
                //     The device instance does not exist in the hardware tree.
                public const int SPAPI_E_NO_SUCH_DEVINST = -2146500085;

                //
                // Zusammenfassung:
                //     The icon representing this install class cannot be loaded.
                public const int SPAPI_E_CANT_LOAD_CLASS_ICON = -2146500084;

                //
                // Zusammenfassung:
                //     The class installer registry entry is invalid.
                public const int SPAPI_E_INVALID_CLASS_INSTALLER = -2146500083;

                //
                // Zusammenfassung:
                //     The class installer has indicated that the default action should be performed
                //     for this installation request.
                public const int SPAPI_E_DI_DO_DEFAULT = -2146500082;

                //
                // Zusammenfassung:
                //     The operation does not require any files to be copied.
                public const int SPAPI_E_DI_NOFILECOPY = -2146500081;

                //
                // Zusammenfassung:
                //     The specified hardware profile does not exist.
                public const int SPAPI_E_INVALID_HWPROFILE = -2146500080;

                //
                // Zusammenfassung:
                //     There is no device information element currently selected for this device information
                //     set.
                public const int SPAPI_E_NO_DEVICE_SELECTED = -2146500079;

                //
                // Zusammenfassung:
                //     The operation cannot be performed because the device information set is locked.
                public const int SPAPI_E_DEVINFO_LIST_LOCKED = -2146500078;

                //
                // Zusammenfassung:
                //     The operation cannot be performed because the device information element is locked.
                public const int SPAPI_E_DEVINFO_DATA_LOCKED = -2146500077;

                //
                // Zusammenfassung:
                //     The specified path does not contain any applicable device INFs.
                public const int SPAPI_E_DI_BAD_PATH = -2146500076;

                //
                // Zusammenfassung:
                //     No class installer parameters have been set for the device information set or
                //     element.
                public const int SPAPI_E_NO_CLASSINSTALL_PARAMS = -2146500075;

                //
                // Zusammenfassung:
                //     The operation cannot be performed because the file queue is locked.
                public const int SPAPI_E_FILEQUEUE_LOCKED = -2146500074;

                //
                // Zusammenfassung:
                //     A service installation section in this INF is invalid.
                public const int SPAPI_E_BAD_SERVICE_INSTALLSECT = -2146500073;

                //
                // Zusammenfassung:
                //     There is no class driver list for the device information element.
                public const int SPAPI_E_NO_CLASS_DRIVER_LIST = -2146500072;

                //
                // Zusammenfassung:
                //     The installation failed because a function driver was not specified for this
                //     device instance.
                public const int SPAPI_E_NO_ASSOCIATED_SERVICE = -2146500071;

                //
                // Zusammenfassung:
                //     There is presently no default device interface designated for this interface
                //     class.
                public const int SPAPI_E_NO_DEFAULT_DEVICE_INTERFACE = -2146500070;

                //
                // Zusammenfassung:
                //     The operation cannot be performed because the device interface is currently active.
                public const int SPAPI_E_DEVICE_INTERFACE_ACTIVE = -2146500069;

                //
                // Zusammenfassung:
                //     The operation cannot be performed because the device interface has been removed
                //     from the system.
                public const int SPAPI_E_DEVICE_INTERFACE_REMOVED = -2146500068;

                //
                // Zusammenfassung:
                //     An interface installation section in this INF is invalid.
                public const int SPAPI_E_BAD_INTERFACE_INSTALLSECT = -2146500067;

                //
                // Zusammenfassung:
                //     This interface class does not exist in the system.
                public const int SPAPI_E_NO_SUCH_INTERFACE_CLASS = -2146500066;

                //
                // Zusammenfassung:
                //     The reference string supplied for this interface device is invalid.
                public const int SPAPI_E_INVALID_REFERENCE_STRING = -2146500065;

                //
                // Zusammenfassung:
                //     The specified machine name does not conform to Universal Naming Convention (UNCs).
                public const int SPAPI_E_INVALID_MACHINENAME = -2146500064;

                //
                // Zusammenfassung:
                //     A general remote communication error occurred.
                public const int SPAPI_E_REMOTE_COMM_FAILURE = -2146500063;

                //
                // Zusammenfassung:
                //     The machine selected for remote communication is not available at this time.
                public const int SPAPI_E_MACHINE_UNAVAILABLE = -2146500062;

                //
                // Zusammenfassung:
                //     The Plug and Play service is not available on the remote machine.
                public const int SPAPI_E_NO_CONFIGMGR_SERVICES = -2146500061;

                //
                // Zusammenfassung:
                //     The property page provider registry entry is invalid.
                public const int SPAPI_E_INVALID_PROPPAGE_PROVIDER = -2146500060;

                //
                // Zusammenfassung:
                //     The requested device interface is not present in the system.
                public const int SPAPI_E_NO_SUCH_DEVICE_INTERFACE = -2146500059;

                //
                // Zusammenfassung:
                //     The device's co-installer has additional work to perform after installation is
                //     complete.
                public const int SPAPI_E_DI_POSTPROCESSING_REQUIRED = -2146500058;

                //
                // Zusammenfassung:
                //     The device's co-installer is invalid.
                public const int SPAPI_E_INVALID_COINSTALLER = -2146500057;

                //
                // Zusammenfassung:
                //     There are no compatible drivers for this device.
                public const int SPAPI_E_NO_COMPAT_DRIVERS = -2146500056;

                //
                // Zusammenfassung:
                //     There is no icon that represents this device or device type.
                public const int SPAPI_E_NO_DEVICE_ICON = -2146500055;

                //
                // Zusammenfassung:
                //     A logical configuration specified in this INF is invalid.
                public const int SPAPI_E_INVALID_INF_LOGCONFIG = -2146500054;

                //
                // Zusammenfassung:
                //     The class installer has denied the request to install or upgrade this device.
                public const int SPAPI_E_DI_DONT_INSTALL = -2146500053;

                //
                // Zusammenfassung:
                //     One of the filter drivers installed for this device is invalid.
                public const int SPAPI_E_INVALID_FILTER_DRIVER = -2146500052;

                //
                // Zusammenfassung:
                //     The driver selected for this device does not support Windows XP operating system.
                public const int SPAPI_E_NON_WINDOWS_NT_DRIVER = -2146500051;

                //
                // Zusammenfassung:
                //     The driver selected for this device does not support Windows.
                public const int SPAPI_E_NON_WINDOWS_DRIVER = -2146500050;

                //
                // Zusammenfassung:
                //     The third-party INF does not contain digital signature information.
                public const int SPAPI_E_NO_CATALOG_FOR_OEM_INF = -2146500049;

                //
                // Zusammenfassung:
                //     An invalid attempt was made to use a device installation file queue for verification
                //     of digital signatures relative to other platforms.
                public const int SPAPI_E_DEVINSTALL_QUEUE_NONNATIVE = -2146500048;

                //
                // Zusammenfassung:
                //     The device cannot be disabled.
                public const int SPAPI_E_NOT_DISABLEABLE = -2146500047;

                //
                // Zusammenfassung:
                //     The device could not be dynamically removed.
                public const int SPAPI_E_CANT_REMOVE_DEVINST = -2146500046;

                //
                // Zusammenfassung:
                //     Cannot copy to specified target.
                public const int SPAPI_E_INVALID_TARGET = -2146500045;

                //
                // Zusammenfassung:
                //     Driver is not intended for this platform.
                public const int SPAPI_E_DRIVER_NONNATIVE = -2146500044;

                //
                // Zusammenfassung:
                //     Operation not allowed in WOW64.
                public const int SPAPI_E_IN_WOW64 = -2146500043;

                //
                // Zusammenfassung:
                //     The operation involving unsigned file copying was rolled back, so that a system
                //     restore point could be set.
                public const int SPAPI_E_SET_SYSTEM_RESTORE_POINT = -2146500042;

                //
                // Zusammenfassung:
                //     An INF was copied into the Windows INF directory in an improper manner.
                public const int SPAPI_E_INCORRECTLY_COPIED_INF = -2146500041;

                //
                // Zusammenfassung:
                //     The Security Configuration Editor (SCE) APIs have been disabled on this embedded
                //     product.
                public const int SPAPI_E_SCE_DISABLED = -2146500040;

                //
                // Zusammenfassung:
                //     An unknown exception was encountered.
                public const int SPAPI_E_UNKNOWN_EXCEPTION = -2146500039;

                //
                // Zusammenfassung:
                //     A problem was encountered when accessing the Plug and Play registry database.
                public const int SPAPI_E_PNP_REGISTRY_ERROR = -2146500038;

                //
                // Zusammenfassung:
                //     The requested operation is not supported for a remote machine.
                public const int SPAPI_E_REMOTE_REQUEST_UNSUPPORTED = -2146500037;

                //
                // Zusammenfassung:
                //     The specified file is not an installed original equipment manufacturer (OEM)
                //     INF.
                public const int SPAPI_E_NOT_AN_INSTALLED_OEM_INF = -2146500036;

                //
                // Zusammenfassung:
                //     One or more devices are presently installed using the specified INF.
                public const int SPAPI_E_INF_IN_USE_BY_DEVICES = -2146500035;

                //
                // Zusammenfassung:
                //     The requested device install operation is obsolete.
                public const int SPAPI_E_DI_FUNCTION_OBSOLETE = -2146500034;

                //
                // Zusammenfassung:
                //     A file could not be verified because it does not have an associated catalog signed
                //     via Authenticode.
                public const int SPAPI_E_NO_AUTHENTICODE_CATALOG = -2146500033;

                //
                // Zusammenfassung:
                //     Authenticode signature verification is not supported for the specified INF.
                public const int SPAPI_E_AUTHENTICODE_DISALLOWED = -2146500032;

                //
                // Zusammenfassung:
                //     The INF was signed with an Authenticode catalog from a trusted publisher.
                public const int SPAPI_E_AUTHENTICODE_TRUSTED_PUBLISHER = -2146500031;

                //
                // Zusammenfassung:
                //     The publisher of an Authenticode-signed catalog has not yet been established
                //     as trusted.
                public const int SPAPI_E_AUTHENTICODE_TRUST_NOT_ESTABLISHED = -2146500030;

                //
                // Zusammenfassung:
                //     The publisher of an Authenticode-signed catalog was not established as trusted.
                public const int SPAPI_E_AUTHENTICODE_PUBLISHER_NOT_TRUSTED = -2146500029;

                //
                // Zusammenfassung:
                //     The software was tested for compliance with Windows logo requirements on a different
                //     version of Windows and might not be compatible with this version.
                public const int SPAPI_E_SIGNATURE_OSATTRIBUTE_MISMATCH = -2146500028;

                //
                // Zusammenfassung:
                //     The file can be validated only by a catalog signed via Authenticode.
                public const int SPAPI_E_ONLY_VALIDATE_VIA_AUTHENTICODE = -2146500027;

                //
                // Zusammenfassung:
                //     One of the installers for this device cannot perform the installation at this
                //     time.
                public const int SPAPI_E_DEVICE_INSTALLER_NOT_READY = -2146500026;

                //
                // Zusammenfassung:
                //     A problem was encountered while attempting to add the driver to the store.
                public const int SPAPI_E_DRIVER_STORE_ADD_FAILED = -2146500025;

                //
                // Zusammenfassung:
                //     The installation of this device is forbidden by system policy. Contact your system
                //     administrator.
                public const int SPAPI_E_DEVICE_INSTALL_BLOCKED = -2146500024;

                //
                // Zusammenfassung:
                //     The installation of this driver is forbidden by system policy. Contact your system
                //     administrator.
                public const int SPAPI_E_DRIVER_INSTALL_BLOCKED = -2146500023;

                //
                // Zusammenfassung:
                //     The specified INF is the wrong type for this operation.
                public const int SPAPI_E_WRONG_INF_TYPE = -2146500022;

                //
                // Zusammenfassung:
                //     The hash for the file is not present in the specified catalog file. The file
                //     is likely corrupt or the victim of tampering.
                public const int SPAPI_E_FILE_HASH_NOT_IN_CATALOG = -2146500021;

                //
                // Zusammenfassung:
                //     A problem was encountered while attempting to delete the driver from the store.
                public const int SPAPI_E_DRIVER_STORE_DELETE_FAILED = -2146500020;

                //
                // Zusammenfassung:
                //     An unrecoverable stack overflow was encountered.
                public const int SPAPI_E_UNRECOVERABLE_STACK_OVERFLOW = -2146499840;

                //
                // Zusammenfassung:
                //     No installed components were detected.
                public const int SPAPI_E_ERROR_NOT_INSTALLED = -2146496512;

                //
                // Zusammenfassung:
                //     An internal consistency check failed.
                public const int SCARD_F_INTERNAL_ERROR = -2146435071;

                //
                // Zusammenfassung:
                //     The action was canceled by an SCardCancel request.
                public const int SCARD_E_CANCELLED = -2146435070;

                //
                // Zusammenfassung:
                //     The supplied handle was invalid.
                public const int SCARD_E_INVALID_HANDLE = -2146435069;

                //
                // Zusammenfassung:
                //     One or more of the supplied parameters could not be properly interpreted.
                public const int SCARD_E_INVALID_PARAMETER = -2146435068;

                //
                // Zusammenfassung:
                //     Registry startup information is missing or invalid.
                public const int SCARD_E_INVALID_TARGET = -2146435067;

                //
                // Zusammenfassung:
                //     Not enough memory available to complete this command.
                public const int SCARD_E_NO_MEMORY = -2146435066;

                //
                // Zusammenfassung:
                //     An internal consistency timer has expired.
                public const int SCARD_F_WAITED_TOO_LONG = -2146435065;

                //
                // Zusammenfassung:
                //     The data buffer to receive returned data is too small for the returned data.
                public const int SCARD_E_INSUFFICIENT_BUFFER = -2146435064;

                //
                // Zusammenfassung:
                //     The specified reader name is not recognized.
                public const int SCARD_E_UNKNOWN_READER = -2146435063;

                //
                // Zusammenfassung:
                //     The user-specified time-out value has expired.
                public const int SCARD_E_TIMEOUT = -2146435062;

                //
                // Zusammenfassung:
                //     The smart card cannot be accessed because of other connections outstanding.
                public const int SCARD_E_SHARING_VIOLATION = -2146435061;

                //
                // Zusammenfassung:
                //     The operation requires a smart card, but no smart card is currently in the device.
                public const int SCARD_E_NO_SMARTCARD = -2146435060;

                //
                // Zusammenfassung:
                //     The specified smart card name is not recognized.
                public const int SCARD_E_UNKNOWN_CARD = -2146435059;

                //
                // Zusammenfassung:
                //     The system could not dispose of the media in the requested manner.
                public const int SCARD_E_CANT_DISPOSE = -2146435058;

                //
                // Zusammenfassung:
                //     The requested protocols are incompatible with the protocol currently in use with
                //     the smart card.
                public const int SCARD_E_PROTO_MISMATCH = -2146435057;

                //
                // Zusammenfassung:
                //     The reader or smart card is not ready to accept commands.
                public const int SCARD_E_NOT_READY = -2146435056;

                //
                // Zusammenfassung:
                //     One or more of the supplied parameters values could not be properly interpreted.
                public const int SCARD_E_INVALID_VALUE = -2146435055;

                //
                // Zusammenfassung:
                //     The action was canceled by the system, presumably to log off or shut down.
                public const int SCARD_E_SYSTEM_CANCELLED = -2146435054;

                //
                // Zusammenfassung:
                //     An internal communications error has been detected.
                public const int SCARD_F_COMM_ERROR = -2146435053;

                //
                // Zusammenfassung:
                //     An internal error has been detected, but the source is unknown.
                public const int SCARD_F_UNKNOWN_ERROR = -2146435052;

                //
                // Zusammenfassung:
                //     An automatic terminal recognition (ATR) obtained from the registry is not a valid
                //     ATR string.
                public const int SCARD_E_INVALID_ATR = -2146435051;

                //
                // Zusammenfassung:
                //     An attempt was made to end a nonexistent transaction.
                public const int SCARD_E_NOT_TRANSACTED = -2146435050;

                //
                // Zusammenfassung:
                //     The specified reader is not currently available for use.
                public const int SCARD_E_READER_UNAVAILABLE = -2146435049;

                //
                // Zusammenfassung:
                //     The operation has been aborted to allow the server application to exit.
                public const int SCARD_P_SHUTDOWN = -2146435048;

                //
                // Zusammenfassung:
                //     The peripheral component interconnect (PCI) Receive buffer was too small.
                public const int SCARD_E_PCI_TOO_SMALL = -2146435047;

                //
                // Zusammenfassung:
                //     The reader driver does not meet minimal requirements for support.
                public const int SCARD_E_READER_UNSUPPORTED = -2146435046;

                //
                // Zusammenfassung:
                //     The reader driver did not produce a unique reader name.
                public const int SCARD_E_DUPLICATE_READER = -2146435045;

                //
                // Zusammenfassung:
                //     The smart card does not meet minimal requirements for support.
                public const int SCARD_E_CARD_UNSUPPORTED = -2146435044;

                //
                // Zusammenfassung:
                //     The smart card resource manager is not running.
                public const int SCARD_E_NO_SERVICE = -2146435043;

                //
                // Zusammenfassung:
                //     The smart card resource manager has shut down.
                public const int SCARD_E_SERVICE_STOPPED = -2146435042;

                //
                // Zusammenfassung:
                //     An unexpected card error has occurred.
                public const int SCARD_E_UNEXPECTED = -2146435041;

                //
                // Zusammenfassung:
                //     No primary provider can be found for the smart card.
                public const int SCARD_E_ICC_INSTALLATION = -2146435040;

                //
                // Zusammenfassung:
                //     The requested order of object creation is not supported.
                public const int SCARD_E_ICC_CREATEORDER = -2146435039;

                //
                // Zusammenfassung:
                //     This smart card does not support the requested feature.
                public const int SCARD_E_UNSUPPORTED_FEATURE = -2146435038;

                //
                // Zusammenfassung:
                //     The identified directory does not exist in the smart card.
                public const int SCARD_E_DIR_NOT_FOUND = -2146435037;

                //
                // Zusammenfassung:
                //     The identified file does not exist in the smart card.
                public const int SCARD_E_FILE_NOT_FOUND = -2146435036;

                //
                // Zusammenfassung:
                //     The supplied path does not represent a smart card directory.
                public const int SCARD_E_NO_DIR = -2146435035;

                //
                // Zusammenfassung:
                //     The supplied path does not represent a smart card file.
                public const int SCARD_E_NO_FILE = -2146435034;

                //
                // Zusammenfassung:
                //     Access is denied to this file.
                public const int SCARD_E_NO_ACCESS = -2146435033;

                //
                // Zusammenfassung:
                //     The smart card does not have enough memory to store the information.
                public const int SCARD_E_WRITE_TOO_MANY = -2146435032;

                //
                // Zusammenfassung:
                //     There was an error trying to set the smart card file object pointer.
                public const int SCARD_E_BAD_SEEK = -2146435031;

                //
                // Zusammenfassung:
                //     The supplied PIN is incorrect.
                public const int SCARD_E_INVALID_CHV = -2146435030;

                //
                // Zusammenfassung:
                //     An unrecognized error code was returned from a layered component.
                public const int SCARD_E_UNKNOWN_RES_MNG = -2146435029;

                //
                // Zusammenfassung:
                //     The requested certificate does not exist.
                public const int SCARD_E_NO_SUCH_CERTIFICATE = -2146435028;

                //
                // Zusammenfassung:
                //     The requested certificate could not be obtained.
                public const int SCARD_E_CERTIFICATE_UNAVAILABLE = -2146435027;

                //
                // Zusammenfassung:
                //     Cannot find a smart card reader.
                public const int SCARD_E_NO_READERS_AVAILABLE = -2146435026;

                //
                // Zusammenfassung:
                //     A communications error with the smart card has been detected. Retry the operation.
                public const int SCARD_E_COMM_DATA_LOST = -2146435025;

                //
                // Zusammenfassung:
                //     The requested key container does not exist on the smart card.
                public const int SCARD_E_NO_KEY_CONTAINER = -2146435024;

                //
                // Zusammenfassung:
                //     The smart card resource manager is too busy to complete this operation.
                public const int SCARD_E_SERVER_TOO_BUSY = -2146435023;

                //
                // Zusammenfassung:
                //     The reader cannot communicate with the smart card, due to ATR configuration conflicts.
                public const int SCARD_W_UNSUPPORTED_CARD = -2146434971;

                //
                // Zusammenfassung:
                //     The smart card is not responding to a reset.
                public const int SCARD_W_UNRESPONSIVE_CARD = -2146434970;

                //
                // Zusammenfassung:
                //     Power has been removed from the smart card, so that further communication is
                //     not possible.
                public const int SCARD_W_UNPOWERED_CARD = -2146434969;

                //
                // Zusammenfassung:
                //     The smart card has been reset, so any shared state information is invalid.
                public const int SCARD_W_RESET_CARD = -2146434968;

                //
                // Zusammenfassung:
                //     The smart card has been removed, so that further communication is not possible.
                public const int SCARD_W_REMOVED_CARD = -2146434967;

                //
                // Zusammenfassung:
                //     Access was denied because of a security violation.
                public const int SCARD_W_SECURITY_VIOLATION = -2146434966;

                //
                // Zusammenfassung:
                //     The card cannot be accessed because the wrong PIN was presented.
                public const int SCARD_W_WRONG_CHV = -2146434965;

                //
                // Zusammenfassung:
                //     The card cannot be accessed because the maximum number of PIN entry attempts
                //     has been reached.
                public const int SCARD_W_CHV_BLOCKED = -2146434964;

                //
                // Zusammenfassung:
                //     The end of the smart card file has been reached.
                public const int SCARD_W_EOF = -2146434963;

                //
                // Zusammenfassung:
                //     The action was canceled by the user.
                public const int SCARD_W_CANCELLED_BY_USER = -2146434962;

                //
                // Zusammenfassung:
                //     No PIN was presented to the smart card.
                public const int SCARD_W_CARD_NOT_AUTHENTICATED = -2146434961;

                //
                // Zusammenfassung:
                //     Errors occurred accessing one or more objects—the ErrorInfo collection contains
                //     more detail.
                public const int COMADMIN_E_OBJECTERRORS = -2146368511;

                //
                // Zusammenfassung:
                //     One or more of the object's properties are missing or invalid.
                public const int COMADMIN_E_OBJECTINVALID = -2146368510;

                //
                // Zusammenfassung:
                //     The object was not found in the catalog.
                public const int COMADMIN_E_KEYMISSING = -2146368509;

                //
                // Zusammenfassung:
                //     The object is already registered.
                public const int COMADMIN_E_ALREADYINSTALLED = -2146368508;

                //
                // Zusammenfassung:
                //     An error occurred writing to the application file.
                public const int COMADMIN_E_APP_FILE_WRITEFAIL = -2146368505;

                //
                // Zusammenfassung:
                //     An error occurred reading the application file.
                public const int COMADMIN_E_APP_FILE_READFAIL = -2146368504;

                //
                // Zusammenfassung:
                //     Invalid version number in application file.
                public const int COMADMIN_E_APP_FILE_VERSION = -2146368503;

                //
                // Zusammenfassung:
                //     The file path is invalid.
                public const int COMADMIN_E_BADPATH = -2146368502;

                //
                // Zusammenfassung:
                //     The application is already installed.
                public const int COMADMIN_E_APPLICATIONEXISTS = -2146368501;

                //
                // Zusammenfassung:
                //     The role already exists.
                public const int COMADMIN_E_ROLEEXISTS = -2146368500;

                //
                // Zusammenfassung:
                //     An error occurred copying the file.
                public const int COMADMIN_E_CANTCOPYFILE = -2146368499;

                //
                // Zusammenfassung:
                //     One or more users are not valid.
                public const int COMADMIN_E_NOUSER = -2146368497;

                //
                // Zusammenfassung:
                //     One or more users in the application file are not valid.
                public const int COMADMIN_E_INVALIDUSERIDS = -2146368496;

                //
                // Zusammenfassung:
                //     The component's CLSID is missing or corrupt.
                public const int COMADMIN_E_NOREGISTRYCLSID = -2146368495;

                //
                // Zusammenfassung:
                //     The component's programmatic ID is missing or corrupt.
                public const int COMADMIN_E_BADREGISTRYPROGID = -2146368494;

                //
                // Zusammenfassung:
                //     Unable to set required authentication level for update request.
                public const int COMADMIN_E_AUTHENTICATIONLEVEL = -2146368493;

                //
                // Zusammenfassung:
                //     The identity or password set on the application is not valid.
                public const int COMADMIN_E_USERPASSWDNOTVALID = -2146368492;

                //
                // Zusammenfassung:
                //     Application file CLSIDs or instance identifiers (IIDs) do not match corresponding
                //     DLLs.
                public const int COMADMIN_E_CLSIDORIIDMISMATCH = -2146368488;

                //
                // Zusammenfassung:
                //     Interface information is either missing or changed.
                public const int COMADMIN_E_REMOTEINTERFACE = -2146368487;

                //
                // Zusammenfassung:
                //     DllRegisterServer failed on component install.
                public const int COMADMIN_E_DLLREGISTERSERVER = -2146368486;

                //
                // Zusammenfassung:
                //     No server file share available.
                public const int COMADMIN_E_NOSERVERSHARE = -2146368485;

                //
                // Zusammenfassung:
                //     DLL could not be loaded.
                public const int COMADMIN_E_DLLLOADFAILED = -2146368483;

                //
                // Zusammenfassung:
                //     The registered TypeLib ID is not valid.
                public const int COMADMIN_E_BADREGISTRYLIBID = -2146368482;

                //
                // Zusammenfassung:
                //     Application install directory not found.
                public const int COMADMIN_E_APPDIRNOTFOUND = -2146368481;

                //
                // Zusammenfassung:
                //     Errors occurred while in the component registrar.
                public const int COMADMIN_E_REGISTRARFAILED = -2146368477;

                //
                // Zusammenfassung:
                //     The file does not exist.
                public const int COMADMIN_E_COMPFILE_DOESNOTEXIST = -2146368476;

                //
                // Zusammenfassung:
                //     The DLL could not be loaded.
                public const int COMADMIN_E_COMPFILE_LOADDLLFAIL = -2146368475;

                //
                // Zusammenfassung:
                //     GetClassObject failed in the DLL.
                public const int COMADMIN_E_COMPFILE_GETCLASSOBJ = -2146368474;

                //
                // Zusammenfassung:
                //     The DLL does not support the components listed in the TypeLib.
                public const int COMADMIN_E_COMPFILE_CLASSNOTAVAIL = -2146368473;

                //
                // Zusammenfassung:
                //     The TypeLib could not be loaded.
                public const int COMADMIN_E_COMPFILE_BADTLB = -2146368472;

                //
                // Zusammenfassung:
                //     The file does not contain components or component information.
                public const int COMADMIN_E_COMPFILE_NOTINSTALLABLE = -2146368471;

                //
                // Zusammenfassung:
                //     Changes to this object and its subobjects have been disabled.
                public const int COMADMIN_E_NOTCHANGEABLE = -2146368470;

                //
                // Zusammenfassung:
                //     The delete function has been disabled for this object.
                public const int COMADMIN_E_NOTDELETEABLE = -2146368469;

                //
                // Zusammenfassung:
                //     The server catalog version is not supported.
                public const int COMADMIN_E_SESSION = -2146368468;

                //
                // Zusammenfassung:
                //     The component move was disallowed because the source or destination application
                //     is either a system application or currently locked against changes.
                public const int COMADMIN_E_COMP_MOVE_LOCKED = -2146368467;

                //
                // Zusammenfassung:
                //     The component move failed because the destination application no longer exists.
                public const int COMADMIN_E_COMP_MOVE_BAD_DEST = -2146368466;

                //
                // Zusammenfassung:
                //     The system was unable to register the TypeLib.
                public const int COMADMIN_E_REGISTERTLB = -2146368464;

                //
                // Zusammenfassung:
                //     This operation cannot be performed on the system application.
                public const int COMADMIN_E_SYSTEMAPP = -2146368461;

                //
                // Zusammenfassung:
                //     The component registrar referenced in this file is not available.
                public const int COMADMIN_E_COMPFILE_NOREGISTRAR = -2146368460;

                //
                // Zusammenfassung:
                //     A component in the same DLL is already installed.
                public const int COMADMIN_E_COREQCOMPINSTALLED = -2146368459;

                //
                // Zusammenfassung:
                //     The service is not installed.
                public const int COMADMIN_E_SERVICENOTINSTALLED = -2146368458;

                //
                // Zusammenfassung:
                //     One or more property settings are either invalid or in conflict with each other.
                public const int COMADMIN_E_PROPERTYSAVEFAILED = -2146368457;

                //
                // Zusammenfassung:
                //     The object you are attempting to add or rename already exists.
                public const int COMADMIN_E_OBJECTEXISTS = -2146368456;

                //
                // Zusammenfassung:
                //     The component already exists.
                public const int COMADMIN_E_COMPONENTEXISTS = -2146368455;

                //
                // Zusammenfassung:
                //     The registration file is corrupt.
                public const int COMADMIN_E_REGFILE_CORRUPT = -2146368453;

                //
                // Zusammenfassung:
                //     The property value is too large.
                public const int COMADMIN_E_PROPERTY_OVERFLOW = -2146368452;

                //
                // Zusammenfassung:
                //     Object was not found in registry.
                public const int COMADMIN_E_NOTINREGISTRY = -2146368450;

                //
                // Zusammenfassung:
                //     This object cannot be pooled.
                public const int COMADMIN_E_OBJECTNOTPOOLABLE = -2146368449;

                //
                // Zusammenfassung:
                //     A CLSID with the same GUID as the new application ID is already installed on
                //     this machine.
                public const int COMADMIN_E_APPLID_MATCHES_CLSID = -2146368442;

                //
                // Zusammenfassung:
                //     A role assigned to a component, interface, or method did not exist in the application.
                public const int COMADMIN_E_ROLE_DOES_NOT_EXIST = -2146368441;

                //
                // Zusammenfassung:
                //     You must have components in an application to start the application.
                public const int COMADMIN_E_START_APP_NEEDS_COMPONENTS = -2146368440;

                //
                // Zusammenfassung:
                //     This operation is not enabled on this platform.
                public const int COMADMIN_E_REQUIRES_DIFFERENT_PLATFORM = -2146368439;

                //
                // Zusammenfassung:
                //     Application proxy is not exportable.
                public const int COMADMIN_E_CAN_NOT_EXPORT_APP_PROXY = -2146368438;

                //
                // Zusammenfassung:
                //     Failed to start application because it is either a library application or an
                //     application proxy.
                public const int COMADMIN_E_CAN_NOT_START_APP = -2146368437;

                //
                // Zusammenfassung:
                //     System application is not exportable.
                public const int COMADMIN_E_CAN_NOT_EXPORT_SYS_APP = -2146368436;

                //
                // Zusammenfassung:
                //     Cannot subscribe to this component (the component might have been imported).
                public const int COMADMIN_E_CANT_SUBSCRIBE_TO_COMPONENT = -2146368435;

                //
                // Zusammenfassung:
                //     An event class cannot also be a subscriber component.
                public const int COMADMIN_E_EVENTCLASS_CANT_BE_SUBSCRIBER = -2146368434;

                //
                // Zusammenfassung:
                //     Library applications and application proxies are incompatible.
                public const int COMADMIN_E_LIB_APP_PROXY_INCOMPATIBLE = -2146368433;

                //
                // Zusammenfassung:
                //     This function is valid for the base partition only.
                public const int COMADMIN_E_BASE_PARTITION_ONLY = -2146368432;

                //
                // Zusammenfassung:
                //     You cannot start an application that has been disabled.
                public const int COMADMIN_E_START_APP_DISABLED = -2146368431;

                //
                // Zusammenfassung:
                //     The specified partition name is already in use on this computer.
                public const int COMADMIN_E_CAT_DUPLICATE_PARTITION_NAME = -2146368425;

                //
                // Zusammenfassung:
                //     The specified partition name is invalid. Check that the name contains at least
                //     one visible character.
                public const int COMADMIN_E_CAT_INVALID_PARTITION_NAME = -2146368424;

                //
                // Zusammenfassung:
                //     The partition cannot be deleted because it is the default partition for one or
                //     more users.
                public const int COMADMIN_E_CAT_PARTITION_IN_USE = -2146368423;

                //
                // Zusammenfassung:
                //     The partition cannot be exported because one or more components in the partition
                //     have the same file name.
                public const int COMADMIN_E_FILE_PARTITION_DUPLICATE_FILES = -2146368422;

                //
                // Zusammenfassung:
                //     Applications that contain one or more imported components cannot be installed
                //     into a nonbase partition.
                public const int COMADMIN_E_CAT_IMPORTED_COMPONENTS_NOT_ALLOWED = -2146368421;

                //
                // Zusammenfassung:
                //     The application name is not unique and cannot be resolved to an application ID.
                public const int COMADMIN_E_AMBIGUOUS_APPLICATION_NAME = -2146368420;

                //
                // Zusammenfassung:
                //     The partition name is not unique and cannot be resolved to a partition ID.
                public const int COMADMIN_E_AMBIGUOUS_PARTITION_NAME = -2146368419;

                //
                // Zusammenfassung:
                //     The COM+ registry database has not been initialized.
                public const int COMADMIN_E_REGDB_NOTINITIALIZED = -2146368398;

                //
                // Zusammenfassung:
                //     The COM+ registry database is not open.
                public const int COMADMIN_E_REGDB_NOTOPEN = -2146368397;

                //
                // Zusammenfassung:
                //     The COM+ registry database detected a system error.
                public const int COMADMIN_E_REGDB_SYSTEMERR = -2146368396;

                //
                // Zusammenfassung:
                //     The COM+ registry database is already running.
                public const int COMADMIN_E_REGDB_ALREADYRUNNING = -2146368395;

                //
                // Zusammenfassung:
                //     This version of the COM+ registry database cannot be migrated.
                public const int COMADMIN_E_MIG_VERSIONNOTSUPPORTED = -2146368384;

                //
                // Zusammenfassung:
                //     The schema version to be migrated could not be found in the COM+ registry database.
                public const int COMADMIN_E_MIG_SCHEMANOTFOUND = -2146368383;

                //
                // Zusammenfassung:
                //     There was a type mismatch between binaries.
                public const int COMADMIN_E_CAT_BITNESSMISMATCH = -2146368382;

                //
                // Zusammenfassung:
                //     A binary of unknown or invalid type was provided.
                public const int COMADMIN_E_CAT_UNACCEPTABLEBITNESS = -2146368381;

                //
                // Zusammenfassung:
                //     There was a type mismatch between a binary and an application.
                public const int COMADMIN_E_CAT_WRONGAPPBITNESS = -2146368380;

                //
                // Zusammenfassung:
                //     The application cannot be paused or resumed.
                public const int COMADMIN_E_CAT_PAUSE_RESUME_NOT_SUPPORTED = -2146368379;

                //
                // Zusammenfassung:
                //     The COM+ catalog server threw an exception during execution.
                public const int COMADMIN_E_CAT_SERVERFAULT = -2146368378;

                //
                // Zusammenfassung:
                //     Only COM+ applications marked "queued" can be invoked using the "queue" moniker.
                public const int COMQC_E_APPLICATION_NOT_QUEUED = -2146368000;

                //
                // Zusammenfassung:
                //     At least one interface must be marked "queued" to create a queued component instance
                //     with the "queue" moniker.
                public const int COMQC_E_NO_QUEUEABLE_INTERFACES = -2146367999;

                //
                // Zusammenfassung:
                //     Message Queuing is required for the requested operation and is not installed.
                public const int COMQC_E_QUEUING_SERVICE_NOT_AVAILABLE = -2146367998;

                //
                // Zusammenfassung:
                //     Unable to marshal an interface that does not support IPersistStream.
                public const int COMQC_E_NO_IPERSISTSTREAM = -2146367997;

                //
                // Zusammenfassung:
                //     The message is improperly formatted or was damaged in transit.
                public const int COMQC_E_BAD_MESSAGE = -2146367996;

                //
                // Zusammenfassung:
                //     An unauthenticated message was received by an application that accepts only authenticated
                //     messages.
                public const int COMQC_E_UNAUTHENTICATED = -2146367995;

                //
                // Zusammenfassung:
                //     The message was requeued or moved by a user not in the QC Trusted User "role".
                public const int COMQC_E_UNTRUSTED_ENQUEUER = -2146367994;

                //
                // Zusammenfassung:
                //     Cannot create a duplicate resource of type Distributed Transaction Coordinator.
                public const int MSDTC_E_DUPLICATE_RESOURCE = -2146367743;

                //
                // Zusammenfassung:
                //     One of the objects being inserted or updated does not belong to a valid parent
                //     collection.
                public const int COMADMIN_E_OBJECT_PARENT_MISSING = -2146367480;

                //
                // Zusammenfassung:
                //     One of the specified objects cannot be found.
                public const int COMADMIN_E_OBJECT_DOES_NOT_EXIST = -2146367479;

                //
                // Zusammenfassung:
                //     The specified application is not currently running.
                public const int COMADMIN_E_APP_NOT_RUNNING = -2146367478;

                //
                // Zusammenfassung:
                //     The partitions specified are not valid.
                public const int COMADMIN_E_INVALID_PARTITION = -2146367477;

                //
                // Zusammenfassung:
                //     COM+ applications that run as Windows NT service cannot be pooled or recycled.
                public const int COMADMIN_E_SVCAPP_NOT_POOLABLE_OR_RECYCLABLE = -2146367475;

                //
                // Zusammenfassung:
                //     One or more users are already assigned to a local partition set.
                public const int COMADMIN_E_USER_IN_SET = -2146367474;

                //
                // Zusammenfassung:
                //     Library applications cannot be recycled.
                public const int COMADMIN_E_CANTRECYCLELIBRARYAPPS = -2146367473;

                //
                // Zusammenfassung:
                //     Applications running as Windows NT services cannot be recycled.
                public const int COMADMIN_E_CANTRECYCLESERVICEAPPS = -2146367471;

                //
                // Zusammenfassung:
                //     The process has already been recycled.
                public const int COMADMIN_E_PROCESSALREADYRECYCLED = -2146367470;

                //
                // Zusammenfassung:
                //     A paused process cannot be recycled.
                public const int COMADMIN_E_PAUSEDPROCESSMAYNOTBERECYCLED = -2146367469;

                //
                // Zusammenfassung:
                //     Library applications cannot be Windows NT services.
                public const int COMADMIN_E_CANTMAKEINPROCSERVICE = -2146367468;

                //
                // Zusammenfassung:
                //     The ProgID provided to the copy operation is invalid. The ProgID is in use by
                //     another registered CLSID.
                public const int COMADMIN_E_PROGIDINUSEBYCLSID = -2146367467;

                //
                // Zusammenfassung:
                //     The partition specified as the default is not a member of the partition set.
                public const int COMADMIN_E_DEFAULT_PARTITION_NOT_IN_SET = -2146367466;

                //
                // Zusammenfassung:
                //     A recycled process cannot be paused.
                public const int COMADMIN_E_RECYCLEDPROCESSMAYNOTBEPAUSED = -2146367465;

                //
                // Zusammenfassung:
                //     Access to the specified partition is denied.
                public const int COMADMIN_E_PARTITION_ACCESSDENIED = -2146367464;

                //
                // Zusammenfassung:
                //     Only application files (*.msi files) can be installed into partitions.
                public const int COMADMIN_E_PARTITION_MSI_ONLY = -2146367463;

                //
                // Zusammenfassung:
                //     Applications containing one or more legacy components cannot be exported to 1.0
                //     format.
                public const int COMADMIN_E_LEGACYCOMPS_NOT_ALLOWED_IN_1_0_FORMAT = -2146367462;

                //
                // Zusammenfassung:
                //     Legacy components cannot exist in nonbase partitions.
                public const int COMADMIN_E_LEGACYCOMPS_NOT_ALLOWED_IN_NONBASE_PARTITIONS = -2146367461;

                //
                // Zusammenfassung:
                //     A component cannot be moved (or copied) from the System Application, an application
                //     proxy, or a nonchangeable application.
                public const int COMADMIN_E_COMP_MOVE_SOURCE = -2146367460;

                //
                // Zusammenfassung:
                //     A component cannot be moved (or copied) to the System Application, an application
                //     proxy or a nonchangeable application.
                public const int COMADMIN_E_COMP_MOVE_DEST = -2146367459;

                //
                // Zusammenfassung:
                //     A private component cannot be moved (or copied) to a library application or to
                //     the base partition.
                public const int COMADMIN_E_COMP_MOVE_PRIVATE = -2146367458;

                //
                // Zusammenfassung:
                //     The Base Application Partition exists in all partition sets and cannot be removed.
                public const int COMADMIN_E_BASEPARTITION_REQUIRED_IN_SET = -2146367457;

                //
                // Zusammenfassung:
                //     Alas, Event Class components cannot be aliased.
                public const int COMADMIN_E_CANNOT_ALIAS_EVENTCLASS = -2146367456;

                //
                // Zusammenfassung:
                //     Access is denied because the component is private.
                public const int COMADMIN_E_PRIVATE_ACCESSDENIED = -2146367455;

                //
                // Zusammenfassung:
                //     The specified SAFER level is invalid.
                public const int COMADMIN_E_SAFERINVALID = -2146367454;

                //
                // Zusammenfassung:
                //     The specified user cannot write to the system registry.
                public const int COMADMIN_E_REGISTRY_ACCESSDENIED = -2146367453;

                //
                // Zusammenfassung:
                //     COM+ partitions are currently disabled.
                public const int COMADMIN_E_PARTITIONS_DISABLED = -2146367452;

                //
                // Zusammenfassung:
                //     A handler was not defined by the filter for this operation.
                public const int ERROR_FLT_NO_HANDLER_DEFINED = -2145452031;

                //
                // Zusammenfassung:
                //     A context is already defined for this object.
                public const int ERROR_FLT_CONTEXT_ALREADY_DEFINED = -2145452030;

                //
                // Zusammenfassung:
                //     Asynchronous requests are not valid for this operation.
                public const int ERROR_FLT_INVALID_ASYNCHRONOUS_REQUEST = -2145452029;

                //
                // Zusammenfassung:
                //     Disallow the Fast IO path for this operation.
                public const int ERROR_FLT_DISALLOW_FAST_IO = -2145452028;

                //
                // Zusammenfassung:
                //     An invalid name request was made. The name requested cannot be retrieved at this
                //     time.
                public const int ERROR_FLT_INVALID_NAME_REQUEST = -2145452027;

                //
                // Zusammenfassung:
                //     Posting this operation to a worker thread for further processing is not safe
                //     at this time because it could lead to a system deadlock.
                public const int ERROR_FLT_NOT_SAFE_TO_POST_OPERATION = -2145452026;

                //
                // Zusammenfassung:
                //     The Filter Manager was not initialized when a filter tried to register. Be sure
                //     that the Filter Manager is being loaded as a driver.
                public const int ERROR_FLT_NOT_INITIALIZED = -2145452025;

                //
                // Zusammenfassung:
                //     The filter is not ready for attachment to volumes because it has not finished
                //     initializing (FltStartFiltering has not been called).
                public const int ERROR_FLT_FILTER_NOT_READY = -2145452024;

                //
                // Zusammenfassung:
                //     The filter must clean up any operation-specific context at this time because
                //     it is being removed from the system before the operation is completed by the
                //     lower drivers.
                public const int ERROR_FLT_POST_OPERATION_CLEANUP = -2145452023;

                //
                // Zusammenfassung:
                //     The Filter Manager had an internal error from which it cannot recover; therefore,
                //     the operation has been failed. This is usually the result of a filter returning
                //     an invalid value from a preoperation callback.
                public const int ERROR_FLT_INTERNAL_ERROR = -2145452022;

                //
                // Zusammenfassung:
                //     The object specified for this action is in the process of being deleted; therefore,
                //     the action requested cannot be completed at this time.
                public const int ERROR_FLT_DELETING_OBJECT = -2145452021;

                //
                // Zusammenfassung:
                //     Nonpaged pool must be used for this type of context.
                public const int ERROR_FLT_MUST_BE_NONPAGED_POOL = -2145452020;

                //
                // Zusammenfassung:
                //     A duplicate handler definition has been provided for an operation.
                public const int ERROR_FLT_DUPLICATE_ENTRY = -2145452019;

                //
                // Zusammenfassung:
                //     The callback data queue has been disabled.
                public const int ERROR_FLT_CBDQ_DISABLED = -2145452018;

                //
                // Zusammenfassung:
                //     Do not attach the filter to the volume at this time.
                public const int ERROR_FLT_DO_NOT_ATTACH = -2145452017;

                //
                // Zusammenfassung:
                //     Do not detach the filter from the volume at this time.
                public const int ERROR_FLT_DO_NOT_DETACH = -2145452016;

                //
                // Zusammenfassung:
                //     An instance already exists at this altitude on the volume specified.
                public const int ERROR_FLT_INSTANCE_ALTITUDE_COLLISION = -2145452015;

                //
                // Zusammenfassung:
                //     An instance already exists with this name on the volume specified.
                public const int ERROR_FLT_INSTANCE_NAME_COLLISION = -2145452014;

                //
                // Zusammenfassung:
                //     The system could not find the filter specified.
                public const int ERROR_FLT_FILTER_NOT_FOUND = -2145452013;

                //
                // Zusammenfassung:
                //     The system could not find the volume specified.
                public const int ERROR_FLT_VOLUME_NOT_FOUND = -2145452012;

                //
                // Zusammenfassung:
                //     The system could not find the instance specified.
                public const int ERROR_FLT_INSTANCE_NOT_FOUND = -2145452011;

                //
                // Zusammenfassung:
                //     No registered context allocation definition was found for the given request.
                public const int ERROR_FLT_CONTEXT_ALLOCATION_NOT_FOUND = -2145452010;

                //
                // Zusammenfassung:
                //     An invalid parameter was specified during context registration.
                public const int ERROR_FLT_INVALID_CONTEXT_REGISTRATION = -2145452009;

                //
                // Zusammenfassung:
                //     The name requested was not found in the Filter Manager name cache and could not
                //     be retrieved from the file system.
                public const int ERROR_FLT_NAME_CACHE_MISS = -2145452008;

                //
                // Zusammenfassung:
                //     The requested device object does not exist for the given volume.
                public const int ERROR_FLT_NO_DEVICE_OBJECT = -2145452007;

                //
                // Zusammenfassung:
                //     The specified volume is already mounted.
                public const int ERROR_FLT_VOLUME_ALREADY_MOUNTED = -2145452006;

                //
                // Zusammenfassung:
                //     The specified Transaction Context is already enlisted in a transaction.
                public const int ERROR_FLT_ALREADY_ENLISTED = -2145452005;

                //
                // Zusammenfassung:
                //     The specified context is already attached to another object.
                public const int ERROR_FLT_CONTEXT_ALREADY_LINKED = -2145452004;

                //
                // Zusammenfassung:
                //     No waiter is present for the filter's reply to this message.
                public const int ERROR_FLT_NO_WAITER_FOR_REPLY = -2145452000;

                //
                // Zusammenfassung:
                //     {Display Driver Stopped Responding} The %hs display driver has stopped working
                //     normally. Save your work and reboot the system to restore full display functionality.
                //     The next time you reboot the machine a dialog will be displayed giving you a
                //     chance to report this failure to Microsoft.
                public const int ERROR_HUNG_DISPLAY_DRIVER_THREAD = -2144993279;

                //
                // Zusammenfassung:
                //     Monitor descriptor could not be obtained.
                public const int ERROR_MONITOR_NO_DESCRIPTOR = -2144989183;

                //
                // Zusammenfassung:
                //     Format of the obtained monitor descriptor is not supported by this release.
                public const int ERROR_MONITOR_UNKNOWN_DESCRIPTOR_FORMAT = -2144989182;

                //
                // Zusammenfassung:
                //     {Desktop Composition is Disabled} The operation could not be completed because
                //     desktop composition is disabled.
                public const int DWM_E_COMPOSITIONDISABLED = -2144980991;

                //
                // Zusammenfassung:
                //     {Some Desktop Composition APIs Are Not Supported While Remoting} Some desktop
                //     composition APIs are not supported while remoting. The operation is not supported
                //     while running in a remote session.
                public const int DWM_E_REMOTING_NOT_SUPPORTED = -2144980990;

                //
                // Zusammenfassung:
                //     {No DWM Redirection Surface is Available} The Desktop Window Manager (DWM) was
                //     unable to provide a redirection surface to complete the DirectX present.
                public const int DWM_E_NO_REDIRECTION_SURFACE_AVAILABLE = -2144980989;

                //
                // Zusammenfassung:
                //     {DWM Is Not Queuing Presents for the Specified Window} The window specified is
                //     not currently using queued presents.
                public const int DWM_E_NOT_QUEUING_PRESENTS = -2144980988;

                //
                // Zusammenfassung:
                //     This is an error mask to convert Trusted Platform Module (TPM) hardware errors
                //     to Win32 errors.
                public const int TPM_E_ERROR_MASK = -2144862208;

                //
                // Zusammenfassung:
                //     Authentication failed.
                public const int TPM_E_AUTHFAIL = -2144862207;

                //
                // Zusammenfassung:
                //     The index to a Platform Configuration Register (PCR), DIR, or other register
                //     is incorrect.
                public const int TPM_E_BADINDEX = -2144862206;

                //
                // Zusammenfassung:
                //     One or more parameters are bad.
                public const int TPM_E_BAD_PARAMETER = -2144862205;

                //
                // Zusammenfassung:
                //     An operation completed successfully but the auditing of that operation failed.
                public const int TPM_E_AUDITFAILURE = -2144862204;

                //
                // Zusammenfassung:
                //     The clear disable flag is set and all clear operations now require physical access.
                public const int TPM_E_CLEAR_DISABLED = -2144862203;

                //
                // Zusammenfassung:
                //     The TPM is deactivated.
                public const int TPM_E_DEACTIVATED = -2144862202;

                //
                // Zusammenfassung:
                //     The TPM is disabled.
                public const int TPM_E_DISABLED = -2144862201;

                //
                // Zusammenfassung:
                //     The target command has been disabled.
                public const int TPM_E_DISABLED_CMD = -2144862200;

                //
                // Zusammenfassung:
                //     The operation failed.
                public const int TPM_E_FAIL = -2144862199;

                //
                // Zusammenfassung:
                //     The ordinal was unknown or inconsistent.
                public const int TPM_E_BAD_ORDINAL = -2144862198;

                //
                // Zusammenfassung:
                //     The ability to install an owner is disabled.
                public const int TPM_E_INSTALL_DISABLED = -2144862197;

                //
                // Zusammenfassung:
                //     The key handle cannot be interpreted.
                public const int TPM_E_INVALID_KEYHANDLE = -2144862196;

                //
                // Zusammenfassung:
                //     The key handle points to an invalid key.
                public const int TPM_E_KEYNOTFOUND = -2144862195;

                //
                // Zusammenfassung:
                //     Unacceptable encryption scheme.
                public const int TPM_E_INAPPROPRIATE_ENC = -2144862194;

                //
                // Zusammenfassung:
                //     Migration authorization failed.
                public const int TPM_E_MIGRATEFAIL = -2144862193;

                //
                // Zusammenfassung:
                //     PCR information could not be interpreted.
                public const int TPM_E_INVALID_PCR_INFO = -2144862192;

                //
                // Zusammenfassung:
                //     No room to load key.
                public const int TPM_E_NOSPACE = -2144862191;

                //
                // Zusammenfassung:
                //     There is no storage root key (SRK) set.
                public const int TPM_E_NOSRK = -2144862190;

                //
                // Zusammenfassung:
                //     An encrypted blob is invalid or was not created by this TPM.
                public const int TPM_E_NOTSEALED_BLOB = -2144862189;

                //
                // Zusammenfassung:
                //     There is already an owner.
                public const int TPM_E_OWNER_SET = -2144862188;

                //
                // Zusammenfassung:
                //     The TPM has insufficient internal resources to perform the requested action.
                public const int TPM_E_RESOURCES = -2144862187;

                //
                // Zusammenfassung:
                //     A random string was too short.
                public const int TPM_E_SHORTRANDOM = -2144862186;

                //
                // Zusammenfassung:
                //     The TPM does not have the space to perform the operation.
                public const int TPM_E_SIZE = -2144862185;

                //
                // Zusammenfassung:
                //     The named PCR value does not match the current PCR value.
                public const int TPM_E_WRONGPCRVAL = -2144862184;

                //
                // Zusammenfassung:
                //     The paramSize argument to the command has the incorrect value.
                public const int TPM_E_BAD_PARAM_SIZE = -2144862183;

                //
                // Zusammenfassung:
                //     There is no existing SHA-1 thread.
                public const int TPM_E_SHA_THREAD = -2144862182;

                //
                // Zusammenfassung:
                //     The calculation is unable to proceed because the existing SHA-1 thread has already
                //     encountered an error.
                public const int TPM_E_SHA_ERROR = -2144862181;

                //
                // Zusammenfassung:
                //     Self-test has failed and the TPM has shut down.
                public const int TPM_E_FAILEDSELFTEST = -2144862180;

                //
                // Zusammenfassung:
                //     The authorization for the second key in a two-key function failed authorization.
                public const int TPM_E_AUTH2FAIL = -2144862179;

                //
                // Zusammenfassung:
                //     The tag value sent to for a command is invalid.
                public const int TPM_E_BADTAG = -2144862178;

                //
                // Zusammenfassung:
                //     An I/O error occurred transmitting information to the TPM.
                public const int TPM_E_IOERROR = -2144862177;

                //
                // Zusammenfassung:
                //     The encryption process had a problem.
                public const int TPM_E_ENCRYPT_ERROR = -2144862176;

                //
                // Zusammenfassung:
                //     The decryption process did not complete.
                public const int TPM_E_DECRYPT_ERROR = -2144862175;

                //
                // Zusammenfassung:
                //     An invalid handle was used.
                public const int TPM_E_INVALID_AUTHHANDLE = -2144862174;

                //
                // Zusammenfassung:
                //     The TPM does not have an endorsement key (EK) installed.
                public const int TPM_E_NO_ENDORSEMENT = -2144862173;

                //
                // Zusammenfassung:
                //     The usage of a key is not allowed.
                public const int TPM_E_INVALID_KEYUSAGE = -2144862172;

                //
                // Zusammenfassung:
                //     The submitted entity type is not allowed.
                public const int TPM_E_WRONG_ENTITYTYPE = -2144862171;

                //
                // Zusammenfassung:
                //     The command was received in the wrong sequence relative to TPM_Init and a subsequent
                //     TPM_Startup.
                public const int TPM_E_INVALID_POSTINIT = -2144862170;

                //
                // Zusammenfassung:
                //     Signed data cannot include additional DER information.
                public const int TPM_E_INAPPROPRIATE_SIG = -2144862169;

                //
                // Zusammenfassung:
                //     The key properties in TPM_KEY_PARMs are not supported by this TPM.
                public const int TPM_E_BAD_KEY_PROPERTY = -2144862168;

                //
                // Zusammenfassung:
                //     The migration properties of this key are incorrect.
                public const int TPM_E_BAD_MIGRATION = -2144862167;

                //
                // Zusammenfassung:
                //     The signature or encryption scheme for this key is incorrect or not permitted
                //     in this situation.
                public const int TPM_E_BAD_SCHEME = -2144862166;

                //
                // Zusammenfassung:
                //     The size of the data (or blob) parameter is bad or inconsistent with the referenced
                //     key.
                public const int TPM_E_BAD_DATASIZE = -2144862165;

                //
                // Zusammenfassung:
                //     A mode parameter is bad, such as capArea or subCapArea for TPM_GetCapability,
                //     physicalPresence parameter for TPM_PhysicalPresence, or migrationType for TPM_CreateMigrationBlob.
                public const int TPM_E_BAD_MODE = -2144862164;

                //
                // Zusammenfassung:
                //     Either the physicalPresence or physicalPresenceLock bits have the wrong value.
                public const int TPM_E_BAD_PRESENCE = -2144862163;

                //
                // Zusammenfassung:
                //     The TPM cannot perform this version of the capability.
                public const int TPM_E_BAD_VERSION = -2144862162;

                //
                // Zusammenfassung:
                //     The TPM does not allow for wrapped transport sessions.
                public const int TPM_E_NO_WRAP_TRANSPORT = -2144862161;

                //
                // Zusammenfassung:
                //     TPM audit construction failed and the underlying command was returning a failure
                //     code also.
                public const int TPM_E_AUDITFAIL_UNSUCCESSFUL = -2144862160;

                //
                // Zusammenfassung:
                //     TPM audit construction failed and the underlying command was returning success.
                public const int TPM_E_AUDITFAIL_SUCCESSFUL = -2144862159;

                //
                // Zusammenfassung:
                //     Attempt to reset a PCR that does not have the resettable attribute.
                public const int TPM_E_NOTRESETABLE = -2144862158;

                //
                // Zusammenfassung:
                //     Attempt to reset a PCR register that requires locality and the locality modifier
                //     not part of command transport.
                public const int TPM_E_NOTLOCAL = -2144862157;

                //
                // Zusammenfassung:
                //     Make identity blob not properly typed.
                public const int TPM_E_BAD_TYPE = -2144862156;

                //
                // Zusammenfassung:
                //     When saving context identified resource type does not match actual resource.
                public const int TPM_E_INVALID_RESOURCE = -2144862155;

                //
                // Zusammenfassung:
                //     The TPM is attempting to execute a command only available when in Federal Information
                //     Processing Standards (FIPS) mode.
                public const int TPM_E_NOTFIPS = -2144862154;

                //
                // Zusammenfassung:
                //     The command is attempting to use an invalid family ID.
                public const int TPM_E_INVALID_FAMILY = -2144862153;

                //
                // Zusammenfassung:
                //     The permission to manipulate the NV storage is not available.
                public const int TPM_E_NO_NV_PERMISSION = -2144862152;

                //
                // Zusammenfassung:
                //     The operation requires a signed command.
                public const int TPM_E_REQUIRES_SIGN = -2144862151;

                //
                // Zusammenfassung:
                //     Wrong operation to load an NV key.
                public const int TPM_E_KEY_NOTSUPPORTED = -2144862150;

                //
                // Zusammenfassung:
                //     NV_LoadKey blob requires both owner and blob authorization.
                public const int TPM_E_AUTH_CONFLICT = -2144862149;

                //
                // Zusammenfassung:
                //     The NV area is locked and not writable.
                public const int TPM_E_AREA_LOCKED = -2144862148;

                //
                // Zusammenfassung:
                //     The locality is incorrect for the attempted operation.
                public const int TPM_E_BAD_LOCALITY = -2144862147;

                //
                // Zusammenfassung:
                //     The NV area is read-only and cannot be written to.
                public const int TPM_E_READ_ONLY = -2144862146;

                //
                // Zusammenfassung:
                //     There is no protection on the write to the NV area.
                public const int TPM_E_PER_NOWRITE = -2144862145;

                //
                // Zusammenfassung:
                //     The family count value does not match.
                public const int TPM_E_FAMILYCOUNT = -2144862144;

                //
                // Zusammenfassung:
                //     The NV area has already been written to.
                public const int TPM_E_WRITE_LOCKED = -2144862143;

                //
                // Zusammenfassung:
                //     The NV area attributes conflict.
                public const int TPM_E_BAD_ATTRIBUTES = -2144862142;

                //
                // Zusammenfassung:
                //     The structure tag and version are invalid or inconsistent.
                public const int TPM_E_INVALID_STRUCTURE = -2144862141;

                //
                // Zusammenfassung:
                //     The key is under control of the TPM owner and can only be evicted by the TPM
                //     owner.
                public const int TPM_E_KEY_OWNER_CONTROL = -2144862140;

                //
                // Zusammenfassung:
                //     The counter handle is incorrect.
                public const int TPM_E_BAD_COUNTER = -2144862139;

                //
                // Zusammenfassung:
                //     The write is not a complete write of the area.
                public const int TPM_E_NOT_FULLWRITE = -2144862138;

                //
                // Zusammenfassung:
                //     The gap between saved context counts is too large.
                public const int TPM_E_CONTEXT_GAP = -2144862137;

                //
                // Zusammenfassung:
                //     The maximum number of NV writes without an owner has been exceeded.
                public const int TPM_E_MAXNVWRITES = -2144862136;

                //
                // Zusammenfassung:
                //     No operator AuthData value is set.
                public const int TPM_E_NOOPERATOR = -2144862135;

                //
                // Zusammenfassung:
                //     The resource pointed to by context is not loaded.
                public const int TPM_E_RESOURCEMISSING = -2144862134;

                //
                // Zusammenfassung:
                //     The delegate administration is locked.
                public const int TPM_E_DELEGATE_LOCK = -2144862133;

                //
                // Zusammenfassung:
                //     Attempt to manage a family other then the delegated family.
                public const int TPM_E_DELEGATE_FAMILY = -2144862132;

                //
                // Zusammenfassung:
                //     Delegation table management not enabled.
                public const int TPM_E_DELEGATE_ADMIN = -2144862131;

                //
                // Zusammenfassung:
                //     There was a command executed outside an exclusive transport session.
                public const int TPM_E_TRANSPORT_NOTEXCLUSIVE = -2144862130;

                //
                // Zusammenfassung:
                //     Attempt to context save an owner evict controlled key.
                public const int TPM_E_OWNER_CONTROL = -2144862129;

                //
                // Zusammenfassung:
                //     The DAA command has no resources available to execute the command.
                public const int TPM_E_DAA_RESOURCES = -2144862128;

                //
                // Zusammenfassung:
                //     The consistency check on DAA parameter inputData0 has failed.
                public const int TPM_E_DAA_INPUT_DATA0 = -2144862127;

                //
                // Zusammenfassung:
                //     The consistency check on DAA parameter inputData1 has failed.
                public const int TPM_E_DAA_INPUT_DATA1 = -2144862126;

                //
                // Zusammenfassung:
                //     The consistency check on DAA_issuerSettings has failed.
                public const int TPM_E_DAA_ISSUER_SETTINGS = -2144862125;

                //
                // Zusammenfassung:
                //     The consistency check on DAA_tpmSpecific has failed.
                public const int TPM_E_DAA_TPM_SETTINGS = -2144862124;

                //
                // Zusammenfassung:
                //     The atomic process indicated by the submitted DAA command is not the expected
                //     process.
                public const int TPM_E_DAA_STAGE = -2144862123;

                //
                // Zusammenfassung:
                //     The issuer's validity check has detected an inconsistency.
                public const int TPM_E_DAA_ISSUER_VALIDITY = -2144862122;

                //
                // Zusammenfassung:
                //     The consistency check on w has failed.
                public const int TPM_E_DAA_WRONG_W = -2144862121;

                //
                // Zusammenfassung:
                //     The handle is incorrect.
                public const int TPM_E_BAD_HANDLE = -2144862120;

                //
                // Zusammenfassung:
                //     Delegation is not correct.
                public const int TPM_E_BAD_DELEGATE = -2144862119;

                //
                // Zusammenfassung:
                //     The context blob is invalid.
                public const int TPM_E_BADCONTEXT = -2144862118;

                //
                // Zusammenfassung:
                //     Too many contexts held by the TPM.
                public const int TPM_E_TOOMANYCONTEXTS = -2144862117;

                //
                // Zusammenfassung:
                //     Migration authority signature validation failure.
                public const int TPM_E_MA_TICKET_SIGNATURE = -2144862116;

                //
                // Zusammenfassung:
                //     Migration destination not authenticated.
                public const int TPM_E_MA_DESTINATION = -2144862115;

                //
                // Zusammenfassung:
                //     Migration source incorrect.
                public const int TPM_E_MA_SOURCE = -2144862114;

                //
                // Zusammenfassung:
                //     Incorrect migration authority.
                public const int TPM_E_MA_AUTHORITY = -2144862113;

                //
                // Zusammenfassung:
                //     Attempt to revoke the EK and the EK is not revocable.
                public const int TPM_E_PERMANENTEK = -2144862111;

                //
                // Zusammenfassung:
                //     Bad signature of CMK ticket.
                public const int TPM_E_BAD_SIGNATURE = -2144862110;

                //
                // Zusammenfassung:
                //     There is no room in the context list for additional contexts.
                public const int TPM_E_NOCONTEXTSPACE = -2144862109;

                //
                // Zusammenfassung:
                //     The command was blocked.
                public const int TPM_E_COMMAND_BLOCKED = -2144861184;

                //
                // Zusammenfassung:
                //     The specified handle was not found.
                public const int TPM_E_INVALID_HANDLE = -2144861183;

                //
                // Zusammenfassung:
                //     The TPM returned a duplicate handle and the command needs to be resubmitted.
                public const int TPM_E_DUPLICATE_VHANDLE = -2144861182;

                //
                // Zusammenfassung:
                //     The command within the transport was blocked.
                public const int TPM_E_EMBEDDED_COMMAND_BLOCKED = -2144861181;

                //
                // Zusammenfassung:
                //     The command within the transport is not supported.
                public const int TPM_E_EMBEDDED_COMMAND_UNSUPPORTED = -2144861180;

                //
                // Zusammenfassung:
                //     The TPM is too busy to respond to the command immediately, but the command could
                //     be resubmitted at a later time.
                public const int TPM_E_RETRY = -2144860160;

                //
                // Zusammenfassung:
                //     SelfTestFull has not been run.
                public const int TPM_E_NEEDS_SELFTEST = -2144860159;

                //
                // Zusammenfassung:
                //     The TPM is currently executing a full self-test.
                public const int TPM_E_DOING_SELFTEST = -2144860158;

                //
                // Zusammenfassung:
                //     The TPM is defending against dictionary attacks and is in a time-out period.
                public const int TPM_E_DEFEND_LOCK_RUNNING = -2144860157;

                //
                // Zusammenfassung:
                //     An internal software error has been detected.
                public const int TBS_E_INTERNAL_ERROR = -2144845823;

                //
                // Zusammenfassung:
                //     One or more input parameters are bad.
                public const int TBS_E_BAD_PARAMETER = -2144845822;

                //
                // Zusammenfassung:
                //     A specified output pointer is bad.
                public const int TBS_E_INVALID_OUTPUT_POINTER = -2144845821;

                //
                // Zusammenfassung:
                //     The specified context handle does not refer to a valid context.
                public const int TBS_E_INVALID_CONTEXT = -2144845820;

                //
                // Zusammenfassung:
                //     A specified output buffer is too small.
                public const int TBS_E_INSUFFICIENT_BUFFER = -2144845819;

                //
                // Zusammenfassung:
                //     An error occurred while communicating with the TPM.
                public const int TBS_E_IOERROR = -2144845818;

                //
                // Zusammenfassung:
                //     One or more context parameters are invalid.
                public const int TBS_E_INVALID_CONTEXT_PARAM = -2144845817;

                //
                // Zusammenfassung:
                //     The TPM Base Services (TBS) is not running and could not be started.
                public const int TBS_E_SERVICE_NOT_RUNNING = -2144845816;

                //
                // Zusammenfassung:
                //     A new context could not be created because there are too many open contexts.
                public const int TBS_E_TOO_MANY_TBS_CONTEXTS = -2144845815;

                //
                // Zusammenfassung:
                //     A new virtual resource could not be created because there are too many open virtual
                //     resources.
                public const int TBS_E_TOO_MANY_RESOURCES = -2144845814;

                //
                // Zusammenfassung:
                //     The TBS service has been started but is not yet running.
                public const int TBS_E_SERVICE_START_PENDING = -2144845813;

                //
                // Zusammenfassung:
                //     The physical presence interface is not supported.
                public const int TBS_E_PPI_NOT_SUPPORTED = -2144845812;

                //
                // Zusammenfassung:
                //     The command was canceled.
                public const int TBS_E_COMMAND_CANCELED = -2144845811;

                //
                // Zusammenfassung:
                //     The input or output buffer is too large.
                public const int TBS_E_BUFFER_TOO_LARGE = -2144845810;

                //
                // Zusammenfassung:
                //     The command buffer is not in the correct state.
                public const int TPMAPI_E_INVALID_STATE = -2144796416;

                //
                // Zusammenfassung:
                //     The command buffer does not contain enough data to satisfy the request.
                public const int TPMAPI_E_NOT_ENOUGH_DATA = -2144796415;

                //
                // Zusammenfassung:
                //     The command buffer cannot contain any more data.
                public const int TPMAPI_E_TOO_MUCH_DATA = -2144796414;

                //
                // Zusammenfassung:
                //     One or more output parameters was null or invalid.
                public const int TPMAPI_E_INVALID_OUTPUT_POINTER = -2144796413;

                //
                // Zusammenfassung:
                //     One or more input parameters are invalid.
                public const int TPMAPI_E_INVALID_PARAMETER = -2144796412;

                //
                // Zusammenfassung:
                //     Not enough memory was available to satisfy the request.
                public const int TPMAPI_E_OUT_OF_MEMORY = -2144796411;

                //
                // Zusammenfassung:
                //     The specified buffer was too small.
                public const int TPMAPI_E_BUFFER_TOO_SMALL = -2144796410;

                //
                // Zusammenfassung:
                //     An internal error was detected.
                public const int TPMAPI_E_INTERNAL_ERROR = -2144796409;

                //
                // Zusammenfassung:
                //     The caller does not have the appropriate rights to perform the requested operation.
                public const int TPMAPI_E_ACCESS_DENIED = -2144796408;

                //
                // Zusammenfassung:
                //     The specified authorization information was invalid.
                public const int TPMAPI_E_AUTHORIZATION_FAILED = -2144796407;

                //
                // Zusammenfassung:
                //     The specified context handle was not valid.
                public const int TPMAPI_E_INVALID_CONTEXT_HANDLE = -2144796406;

                //
                // Zusammenfassung:
                //     An error occurred while communicating with the TBS.
                public const int TPMAPI_E_TBS_COMMUNICATION_ERROR = -2144796405;

                //
                // Zusammenfassung:
                //     The TPM returned an unexpected result.
                public const int TPMAPI_E_TPM_COMMAND_ERROR = -2144796404;

                //
                // Zusammenfassung:
                //     The message was too large for the encoding scheme.
                public const int TPMAPI_E_MESSAGE_TOO_LARGE = -2144796403;

                //
                // Zusammenfassung:
                //     The encoding in the binary large object (BLOB) was not recognized.
                public const int TPMAPI_E_INVALID_ENCODING = -2144796402;

                //
                // Zusammenfassung:
                //     The key size is not valid.
                public const int TPMAPI_E_INVALID_KEY_SIZE = -2144796401;

                //
                // Zusammenfassung:
                //     The encryption operation failed.
                public const int TPMAPI_E_ENCRYPTION_FAILED = -2144796400;

                //
                // Zusammenfassung:
                //     The key parameters structure was not valid.
                public const int TPMAPI_E_INVALID_KEY_PARAMS = -2144796399;

                //
                // Zusammenfassung:
                //     The requested supplied data does not appear to be a valid migration authorization
                //     BLOB.
                public const int TPMAPI_E_INVALID_MIGRATION_AUTHORIZATION_BLOB = -2144796398;

                //
                // Zusammenfassung:
                //     The specified PCR index was invalid.
                public const int TPMAPI_E_INVALID_PCR_INDEX = -2144796397;

                //
                // Zusammenfassung:
                //     The data given does not appear to be a valid delegate BLOB.
                public const int TPMAPI_E_INVALID_DELEGATE_BLOB = -2144796396;

                //
                // Zusammenfassung:
                //     One or more of the specified context parameters was not valid.
                public const int TPMAPI_E_INVALID_CONTEXT_PARAMS = -2144796395;

                //
                // Zusammenfassung:
                //     The data given does not appear to be a valid key BLOB.
                public const int TPMAPI_E_INVALID_KEY_BLOB = -2144796394;

                //
                // Zusammenfassung:
                //     The specified PCR data was invalid.
                public const int TPMAPI_E_INVALID_PCR_DATA = -2144796393;

                //
                // Zusammenfassung:
                //     The format of the owner authorization data was invalid.
                public const int TPMAPI_E_INVALID_OWNER_AUTH = -2144796392;

                //
                // Zusammenfassung:
                //     The specified buffer was too small.
                public const int TBSIMP_E_BUFFER_TOO_SMALL = -2144796160;

                //
                // Zusammenfassung:
                //     The context could not be cleaned up.
                public const int TBSIMP_E_CLEANUP_FAILED = -2144796159;

                //
                // Zusammenfassung:
                //     The specified context handle is invalid.
                public const int TBSIMP_E_INVALID_CONTEXT_HANDLE = -2144796158;

                //
                // Zusammenfassung:
                //     An invalid context parameter was specified.
                public const int TBSIMP_E_INVALID_CONTEXT_PARAM = -2144796157;

                //
                // Zusammenfassung:
                //     An error occurred while communicating with the TPM.
                public const int TBSIMP_E_TPM_ERROR = -2144796156;

                //
                // Zusammenfassung:
                //     No entry with the specified key was found.
                public const int TBSIMP_E_HASH_BAD_KEY = -2144796155;

                //
                // Zusammenfassung:
                //     The specified virtual handle matches a virtual handle already in use.
                public const int TBSIMP_E_DUPLICATE_VHANDLE = -2144796154;

                //
                // Zusammenfassung:
                //     The pointer to the returned handle location was null or invalid.
                public const int TBSIMP_E_INVALID_OUTPUT_POINTER = -2144796153;

                //
                // Zusammenfassung:
                //     One or more parameters are invalid.
                public const int TBSIMP_E_INVALID_PARAMETER = -2144796152;

                //
                // Zusammenfassung:
                //     The RPC subsystem could not be initialized.
                public const int TBSIMP_E_RPC_INIT_FAILED = -2144796151;

                //
                // Zusammenfassung:
                //     The TBS scheduler is not running.
                public const int TBSIMP_E_SCHEDULER_NOT_RUNNING = -2144796150;

                //
                // Zusammenfassung:
                //     The command was canceled.
                public const int TBSIMP_E_COMMAND_CANCELED = -2144796149;

                //
                // Zusammenfassung:
                //     There was not enough memory to fulfill the request.
                public const int TBSIMP_E_OUT_OF_MEMORY = -2144796148;

                //
                // Zusammenfassung:
                //     The specified list is empty, or the iteration has reached the end of the list.
                public const int TBSIMP_E_LIST_NO_MORE_ITEMS = -2144796147;

                //
                // Zusammenfassung:
                //     The specified item was not found in the list.
                public const int TBSIMP_E_LIST_NOT_FOUND = -2144796146;

                //
                // Zusammenfassung:
                //     The TPM does not have enough space to load the requested resource.
                public const int TBSIMP_E_NOT_ENOUGH_SPACE = -2144796145;

                //
                // Zusammenfassung:
                //     There are too many TPM contexts in use.
                public const int TBSIMP_E_NOT_ENOUGH_TPM_CONTEXTS = -2144796144;

                //
                // Zusammenfassung:
                //     The TPM command failed.
                public const int TBSIMP_E_COMMAND_FAILED = -2144796143;

                //
                // Zusammenfassung:
                //     The TBS does not recognize the specified ordinal.
                public const int TBSIMP_E_UNKNOWN_ORDINAL = -2144796142;

                //
                // Zusammenfassung:
                //     The requested resource is no longer available.
                public const int TBSIMP_E_RESOURCE_EXPIRED = -2144796141;

                //
                // Zusammenfassung:
                //     The resource type did not match.
                public const int TBSIMP_E_INVALID_RESOURCE = -2144796140;

                //
                // Zusammenfassung:
                //     No resources can be unloaded.
                public const int TBSIMP_E_NOTHING_TO_UNLOAD = -2144796139;

                //
                // Zusammenfassung:
                //     No new entries can be added to the hash table.
                public const int TBSIMP_E_HASH_TABLE_FULL = -2144796138;

                //
                // Zusammenfassung:
                //     A new TBS context could not be created because there are too many open contexts.
                public const int TBSIMP_E_TOO_MANY_TBS_CONTEXTS = -2144796137;

                //
                // Zusammenfassung:
                //     A new virtual resource could not be created because there are too many open virtual
                //     resources.
                public const int TBSIMP_E_TOO_MANY_RESOURCES = -2144796136;

                //
                // Zusammenfassung:
                //     The physical presence interface is not supported.
                public const int TBSIMP_E_PPI_NOT_SUPPORTED = -2144796135;

                //
                // Zusammenfassung:
                //     TBS is not compatible with the version of TPM found on the system.
                public const int TBSIMP_E_TPM_INCOMPATIBLE = -2144796134;

                //
                // Zusammenfassung:
                //     A general error was detected when attempting to acquire the BIOS response to
                //     a physical presence command.
                public const int TPM_E_PPI_ACPI_FAILURE = -2144795904;

                //
                // Zusammenfassung:
                //     The user failed to confirm the TPM operation request.
                public const int TPM_E_PPI_USER_ABORT = -2144795903;

                //
                // Zusammenfassung:
                //     The BIOS failure prevented the successful execution of the requested TPM operation
                //     (for example, invalid TPM operation request, BIOS communication error with the
                //     TPM).
                public const int TPM_E_PPI_BIOS_FAILURE = -2144795902;

                //
                // Zusammenfassung:
                //     The BIOS does not support the physical presence interface.
                public const int TPM_E_PPI_NOT_SUPPORTED = -2144795901;

                //
                // Zusammenfassung:
                //     A Data Collector Set was not found.
                public const int PLA_E_DCS_NOT_FOUND = -2144337918;

                //
                // Zusammenfassung:
                //     Unable to start Data Collector Set because there are too many folders.
                public const int PLA_E_TOO_MANY_FOLDERS = -2144337851;

                //
                // Zusammenfassung:
                //     Not enough free disk space to start Data Collector Set.
                public const int PLA_E_NO_MIN_DISK = -2144337808;

                //
                // Zusammenfassung:
                //     Data Collector Set is in use.
                public const int PLA_E_DCS_IN_USE = -2144337750;

                //
                // Zusammenfassung:
                //     Data Collector Set already exists.
                public const int PLA_E_DCS_ALREADY_EXISTS = -2144337737;

                //
                // Zusammenfassung:
                //     Property value conflict.
                public const int PLA_E_PROPERTY_CONFLICT = -2144337663;

                //
                // Zusammenfassung:
                //     The current configuration for this Data Collector Set requires that it contain
                //     exactly one Data Collector.
                public const int PLA_E_DCS_SINGLETON_REQUIRED = -2144337662;

                //
                // Zusammenfassung:
                //     A user account is required to commit the current Data Collector Set properties.
                public const int PLA_E_CREDENTIALS_REQUIRED = -2144337661;

                //
                // Zusammenfassung:
                //     Data Collector Set is not running.
                public const int PLA_E_DCS_NOT_RUNNING = -2144337660;

                //
                // Zusammenfassung:
                //     A conflict was detected in the list of include and exclude APIs. Do not specify
                //     the same API in both the include list and the exclude list.
                public const int PLA_E_CONFLICT_INCL_EXCL_API = -2144337659;

                //
                // Zusammenfassung:
                //     The executable path specified refers to a network share or UNC path.
                public const int PLA_E_NETWORK_EXE_NOT_VALID = -2144337658;

                //
                // Zusammenfassung:
                //     The executable path specified is already configured for API tracing.
                public const int PLA_E_EXE_ALREADY_CONFIGURED = -2144337657;

                //
                // Zusammenfassung:
                //     The executable path specified does not exist. Verify that the specified path
                //     is correct.
                public const int PLA_E_EXE_PATH_NOT_VALID = -2144337656;

                //
                // Zusammenfassung:
                //     Data Collector already exists.
                public const int PLA_E_DC_ALREADY_EXISTS = -2144337655;

                //
                // Zusammenfassung:
                //     The wait for the Data Collector Set start notification has timed out.
                public const int PLA_E_DCS_START_WAIT_TIMEOUT = -2144337654;

                //
                // Zusammenfassung:
                //     The wait for the Data Collector to start has timed out.
                public const int PLA_E_DC_START_WAIT_TIMEOUT = -2144337653;

                //
                // Zusammenfassung:
                //     The wait for the report generation tool to finish has timed out.
                public const int PLA_E_REPORT_WAIT_TIMEOUT = -2144337652;

                //
                // Zusammenfassung:
                //     Duplicate items are not allowed.
                public const int PLA_E_NO_DUPLICATES = -2144337651;

                //
                // Zusammenfassung:
                //     When specifying the executable to trace, you must specify a full path to the
                //     executable and not just a file name.
                public const int PLA_E_EXE_FULL_PATH_REQUIRED = -2144337650;

                //
                // Zusammenfassung:
                //     The session name provided is invalid.
                public const int PLA_E_INVALID_SESSION_NAME = -2144337649;

                //
                // Zusammenfassung:
                //     The Event Log channel Microsoft-Windows-Diagnosis-PLA/Operational must be enabled
                //     to perform this operation.
                public const int PLA_E_PLA_CHANNEL_NOT_ENABLED = -2144337648;

                //
                // Zusammenfassung:
                //     The Event Log channel Microsoft-Windows-TaskScheduler must be enabled to perform
                //     this operation.
                public const int PLA_E_TASKSCHED_CHANNEL_NOT_ENABLED = -2144337647;

                //
                // Zusammenfassung:
                //     The volume must be unlocked before it can be used.
                public const int FVE_E_LOCKED_VOLUME = -2144272384;

                //
                // Zusammenfassung:
                //     The volume is fully decrypted and no key is available.
                public const int FVE_E_NOT_ENCRYPTED = -2144272383;

                //
                // Zusammenfassung:
                //     The firmware does not support using a TPM during boot.
                public const int FVE_E_NO_TPM_BIOS = -2144272382;

                //
                // Zusammenfassung:
                //     The firmware does not use a TPM to perform initial program load (IPL) measurement.
                public const int FVE_E_NO_MBR_METRIC = -2144272381;

                //
                // Zusammenfassung:
                //     The master boot record (MBR) is not TPM-aware.
                public const int FVE_E_NO_BOOTSECTOR_METRIC = -2144272380;

                //
                // Zusammenfassung:
                //     The BOOTMGR is not being measured by the TPM.
                public const int FVE_E_NO_BOOTMGR_METRIC = -2144272379;

                //
                // Zusammenfassung:
                //     The BOOTMGR component does not perform expected TPM measurements.
                public const int FVE_E_WRONG_BOOTMGR = -2144272378;

                //
                // Zusammenfassung:
                //     No secure key protection mechanism has been defined.
                public const int FVE_E_SECURE_KEY_REQUIRED = -2144272377;

                //
                // Zusammenfassung:
                //     This volume has not been provisioned for encryption.
                public const int FVE_E_NOT_ACTIVATED = -2144272376;

                //
                // Zusammenfassung:
                //     Requested action was denied by the full-volume encryption (FVE) control engine.
                public const int FVE_E_ACTION_NOT_ALLOWED = -2144272375;

                //
                // Zusammenfassung:
                //     The Active Directory forest does not contain the required attributes and classes
                //     to host FVE or TPM information.
                public const int FVE_E_AD_SCHEMA_NOT_INSTALLED = -2144272374;

                //
                // Zusammenfassung:
                //     The type of data obtained from Active Directory was not expected.
                public const int FVE_E_AD_INVALID_DATATYPE = -2144272373;

                //
                // Zusammenfassung:
                //     The size of the data obtained from Active Directory was not expected.
                public const int FVE_E_AD_INVALID_DATASIZE = -2144272372;

                //
                // Zusammenfassung:
                //     The attribute read from Active Directory has no (zero) values.
                public const int FVE_E_AD_NO_VALUES = -2144272371;

                //
                // Zusammenfassung:
                //     The attribute was not set.
                public const int FVE_E_AD_ATTR_NOT_SET = -2144272370;

                //
                // Zusammenfassung:
                //     The specified GUID could not be found.
                public const int FVE_E_AD_GUID_NOT_FOUND = -2144272369;

                //
                // Zusammenfassung:
                //     The control block for the encrypted volume is not valid.
                public const int FVE_E_BAD_INFORMATION = -2144272368;

                //
                // Zusammenfassung:
                //     Not enough free space remaining on volume to allow encryption.
                public const int FVE_E_TOO_SMALL = -2144272367;

                //
                // Zusammenfassung:
                //     The volume cannot be encrypted because it is required to boot the operating system.
                public const int FVE_E_SYSTEM_VOLUME = -2144272366;

                //
                // Zusammenfassung:
                //     The volume cannot be encrypted because the file system is not supported.
                public const int FVE_E_FAILED_WRONG_FS = -2144272365;

                //
                // Zusammenfassung:
                //     The file system is inconsistent. Run CHKDSK.
                public const int FVE_E_FAILED_BAD_FS = -2144272364;

                //
                // Zusammenfassung:
                //     This volume cannot be encrypted.
                public const int FVE_E_NOT_SUPPORTED = -2144272363;

                //
                // Zusammenfassung:
                //     Data supplied is malformed.
                public const int FVE_E_BAD_DATA = -2144272362;

                //
                // Zusammenfassung:
                //     Volume is not bound to the system.
                public const int FVE_E_VOLUME_NOT_BOUND = -2144272361;

                //
                // Zusammenfassung:
                //     TPM must be owned before a volume can be bound to it.
                public const int FVE_E_TPM_NOT_OWNED = -2144272360;

                //
                // Zusammenfassung:
                //     The volume specified is not a data volume.
                public const int FVE_E_NOT_DATA_VOLUME = -2144272359;

                //
                // Zusammenfassung:
                //     The buffer supplied to a function was insufficient to contain the returned data.
                public const int FVE_E_AD_INSUFFICIENT_BUFFER = -2144272358;

                //
                // Zusammenfassung:
                //     A read operation failed while converting the volume.
                public const int FVE_E_CONV_READ = -2144272357;

                //
                // Zusammenfassung:
                //     A write operation failed while converting the volume.
                public const int FVE_E_CONV_WRITE = -2144272356;

                //
                // Zusammenfassung:
                //     One or more key protection mechanisms are required for this volume.
                public const int FVE_E_KEY_REQUIRED = -2144272355;

                //
                // Zusammenfassung:
                //     Cluster configurations are not supported.
                public const int FVE_E_CLUSTERING_NOT_SUPPORTED = -2144272354;

                //
                // Zusammenfassung:
                //     The volume is already bound to the system.
                public const int FVE_E_VOLUME_BOUND_ALREADY = -2144272353;

                //
                // Zusammenfassung:
                //     The boot OS volume is not being protected via FVE.
                public const int FVE_E_OS_NOT_PROTECTED = -2144272352;

                //
                // Zusammenfassung:
                //     All protection mechanisms are effectively disabled (clear key exists).
                public const int FVE_E_PROTECTION_DISABLED = -2144272351;

                //
                // Zusammenfassung:
                //     A recovery key protection mechanism is required.
                public const int FVE_E_RECOVERY_KEY_REQUIRED = -2144272350;

                //
                // Zusammenfassung:
                //     This volume cannot be bound to a TPM.
                public const int FVE_E_FOREIGN_VOLUME = -2144272349;

                //
                // Zusammenfassung:
                //     The control block for the encrypted volume was updated by another thread. Try
                //     again.
                public const int FVE_E_OVERLAPPED_UPDATE = -2144272348;

                //
                // Zusammenfassung:
                //     The SRK authentication of the TPM is not zero and, therefore, is not compatible.
                public const int FVE_E_TPM_SRK_AUTH_NOT_ZERO = -2144272347;

                //
                // Zusammenfassung:
                //     The volume encryption algorithm cannot be used on this sector size.
                public const int FVE_E_FAILED_SECTOR_SIZE = -2144272346;

                //
                // Zusammenfassung:
                //     BitLocker recovery authentication failed.
                public const int FVE_E_FAILED_AUTHENTICATION = -2144272345;

                //
                // Zusammenfassung:
                //     The volume specified is not the boot OS volume.
                public const int FVE_E_NOT_OS_VOLUME = -2144272344;

                //
                // Zusammenfassung:
                //     Auto-unlock information for data volumes is present on the boot OS volume.
                public const int FVE_E_AUTOUNLOCK_ENABLED = -2144272343;

                //
                // Zusammenfassung:
                //     The system partition boot sector does not perform TPM measurements.
                public const int FVE_E_WRONG_BOOTSECTOR = -2144272342;

                //
                // Zusammenfassung:
                //     The system partition file system must be NTFS.
                public const int FVE_E_WRONG_SYSTEM_FS = -2144272341;

                //
                // Zusammenfassung:
                //     Group policy requires a recovery password before encryption can begin.
                public const int FVE_E_POLICY_PASSWORD_REQUIRED = -2144272340;

                //
                // Zusammenfassung:
                //     The volume encryption algorithm and key cannot be set on an encrypted volume.
                public const int FVE_E_CANNOT_SET_FVEK_ENCRYPTED = -2144272339;

                //
                // Zusammenfassung:
                //     A key must be specified before encryption can begin.
                public const int FVE_E_CANNOT_ENCRYPT_NO_KEY = -2144272338;

                //
                // Zusammenfassung:
                //     A bootable CD/DVD is in the system. Remove the CD/DVD and reboot the system.
                public const int FVE_E_BOOTABLE_CDDVD = -2144272336;

                //
                // Zusammenfassung:
                //     An instance of this key protector already exists on the volume.
                public const int FVE_E_PROTECTOR_EXISTS = -2144272335;

                //
                // Zusammenfassung:
                //     The file cannot be saved to a relative path.
                public const int FVE_E_RELATIVE_PATH = -2144272334;

                //
                // Zusammenfassung:
                //     The callout does not exist.
                public const int FWP_E_CALLOUT_NOT_FOUND = -2144206847;

                //
                // Zusammenfassung:
                //     The filter condition does not exist.
                public const int FWP_E_CONDITION_NOT_FOUND = -2144206846;

                //
                // Zusammenfassung:
                //     The filter does not exist.
                public const int FWP_E_FILTER_NOT_FOUND = -2144206845;

                //
                // Zusammenfassung:
                //     The layer does not exist.
                public const int FWP_E_LAYER_NOT_FOUND = -2144206844;

                //
                // Zusammenfassung:
                //     The provider does not exist.
                public const int FWP_E_PROVIDER_NOT_FOUND = -2144206843;

                //
                // Zusammenfassung:
                //     The provider context does not exist.
                public const int FWP_E_PROVIDER_CONTEXT_NOT_FOUND = -2144206842;

                //
                // Zusammenfassung:
                //     The sublayer does not exist.
                public const int FWP_E_SUBLAYER_NOT_FOUND = -2144206841;

                //
                // Zusammenfassung:
                //     The object does not exist.
                public const int FWP_E_NOT_FOUND = -2144206840;

                //
                // Zusammenfassung:
                //     An object with that GUID or LUID already exists.
                public const int FWP_E_ALREADY_EXISTS = -2144206839;

                //
                // Zusammenfassung:
                //     The object is referenced by other objects and, therefore, cannot be deleted.
                public const int FWP_E_IN_USE = -2144206838;

                //
                // Zusammenfassung:
                //     The call is not allowed from within a dynamic session.
                public const int FWP_E_DYNAMIC_SESSION_IN_PROGRESS = -2144206837;

                //
                // Zusammenfassung:
                //     The call was made from the wrong session and, therefore, cannot be completed.
                public const int FWP_E_WRONG_SESSION = -2144206836;

                //
                // Zusammenfassung:
                //     The call must be made from within an explicit transaction.
                public const int FWP_E_NO_TXN_IN_PROGRESS = -2144206835;

                //
                // Zusammenfassung:
                //     The call is not allowed from within an explicit transaction.
                public const int FWP_E_TXN_IN_PROGRESS = -2144206834;

                //
                // Zusammenfassung:
                //     The explicit transaction has been forcibly canceled.
                public const int FWP_E_TXN_ABORTED = -2144206833;

                //
                // Zusammenfassung:
                //     The session has been canceled.
                public const int FWP_E_SESSION_ABORTED = -2144206832;

                //
                // Zusammenfassung:
                //     The call is not allowed from within a read-only transaction.
                public const int FWP_E_INCOMPATIBLE_TXN = -2144206831;

                //
                // Zusammenfassung:
                //     The call timed out while waiting to acquire the transaction lock.
                public const int FWP_E_TIMEOUT = -2144206830;

                //
                // Zusammenfassung:
                //     Collection of network diagnostic events is disabled.
                public const int FWP_E_NET_EVENTS_DISABLED = -2144206829;

                //
                // Zusammenfassung:
                //     The operation is not supported by the specified layer.
                public const int FWP_E_INCOMPATIBLE_LAYER = -2144206828;

                //
                // Zusammenfassung:
                //     The call is allowed for kernel-mode callers only.
                public const int FWP_E_KM_CLIENTS_ONLY = -2144206827;

                //
                // Zusammenfassung:
                //     The call tried to associate two objects with incompatible lifetimes.
                public const int FWP_E_LIFETIME_MISMATCH = -2144206826;

                //
                // Zusammenfassung:
                //     The object is built in and, therefore, cannot be deleted.
                public const int FWP_E_BUILTIN_OBJECT = -2144206825;

                //
                // Zusammenfassung:
                //     The maximum number of boot-time filters has been reached.
                public const int FWP_E_TOO_MANY_BOOTTIME_FILTERS = -2144206824;

                //
                // Zusammenfassung:
                //     A notification could not be delivered because a message queue is at its maximum
                //     capacity.
                public const int FWP_E_NOTIFICATION_DROPPED = -2144206823;

                //
                // Zusammenfassung:
                //     The traffic parameters do not match those for the security association context.
                public const int FWP_E_TRAFFIC_MISMATCH = -2144206822;

                //
                // Zusammenfassung:
                //     The call is not allowed for the current security association state.
                public const int FWP_E_INCOMPATIBLE_SA_STATE = -2144206821;

                //
                // Zusammenfassung:
                //     A required pointer is null.
                public const int FWP_E_NULL_POINTER = -2144206820;

                //
                // Zusammenfassung:
                //     An enumerator is not valid.
                public const int FWP_E_INVALID_ENUMERATOR = -2144206819;

                //
                // Zusammenfassung:
                //     The flags field contains an invalid value.
                public const int FWP_E_INVALID_FLAGS = -2144206818;

                //
                // Zusammenfassung:
                //     A network mask is not valid.
                public const int FWP_E_INVALID_NET_MASK = -2144206817;

                //
                // Zusammenfassung:
                //     An FWP_RANGE is not valid.
                public const int FWP_E_INVALID_RANGE = -2144206816;

                //
                // Zusammenfassung:
                //     The time interval is not valid.
                public const int FWP_E_INVALID_INTERVAL = -2144206815;

                //
                // Zusammenfassung:
                //     An array that must contain at least one element that is zero-length.
                public const int FWP_E_ZERO_LENGTH_ARRAY = -2144206814;

                //
                // Zusammenfassung:
                //     The displayData.name field cannot be null.
                public const int FWP_E_NULL_DISPLAY_NAME = -2144206813;

                //
                // Zusammenfassung:
                //     The action type is not one of the allowed action types for a filter.
                public const int FWP_E_INVALID_ACTION_TYPE = -2144206812;

                //
                // Zusammenfassung:
                //     The filter weight is not valid.
                public const int FWP_E_INVALID_WEIGHT = -2144206811;

                //
                // Zusammenfassung:
                //     A filter condition contains a match type that is not compatible with the operands.
                public const int FWP_E_MATCH_TYPE_MISMATCH = -2144206810;

                //
                // Zusammenfassung:
                //     An FWP_VALUE or FWPM_CONDITION_VALUE is of the wrong type.
                public const int FWP_E_TYPE_MISMATCH = -2144206809;

                //
                // Zusammenfassung:
                //     An integer value is outside the allowed range.
                public const int FWP_E_OUT_OF_BOUNDS = -2144206808;

                //
                // Zusammenfassung:
                //     A reserved field is nonzero.
                public const int FWP_E_RESERVED = -2144206807;

                //
                // Zusammenfassung:
                //     A filter cannot contain multiple conditions operating on a single field.
                public const int FWP_E_DUPLICATE_CONDITION = -2144206806;

                //
                // Zusammenfassung:
                //     A policy cannot contain the same keying module more than once.
                public const int FWP_E_DUPLICATE_KEYMOD = -2144206805;

                //
                // Zusammenfassung:
                //     The action type is not compatible with the layer.
                public const int FWP_E_ACTION_INCOMPATIBLE_WITH_LAYER = -2144206804;

                //
                // Zusammenfassung:
                //     The action type is not compatible with the sublayer.
                public const int FWP_E_ACTION_INCOMPATIBLE_WITH_SUBLAYER = -2144206803;

                //
                // Zusammenfassung:
                //     The raw context or the provider context is not compatible with the layer.
                public const int FWP_E_CONTEXT_INCOMPATIBLE_WITH_LAYER = -2144206802;

                //
                // Zusammenfassung:
                //     The raw context or the provider context is not compatible with the callout.
                public const int FWP_E_CONTEXT_INCOMPATIBLE_WITH_CALLOUT = -2144206801;

                //
                // Zusammenfassung:
                //     The authentication method is not compatible with the policy type.
                public const int FWP_E_INCOMPATIBLE_AUTH_METHOD = -2144206800;

                //
                // Zusammenfassung:
                //     The Diffie-Hellman group is not compatible with the policy type.
                public const int FWP_E_INCOMPATIBLE_DH_GROUP = -2144206799;

                //
                // Zusammenfassung:
                //     An Internet Key Exchange (IKE) policy cannot contain an Extended Mode policy.
                public const int FWP_E_EM_NOT_SUPPORTED = -2144206798;

                //
                // Zusammenfassung:
                //     The enumeration template or subscription will never match any objects.
                public const int FWP_E_NEVER_MATCH = -2144206797;

                //
                // Zusammenfassung:
                //     The provider context is of the wrong type.
                public const int FWP_E_PROVIDER_CONTEXT_MISMATCH = -2144206796;

                //
                // Zusammenfassung:
                //     The parameter is incorrect.
                public const int FWP_E_INVALID_PARAMETER = -2144206795;

                //
                // Zusammenfassung:
                //     The maximum number of sublayers has been reached.
                public const int FWP_E_TOO_MANY_SUBLAYERS = -2144206794;

                //
                // Zusammenfassung:
                //     The notification function for a callout returned an error.
                public const int FWP_E_CALLOUT_NOTIFICATION_FAILED = -2144206793;

                //
                // Zusammenfassung:
                //     The IPsec authentication configuration is not compatible with the authentication
                //     type.
                public const int FWP_E_INCOMPATIBLE_AUTH_CONFIG = -2144206792;

                //
                // Zusammenfassung:
                //     The IPsec cipher configuration is not compatible with the cipher type.
                public const int FWP_E_INCOMPATIBLE_CIPHER_CONFIG = -2144206791;

                //
                // Zusammenfassung:
                //     The binding to the network interface is being closed.
                public const int ERROR_NDIS_INTERFACE_CLOSING = -2144075774;

                //
                // Zusammenfassung:
                //     An invalid version was specified.
                public const int ERROR_NDIS_BAD_VERSION = -2144075772;

                //
                // Zusammenfassung:
                //     An invalid characteristics table was used.
                public const int ERROR_NDIS_BAD_CHARACTERISTICS = -2144075771;

                //
                // Zusammenfassung:
                //     Failed to find the network interface, or the network interface is not ready.
                public const int ERROR_NDIS_ADAPTER_NOT_FOUND = -2144075770;

                //
                // Zusammenfassung:
                //     Failed to open the network interface.
                public const int ERROR_NDIS_OPEN_FAILED = -2144075769;

                //
                // Zusammenfassung:
                //     The network interface has encountered an internal unrecoverable failure.
                public const int ERROR_NDIS_DEVICE_FAILED = -2144075768;

                //
                // Zusammenfassung:
                //     The multicast list on the network interface is full.
                public const int ERROR_NDIS_MULTICAST_FULL = -2144075767;

                //
                // Zusammenfassung:
                //     An attempt was made to add a duplicate multicast address to the list.
                public const int ERROR_NDIS_MULTICAST_EXISTS = -2144075766;

                //
                // Zusammenfassung:
                //     At attempt was made to remove a multicast address that was never added.
                public const int ERROR_NDIS_MULTICAST_NOT_FOUND = -2144075765;

                //
                // Zusammenfassung:
                //     The network interface aborted the request.
                public const int ERROR_NDIS_REQUEST_ABORTED = -2144075764;

                //
                // Zusammenfassung:
                //     The network interface cannot process the request because it is being reset.
                public const int ERROR_NDIS_RESET_IN_PROGRESS = -2144075763;

                //
                // Zusammenfassung:
                //     An attempt was made to send an invalid packet on a network interface.
                public const int ERROR_NDIS_INVALID_PACKET = -2144075761;

                //
                // Zusammenfassung:
                //     The specified request is not a valid operation for the target device.
                public const int ERROR_NDIS_INVALID_DEVICE_REQUEST = -2144075760;

                //
                // Zusammenfassung:
                //     The network interface is not ready to complete this operation.
                public const int ERROR_NDIS_ADAPTER_NOT_READY = -2144075759;

                //
                // Zusammenfassung:
                //     The length of the buffer submitted for this operation is not valid.
                public const int ERROR_NDIS_INVALID_LENGTH = -2144075756;

                //
                // Zusammenfassung:
                //     The data used for this operation is not valid.
                public const int ERROR_NDIS_INVALID_DATA = -2144075755;

                //
                // Zusammenfassung:
                //     The length of the buffer submitted for this operation is too small.
                public const int ERROR_NDIS_BUFFER_TOO_SHORT = -2144075754;

                //
                // Zusammenfassung:
                //     The network interface does not support this OID.
                public const int ERROR_NDIS_INVALID_OID = -2144075753;

                //
                // Zusammenfassung:
                //     The network interface has been removed.
                public const int ERROR_NDIS_ADAPTER_REMOVED = -2144075752;

                //
                // Zusammenfassung:
                //     The network interface does not support this media type.
                public const int ERROR_NDIS_UNSUPPORTED_MEDIA = -2144075751;

                //
                // Zusammenfassung:
                //     An attempt was made to remove a token ring group address that is in use by other
                //     components.
                public const int ERROR_NDIS_GROUP_ADDRESS_IN_USE = -2144075750;

                //
                // Zusammenfassung:
                //     An attempt was made to map a file that cannot be found.
                public const int ERROR_NDIS_FILE_NOT_FOUND = -2144075749;

                //
                // Zusammenfassung:
                //     An error occurred while the NDIS tried to map the file.
                public const int ERROR_NDIS_ERROR_READING_FILE = -2144075748;

                //
                // Zusammenfassung:
                //     An attempt was made to map a file that is already mapped.
                public const int ERROR_NDIS_ALREADY_MAPPED = -2144075747;

                //
                // Zusammenfassung:
                //     An attempt to allocate a hardware resource failed because the resource is used
                //     by another component.
                public const int ERROR_NDIS_RESOURCE_CONFLICT = -2144075746;

                //
                // Zusammenfassung:
                //     The I/O operation failed because network media is disconnected or the wireless
                //     access point is out of range.
                public const int ERROR_NDIS_MEDIA_DISCONNECTED = -2144075745;

                //
                // Zusammenfassung:
                //     The network address used in the request is invalid.
                public const int ERROR_NDIS_INVALID_ADDRESS = -2144075742;

                //
                // Zusammenfassung:
                //     The offload operation on the network interface has been paused.
                public const int ERROR_NDIS_PAUSED = -2144075734;

                //
                // Zusammenfassung:
                //     The network interface was not found.
                public const int ERROR_NDIS_INTERFACE_NOT_FOUND = -2144075733;

                //
                // Zusammenfassung:
                //     The revision number specified in the structure is not supported.
                public const int ERROR_NDIS_UNSUPPORTED_REVISION = -2144075732;

                //
                // Zusammenfassung:
                //     The specified port does not exist on this network interface.
                public const int ERROR_NDIS_INVALID_PORT = -2144075731;

                //
                // Zusammenfassung:
                //     The current state of the specified port on this network interface does not support
                //     the requested operation.
                public const int ERROR_NDIS_INVALID_PORT_STATE = -2144075730;

                //
                // Zusammenfassung:
                //     The network interface does not support this request.
                public const int ERROR_NDIS_NOT_SUPPORTED = -2144075589;

                //
                // Zusammenfassung:
                //     The wireless local area network (LAN) interface is in auto-configuration mode
                //     and does not support the requested parameter change operation.
                public const int ERROR_NDIS_DOT11_AUTO_CONFIG_ENABLED = -2144067584;

                //
                // Zusammenfassung:
                //     The wireless LAN interface is busy and cannot perform the requested operation.
                public const int ERROR_NDIS_DOT11_MEDIA_IN_USE = -2144067583;

                //
                // Zusammenfassung:
                //     The wireless LAN interface is shutting down and does not support the requested
                //     operation.
                public const int ERROR_NDIS_DOT11_POWER_STATE_INVALID = -2144067582;

                //
                // Zusammenfassung:
                //     A requested object was not found.
                public const int TRK_E_NOT_FOUND = -1913991141;

                //
                // Zusammenfassung:
                //     The server received a CREATE_VOLUME subrequest of a SYNC_VOLUMES request, but
                //     the ServerVolumeTable size limit for the RequestMachine has already been reached.
                public const int TRK_E_VOLUME_QUOTA_EXCEEDED = -1913991140;

                //
                // Zusammenfassung:
                //     The server is busy, and the client should retry the request at a later time.
                public const int TRK_SERVER_TOO_BUSY = -1913991138;

                //
                // Zusammenfassung:
                //     The specified event is currently not being audited.
                public const int ERROR_AUDITING_DISABLED = -1073151999;

                //
                // Zusammenfassung:
                //     The SID filtering operation removed all SIDs.
                public const int ERROR_ALL_SIDS_FILTERED = -1073151998;

                //
                // Zusammenfassung:
                //     Business rule scripts are disabled for the calling application.
                public const int ERROR_BIZRULES_NOT_ENABLED = -1073151997;

                //
                // Zusammenfassung:
                //     There is no connection established with the Windows Media server. The operation
                //     failed.
                public const int NS_E_NOCONNECTION = -1072889851;

                //
                // Zusammenfassung:
                //     Unable to establish a connection to the server.
                public const int NS_E_CANNOTCONNECT = -1072889850;

                //
                // Zusammenfassung:
                //     Unable to destroy the title.
                public const int NS_E_CANNOTDESTROYTITLE = -1072889849;

                //
                // Zusammenfassung:
                //     Unable to rename the title.
                public const int NS_E_CANNOTRENAMETITLE = -1072889848;

                //
                // Zusammenfassung:
                //     Unable to offline disk.
                public const int NS_E_CANNOTOFFLINEDISK = -1072889847;

                //
                // Zusammenfassung:
                //     Unable to online disk.
                public const int NS_E_CANNOTONLINEDISK = -1072889846;

                //
                // Zusammenfassung:
                //     There is no file parser registered for this type of file.
                public const int NS_E_NOREGISTEREDWALKER = -1072889845;

                //
                // Zusammenfassung:
                //     There is no data connection established.
                public const int NS_E_NOFUNNEL = -1072889844;

                //
                // Zusammenfassung:
                //     Failed to load the local play DLL.
                public const int NS_E_NO_LOCALPLAY = -1072889843;

                //
                // Zusammenfassung:
                //     The network is busy.
                public const int NS_E_NETWORK_BUSY = -1072889842;

                //
                // Zusammenfassung:
                //     The server session limit was exceeded.
                public const int NS_E_TOO_MANY_SESS = -1072889841;

                //
                // Zusammenfassung:
                //     The network connection already exists.
                public const int NS_E_ALREADY_CONNECTED = -1072889840;

                //
                // Zusammenfassung:
                //     Index %1 is invalid.
                public const int NS_E_INVALID_INDEX = -1072889839;

                //
                // Zusammenfassung:
                //     There is no protocol or protocol version supported by both the client and the
                //     server.
                public const int NS_E_PROTOCOL_MISMATCH = -1072889838;

                //
                // Zusammenfassung:
                //     The server, a computer set up to offer multimedia content to other computers,
                //     could not handle your request for multimedia content in a timely manner. Please
                //     try again later.
                public const int NS_E_TIMEOUT = -1072889837;

                //
                // Zusammenfassung:
                //     Error writing to the network.
                public const int NS_E_NET_WRITE = -1072889836;

                //
                // Zusammenfassung:
                //     Error reading from the network.
                public const int NS_E_NET_READ = -1072889835;

                //
                // Zusammenfassung:
                //     Error writing to a disk.
                public const int NS_E_DISK_WRITE = -1072889834;

                //
                // Zusammenfassung:
                //     Error reading from a disk.
                public const int NS_E_DISK_READ = -1072889833;

                //
                // Zusammenfassung:
                //     Error writing to a file.
                public const int NS_E_FILE_WRITE = -1072889832;

                //
                // Zusammenfassung:
                //     Error reading from a file.
                public const int NS_E_FILE_READ = -1072889831;

                //
                // Zusammenfassung:
                //     The system cannot find the file specified.
                public const int NS_E_FILE_NOT_FOUND = -1072889830;

                //
                // Zusammenfassung:
                //     The file already exists.
                public const int NS_E_FILE_EXISTS = -1072889829;

                //
                // Zusammenfassung:
                //     The file name, directory name, or volume label syntax is incorrect.
                public const int NS_E_INVALID_NAME = -1072889828;

                //
                // Zusammenfassung:
                //     Failed to open a file.
                public const int NS_E_FILE_OPEN_FAILED = -1072889827;

                //
                // Zusammenfassung:
                //     Unable to allocate a file.
                public const int NS_E_FILE_ALLOCATION_FAILED = -1072889826;

                //
                // Zusammenfassung:
                //     Unable to initialize a file.
                public const int NS_E_FILE_INIT_FAILED = -1072889825;

                //
                // Zusammenfassung:
                //     Unable to play a file.
                public const int NS_E_FILE_PLAY_FAILED = -1072889824;

                //
                // Zusammenfassung:
                //     Could not set the disk UID.
                public const int NS_E_SET_DISK_UID_FAILED = -1072889823;

                //
                // Zusammenfassung:
                //     An error was induced for testing purposes.
                public const int NS_E_INDUCED = -1072889822;

                //
                // Zusammenfassung:
                //     Two Content Servers failed to communicate.
                public const int NS_E_CCLINK_DOWN = -1072889821;

                //
                // Zusammenfassung:
                //     An unknown error occurred.
                public const int NS_E_INTERNAL = -1072889820;

                //
                // Zusammenfassung:
                //     The requested resource is in use.
                public const int NS_E_BUSY = -1072889819;

                //
                // Zusammenfassung:
                //     The specified protocol is not recognized. Be sure that the file name and syntax,
                //     such as slashes, are correct for the protocol.
                public const int NS_E_UNRECOGNIZED_STREAM_TYPE = -1072889818;

                //
                // Zusammenfassung:
                //     The network service provider failed.
                public const int NS_E_NETWORK_SERVICE_FAILURE = -1072889817;

                //
                // Zusammenfassung:
                //     An attempt to acquire a network resource failed.
                public const int NS_E_NETWORK_RESOURCE_FAILURE = -1072889816;

                //
                // Zusammenfassung:
                //     The network connection has failed.
                public const int NS_E_CONNECTION_FAILURE = -1072889815;

                //
                // Zusammenfassung:
                //     The session is being terminated locally.
                public const int NS_E_SHUTDOWN = -1072889814;

                //
                // Zusammenfassung:
                //     The request is invalid in the current state.
                public const int NS_E_INVALID_REQUEST = -1072889813;

                //
                // Zusammenfassung:
                //     There is insufficient bandwidth available to fulfill the request.
                public const int NS_E_INSUFFICIENT_BANDWIDTH = -1072889812;

                //
                // Zusammenfassung:
                //     The disk is not rebuilding.
                public const int NS_E_NOT_REBUILDING = -1072889811;

                //
                // Zusammenfassung:
                //     An operation requested for a particular time could not be carried out on schedule.
                public const int NS_E_LATE_OPERATION = -1072889810;

                //
                // Zusammenfassung:
                //     Invalid or corrupt data was encountered.
                public const int NS_E_INVALID_DATA = -1072889809;

                //
                // Zusammenfassung:
                //     The bandwidth required to stream a file is higher than the maximum file bandwidth
                //     allowed on the server.
                public const int NS_E_FILE_BANDWIDTH_LIMIT = -1072889808;

                //
                // Zusammenfassung:
                //     The client cannot have any more files open simultaneously.
                public const int NS_E_OPEN_FILE_LIMIT = -1072889807;

                //
                // Zusammenfassung:
                //     The server received invalid data from the client on the control connection.
                public const int NS_E_BAD_CONTROL_DATA = -1072889806;

                //
                // Zusammenfassung:
                //     There is no stream available.
                public const int NS_E_NO_STREAM = -1072889805;

                //
                // Zusammenfassung:
                //     There is no more data in the stream.
                public const int NS_E_STREAM_END = -1072889804;

                //
                // Zusammenfassung:
                //     The specified server could not be found.
                public const int NS_E_SERVER_NOT_FOUND = -1072889803;

                //
                // Zusammenfassung:
                //     The specified name is already in use.
                public const int NS_E_DUPLICATE_NAME = -1072889802;

                //
                // Zusammenfassung:
                //     The specified address is already in use.
                public const int NS_E_DUPLICATE_ADDRESS = -1072889801;

                //
                // Zusammenfassung:
                //     The specified address is not a valid multicast address.
                public const int NS_E_BAD_MULTICAST_ADDRESS = -1072889800;

                //
                // Zusammenfassung:
                //     The specified adapter address is invalid.
                public const int NS_E_BAD_ADAPTER_ADDRESS = -1072889799;

                //
                // Zusammenfassung:
                //     The specified delivery mode is invalid.
                public const int NS_E_BAD_DELIVERY_MODE = -1072889798;

                //
                // Zusammenfassung:
                //     The specified station does not exist.
                public const int NS_E_INVALID_CHANNEL = -1072889797;

                //
                // Zusammenfassung:
                //     The specified stream does not exist.
                public const int NS_E_INVALID_STREAM = -1072889796;

                //
                // Zusammenfassung:
                //     The specified archive could not be opened.
                public const int NS_E_INVALID_ARCHIVE = -1072889795;

                //
                // Zusammenfassung:
                //     The system cannot find any titles on the server.
                public const int NS_E_NOTITLES = -1072889794;

                //
                // Zusammenfassung:
                //     The system cannot find the client specified.
                public const int NS_E_INVALID_CLIENT = -1072889793;

                //
                // Zusammenfassung:
                //     The Blackhole Address is not initialized.
                public const int NS_E_INVALID_BLACKHOLE_ADDRESS = -1072889792;

                //
                // Zusammenfassung:
                //     The station does not support the stream format.
                public const int NS_E_INCOMPATIBLE_FORMAT = -1072889791;

                //
                // Zusammenfassung:
                //     The specified key is not valid.
                public const int NS_E_INVALID_KEY = -1072889790;

                //
                // Zusammenfassung:
                //     The specified port is not valid.
                public const int NS_E_INVALID_PORT = -1072889789;

                //
                // Zusammenfassung:
                //     The specified TTL is not valid.
                public const int NS_E_INVALID_TTL = -1072889788;

                //
                // Zusammenfassung:
                //     The request to fast forward or rewind could not be fulfilled.
                public const int NS_E_STRIDE_REFUSED = -1072889787;

                //
                // Zusammenfassung:
                //     Unable to load the appropriate file parser.
                public const int NS_E_MMSAUTOSERVER_CANTFINDWALKER = -1072889786;

                //
                // Zusammenfassung:
                //     Cannot exceed the maximum bandwidth limit.
                public const int NS_E_MAX_BITRATE = -1072889785;

                //
                // Zusammenfassung:
                //     Invalid value for LogFilePeriod.
                public const int NS_E_LOGFILEPERIOD = -1072889784;

                //
                // Zusammenfassung:
                //     Cannot exceed the maximum client limit.
                public const int NS_E_MAX_CLIENTS = -1072889783;

                //
                // Zusammenfassung:
                //     The maximum log file size has been reached.
                public const int NS_E_LOG_FILE_SIZE = -1072889782;

                //
                // Zusammenfassung:
                //     Cannot exceed the maximum file rate.
                public const int NS_E_MAX_FILERATE = -1072889781;

                //
                // Zusammenfassung:
                //     Unknown file type.
                public const int NS_E_WALKER_UNKNOWN = -1072889780;

                //
                // Zusammenfassung:
                //     The specified file, %1, cannot be loaded onto the specified server, %2.
                public const int NS_E_WALKER_SERVER = -1072889779;

                //
                // Zusammenfassung:
                //     There was a usage error with file parser.
                public const int NS_E_WALKER_USAGE = -1072889778;

                //
                // Zusammenfassung:
                //     The Title Server %1 has failed.
                public const int NS_E_TIGER_FAIL = -1072889776;

                //
                // Zusammenfassung:
                //     Content Server %1 (%2) has failed.
                public const int NS_E_CUB_FAIL = -1072889773;

                //
                // Zusammenfassung:
                //     Disk %1 ( %2 ) on Content Server %3, has failed.
                public const int NS_E_DISK_FAIL = -1072889771;

                //
                // Zusammenfassung:
                //     The NetShow data stream limit of %1 streams was reached.
                public const int NS_E_MAX_FUNNELS_ALERT = -1072889760;

                //
                // Zusammenfassung:
                //     The NetShow Video Server was unable to allocate a %1 block file named %2.
                public const int NS_E_ALLOCATE_FILE_FAIL = -1072889759;

                //
                // Zusammenfassung:
                //     A Content Server was unable to page a block.
                public const int NS_E_PAGING_ERROR = -1072889758;

                //
                // Zusammenfassung:
                //     Disk %1 has unrecognized control block version %2.
                public const int NS_E_BAD_BLOCK0_VERSION = -1072889757;

                //
                // Zusammenfassung:
                //     Disk %1 has incorrect uid %2.
                public const int NS_E_BAD_DISK_UID = -1072889756;

                //
                // Zusammenfassung:
                //     Disk %1 has unsupported file system major version %2.
                public const int NS_E_BAD_FSMAJOR_VERSION = -1072889755;

                //
                // Zusammenfassung:
                //     Disk %1 has bad stamp number in control block.
                public const int NS_E_BAD_STAMPNUMBER = -1072889754;

                //
                // Zusammenfassung:
                //     Disk %1 is partially reconstructed.
                public const int NS_E_PARTIALLY_REBUILT_DISK = -1072889753;

                //
                // Zusammenfassung:
                //     EnactPlan gives up.
                public const int NS_E_ENACTPLAN_GIVEUP = -1072889752;

                //
                // Zusammenfassung:
                //     The key was not found in the registry.
                public const int MCMADM_E_REGKEY_NOT_FOUND = -1072889750;

                //
                // Zusammenfassung:
                //     The publishing point cannot be started because the server does not have the appropriate
                //     stream formats. Use the Multicast Announcement Wizard to create a new announcement
                //     for this publishing point.
                public const int NS_E_NO_FORMATS = -1072889749;

                //
                // Zusammenfassung:
                //     No reference URLs were found in an ASX file.
                public const int NS_E_NO_REFERENCES = -1072889748;

                //
                // Zusammenfassung:
                //     Error opening wave device, the device might be in use.
                public const int NS_E_WAVE_OPEN = -1072889747;

                //
                // Zusammenfassung:
                //     Unable to establish a connection to the NetShow event monitor service.
                public const int NS_E_CANNOTCONNECTEVENTS = -1072889745;

                //
                // Zusammenfassung:
                //     No device driver is present on the system.
                public const int NS_E_NO_DEVICE = -1072889743;

                //
                // Zusammenfassung:
                //     No specified device driver is present.
                public const int NS_E_NO_SPECIFIED_DEVICE = -1072889742;

                //
                // Zusammenfassung:
                //     Netshow Events Monitor is not operational and has been disconnected.
                public const int NS_E_MONITOR_GIVEUP = -1072889656;

                //
                // Zusammenfassung:
                //     Disk %1 is remirrored.
                public const int NS_E_REMIRRORED_DISK = -1072889655;

                //
                // Zusammenfassung:
                //     Insufficient data found.
                public const int NS_E_INSUFFICIENT_DATA = -1072889654;

                //
                // Zusammenfassung:
                //     1 failed in file %2 line %3.
                public const int NS_E_ASSERT = -1072889653;

                //
                // Zusammenfassung:
                //     The specified adapter name is invalid.
                public const int NS_E_BAD_ADAPTER_NAME = -1072889652;

                //
                // Zusammenfassung:
                //     The application is not licensed for this feature.
                public const int NS_E_NOT_LICENSED = -1072889651;

                //
                // Zusammenfassung:
                //     Unable to contact the server.
                public const int NS_E_NO_SERVER_CONTACT = -1072889650;

                //
                // Zusammenfassung:
                //     Maximum number of titles exceeded.
                public const int NS_E_TOO_MANY_TITLES = -1072889649;

                //
                // Zusammenfassung:
                //     Maximum size of a title exceeded.
                public const int NS_E_TITLE_SIZE_EXCEEDED = -1072889648;

                //
                // Zusammenfassung:
                //     UDP protocol not enabled. Not trying %1!ls!.
                public const int NS_E_UDP_DISABLED = -1072889647;

                //
                // Zusammenfassung:
                //     TCP protocol not enabled. Not trying %1!ls!.
                public const int NS_E_TCP_DISABLED = -1072889646;

                //
                // Zusammenfassung:
                //     HTTP protocol not enabled. Not trying %1!ls!.
                public const int NS_E_HTTP_DISABLED = -1072889645;

                //
                // Zusammenfassung:
                //     The product license has expired.
                public const int NS_E_LICENSE_EXPIRED = -1072889644;

                //
                // Zusammenfassung:
                //     Source file exceeds the per title maximum bitrate. See NetShow Theater documentation
                //     for more information.
                public const int NS_E_TITLE_BITRATE = -1072889643;

                //
                // Zusammenfassung:
                //     The program name cannot be empty.
                public const int NS_E_EMPTY_PROGRAM_NAME = -1072889642;

                //
                // Zusammenfassung:
                //     Station %1 does not exist.
                public const int NS_E_MISSING_CHANNEL = -1072889641;

                //
                // Zusammenfassung:
                //     You need to define at least one station before this operation can complete.
                public const int NS_E_NO_CHANNELS = -1072889640;

                //
                // Zusammenfassung:
                //     The index specified is invalid.
                public const int NS_E_INVALID_INDEX2 = -1072889639;

                //
                // Zusammenfassung:
                //     Content Server %1 (%2) has failed its link to Content Server %3.
                public const int NS_E_CUB_FAIL_LINK = -1072889456;

                //
                // Zusammenfassung:
                //     Content Server %1 (%2) has incorrect uid %3.
                public const int NS_E_BAD_CUB_UID = -1072889454;

                //
                // Zusammenfassung:
                //     Server unreliable because multiple components failed.
                public const int NS_E_GLITCH_MODE = -1072889451;

                //
                // Zusammenfassung:
                //     Content Server %1 (%2) is unable to communicate with the Media System Network
                //     Protocol.
                public const int NS_E_NO_MEDIA_PROTOCOL = -1072889445;

                //
                // Zusammenfassung:
                //     Nothing to do.
                public const int NS_E_NOTHING_TO_DO = -1072887823;

                //
                // Zusammenfassung:
                //     Not receiving data from the server.
                public const int NS_E_NO_MULTICAST = -1072887822;

                //
                // Zusammenfassung:
                //     The input media format is invalid.
                public const int NS_E_INVALID_INPUT_FORMAT = -1072886856;

                //
                // Zusammenfassung:
                //     The MSAudio codec is not installed on this system.
                public const int NS_E_MSAUDIO_NOT_INSTALLED = -1072886855;

                //
                // Zusammenfassung:
                //     An unexpected error occurred with the MSAudio codec.
                public const int NS_E_UNEXPECTED_MSAUDIO_ERROR = -1072886854;

                //
                // Zusammenfassung:
                //     The output media format is invalid.
                public const int NS_E_INVALID_OUTPUT_FORMAT = -1072886853;

                //
                // Zusammenfassung:
                //     The object must be fully configured before audio samples can be processed.
                public const int NS_E_NOT_CONFIGURED = -1072886852;

                //
                // Zusammenfassung:
                //     You need a license to perform the requested operation on this media file.
                public const int NS_E_PROTECTED_CONTENT = -1072886851;

                //
                // Zusammenfassung:
                //     You need a license to perform the requested operation on this media file.
                public const int NS_E_LICENSE_REQUIRED = -1072886850;

                //
                // Zusammenfassung:
                //     This media file is corrupted or invalid. Contact the content provider for a new
                //     file.
                public const int NS_E_TAMPERED_CONTENT = -1072886849;

                //
                // Zusammenfassung:
                //     The license for this media file has expired. Get a new license or contact the
                //     content provider for further assistance.
                public const int NS_E_LICENSE_OUTOFDATE = -1072886848;

                //
                // Zusammenfassung:
                //     You are not allowed to open this file. Contact the content provider for further
                //     assistance.
                public const int NS_E_LICENSE_INCORRECT_RIGHTS = -1072886847;

                //
                // Zusammenfassung:
                //     The requested audio codec is not installed on this system.
                public const int NS_E_AUDIO_CODEC_NOT_INSTALLED = -1072886846;

                //
                // Zusammenfassung:
                //     An unexpected error occurred with the audio codec.
                public const int NS_E_AUDIO_CODEC_ERROR = -1072886845;

                //
                // Zusammenfassung:
                //     The requested video codec is not installed on this system.
                public const int NS_E_VIDEO_CODEC_NOT_INSTALLED = -1072886844;

                //
                // Zusammenfassung:
                //     An unexpected error occurred with the video codec.
                public const int NS_E_VIDEO_CODEC_ERROR = -1072886843;

                //
                // Zusammenfassung:
                //     The Profile is invalid.
                public const int NS_E_INVALIDPROFILE = -1072886842;

                //
                // Zusammenfassung:
                //     A new version of the SDK is needed to play the requested content.
                public const int NS_E_INCOMPATIBLE_VERSION = -1072886841;

                //
                // Zusammenfassung:
                //     The requested URL is not available in offline mode.
                public const int NS_E_OFFLINE_MODE = -1072886838;

                //
                // Zusammenfassung:
                //     The requested URL cannot be accessed because there is no network connection.
                public const int NS_E_NOT_CONNECTED = -1072886837;

                //
                // Zusammenfassung:
                //     The encoding process was unable to keep up with the amount of supplied data.
                public const int NS_E_TOO_MUCH_DATA = -1072886836;

                //
                // Zusammenfassung:
                //     The given property is not supported.
                public const int NS_E_UNSUPPORTED_PROPERTY = -1072886835;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot copy the files to the CD because they are 8-bit.
                //     Convert the files to 16-bit, 44-kHz stereo files by using Sound Recorder or another
                //     audio-processing program, and then try again.
                public const int NS_E_8BIT_WAVE_UNSUPPORTED = -1072886834;

                //
                // Zusammenfassung:
                //     There are no more samples in the current range.
                public const int NS_E_NO_MORE_SAMPLES = -1072886833;

                //
                // Zusammenfassung:
                //     The given sampling rate is invalid.
                public const int NS_E_INVALID_SAMPLING_RATE = -1072886832;

                //
                // Zusammenfassung:
                //     The given maximum packet size is too small to accommodate this profile.)
                public const int NS_E_MAX_PACKET_SIZE_TOO_SMALL = -1072886831;

                //
                // Zusammenfassung:
                //     The packet arrived too late to be of use.
                public const int NS_E_LATE_PACKET = -1072886830;

                //
                // Zusammenfassung:
                //     The packet is a duplicate of one received before.
                public const int NS_E_DUPLICATE_PACKET = -1072886829;

                //
                // Zusammenfassung:
                //     Supplied buffer is too small.
                public const int NS_E_SDK_BUFFERTOOSMALL = -1072886828;

                //
                // Zusammenfassung:
                //     The wrong number of preprocessing passes was used for the stream's output type.
                public const int NS_E_INVALID_NUM_PASSES = -1072886827;

                //
                // Zusammenfassung:
                //     An attempt was made to add, modify, or delete a read only attribute.
                public const int NS_E_ATTRIBUTE_READ_ONLY = -1072886826;

                //
                // Zusammenfassung:
                //     An attempt was made to add attribute that is not allowed for the given media
                //     type.
                public const int NS_E_ATTRIBUTE_NOT_ALLOWED = -1072886825;

                //
                // Zusammenfassung:
                //     The EDL provided is invalid.
                public const int NS_E_INVALID_EDL = -1072886824;

                //
                // Zusammenfassung:
                //     The Data Unit Extension data was too large to be used.
                public const int NS_E_DATA_UNIT_EXTENSION_TOO_LARGE = -1072886823;

                //
                // Zusammenfassung:
                //     An unexpected error occurred with a DMO codec.
                public const int NS_E_CODEC_DMO_ERROR = -1072886822;

                //
                // Zusammenfassung:
                //     This feature has been disabled by group policy.
                public const int NS_E_FEATURE_DISABLED_BY_GROUP_POLICY = -1072886820;

                //
                // Zusammenfassung:
                //     This feature is disabled in this SKU.
                public const int NS_E_FEATURE_DISABLED_IN_SKU = -1072886819;

                //
                // Zusammenfassung:
                //     There is no CD in the CD drive. Insert a CD, and then try again.
                public const int NS_E_NO_CD = -1072885856;

                //
                // Zusammenfassung:
                //     Windows Media Player could not use digital playback to play the CD. To switch
                //     to analog playback, on the Tools menu, click Options, and then click the Devices
                //     tab. Double-click the CD drive, and then in the Playback area, click Analog.
                //     For additional assistance, click Web Help.
                public const int NS_E_CANT_READ_DIGITAL = -1072885855;

                //
                // Zusammenfassung:
                //     Windows Media Player no longer detects a connected portable device. Reconnect
                //     your portable device, and then try synchronizing the file again.
                public const int NS_E_DEVICE_DISCONNECTED = -1072885854;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file. The portable device does not support
                //     the specified file type.
                public const int NS_E_DEVICE_NOT_SUPPORT_FORMAT = -1072885853;

                //
                // Zusammenfassung:
                //     Windows Media Player could not use digital playback to play the CD. The Player
                //     has automatically switched the CD drive to analog playback. To switch back to
                //     digital CD playback, use the Devices tab. For additional assistance, click Web
                //     Help.
                public const int NS_E_SLOW_READ_DIGITAL = -1072885852;

                //
                // Zusammenfassung:
                //     An invalid line error occurred in the mixer.
                public const int NS_E_MIXER_INVALID_LINE = -1072885851;

                //
                // Zusammenfassung:
                //     An invalid control error occurred in the mixer.
                public const int NS_E_MIXER_INVALID_CONTROL = -1072885850;

                //
                // Zusammenfassung:
                //     An invalid value error occurred in the mixer.
                public const int NS_E_MIXER_INVALID_VALUE = -1072885849;

                //
                // Zusammenfassung:
                //     An unrecognized MMRESULT occurred in the mixer.
                public const int NS_E_MIXER_UNKNOWN_MMRESULT = -1072885848;

                //
                // Zusammenfassung:
                //     User has stopped the operation.
                public const int NS_E_USER_STOP = -1072885847;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot rip the track because a compatible MP3 encoder is
                //     not installed on your computer. Install a compatible MP3 encoder or choose a
                //     different format to rip to (such as Windows Media Audio).
                public const int NS_E_MP3_FORMAT_NOT_FOUND = -1072885846;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot read the CD. The disc might be dirty or damaged.
                //     Turn on error correction, and then try again.
                public const int NS_E_CD_READ_ERROR_NO_CORRECTION = -1072885845;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot read the CD. The disc might be dirty or damaged or
                //     the CD drive might be malfunctioning.
                public const int NS_E_CD_READ_ERROR = -1072885844;

                //
                // Zusammenfassung:
                //     For best performance, do not play CD tracks while ripping them.
                public const int NS_E_CD_SLOW_COPY = -1072885843;

                //
                // Zusammenfassung:
                //     It is not possible to directly burn tracks from one CD to another CD. You must
                //     first rip the tracks from the CD to your computer, and then burn the files to
                //     a blank CD.
                public const int NS_E_CD_COPYTO_CD = -1072885842;

                //
                // Zusammenfassung:
                //     Could not open a sound mixer driver.
                public const int NS_E_MIXER_NODRIVER = -1072885841;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot rip tracks from the CD correctly because the CD drive
                //     settings in Device Manager do not match the CD drive settings in the Player.
                public const int NS_E_REDBOOK_ENABLED_WHILE_COPYING = -1072885840;

                //
                // Zusammenfassung:
                //     Windows Media Player is busy reading the CD.
                public const int NS_E_CD_REFRESH = -1072885839;

                //
                // Zusammenfassung:
                //     Windows Media Player could not use digital playback to play the CD. The Player
                //     has automatically switched the CD drive to analog playback. To switch back to
                //     digital CD playback, use the Devices tab. For additional assistance, click Web
                //     Help.
                public const int NS_E_CD_DRIVER_PROBLEM = -1072885838;

                //
                // Zusammenfassung:
                //     Windows Media Player could not use digital playback to play the CD. The Player
                //     has automatically switched the CD drive to analog playback. To switch back to
                //     digital CD playback, use the Devices tab. For additional assistance, click Web
                //     Help.
                public const int NS_E_WONT_DO_DIGITAL = -1072885837;

                //
                // Zusammenfassung:
                //     A call was made to GetParseError on the XML parser but there was no error to
                //     retrieve.
                public const int NS_E_WMPXML_NOERROR = -1072885836;

                //
                // Zusammenfassung:
                //     The XML Parser ran out of data while parsing.
                public const int NS_E_WMPXML_ENDOFDATA = -1072885835;

                //
                // Zusammenfassung:
                //     A generic parse error occurred in the XML parser but no information is available.
                public const int NS_E_WMPXML_PARSEERROR = -1072885834;

                //
                // Zusammenfassung:
                //     A call get GetNamedAttribute or GetNamedAttributeIndex on the XML parser resulted
                //     in the index not being found.
                public const int NS_E_WMPXML_ATTRIBUTENOTFOUND = -1072885833;

                //
                // Zusammenfassung:
                //     A call was made go GetNamedPI on the XML parser, but the requested Processing
                //     Instruction was not found.
                public const int NS_E_WMPXML_PINOTFOUND = -1072885832;

                //
                // Zusammenfassung:
                //     Persist was called on the XML parser, but the parser has no data to persist.
                public const int NS_E_WMPXML_EMPTYDOC = -1072885831;

                //
                // Zusammenfassung:
                //     This file path is already in the library.
                public const int NS_E_WMP_PATH_ALREADY_IN_LIBRARY = -1072885830;

                //
                // Zusammenfassung:
                //     Windows Media Player is already searching for files to add to your library. Wait
                //     for the current process to finish before attempting to search again.
                public const int NS_E_WMP_FILESCANALREADYSTARTED = -1072885826;

                //
                // Zusammenfassung:
                //     Windows Media Player is unable to find the media you are looking for.
                public const int NS_E_WMP_HME_INVALIDOBJECTID = -1072885825;

                //
                // Zusammenfassung:
                //     A component of Windows Media Player is out-of-date. If you are running a pre-release
                //     version of Windows, try upgrading to a more recent version.
                public const int NS_E_WMP_MF_CODE_EXPIRED = -1072885824;

                //
                // Zusammenfassung:
                //     This container does not support search on items.
                public const int NS_E_WMP_HME_NOTSEARCHABLEFORITEMS = -1072885823;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered a problem while adding one or more files to
                //     the library. For additional assistance, click Web Help.
                public const int NS_E_WMP_ADDTOLIBRARY_FAILED = -1072885817;

                //
                // Zusammenfassung:
                //     A Windows API call failed but no error information was available.
                public const int NS_E_WMP_WINDOWSAPIFAILURE = -1072885816;

                //
                // Zusammenfassung:
                //     This file does not have burn rights. If you obtained this file from an online
                //     store, go to the online store to get burn rights.
                public const int NS_E_WMP_RECORDING_NOT_ALLOWED = -1072885815;

                //
                // Zusammenfassung:
                //     Windows Media Player no longer detects a connected portable device. Reconnect
                //     your portable device, and then try to sync the file again.
                public const int NS_E_DEVICE_NOT_READY = -1072885814;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because it is corrupted.
                public const int NS_E_DAMAGED_FILE = -1072885813;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered an error while attempting to access information
                //     in the library. Try restarting the Player.
                public const int NS_E_MPDB_GENERIC = -1072885812;

                //
                // Zusammenfassung:
                //     The file cannot be added to the library because it is smaller than the "Skip
                //     files smaller than" setting. To add the file, change the setting on the Library
                //     tab. For additional assistance, click Web Help.
                public const int NS_E_FILE_FAILED_CHECKS = -1072885811;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot create the library. You must be logged on as an administrator
                //     or a member of the Administrators group to install the Player. For more information,
                //     contact your system administrator.
                public const int NS_E_MEDIA_LIBRARY_FAILED = -1072885810;

                //
                // Zusammenfassung:
                //     The file is already in use. Close other programs that might be using the file,
                //     or stop playing the file, and then try again.
                public const int NS_E_SHARING_VIOLATION = -1072885809;

                //
                // Zusammenfassung:
                //     Windows Media Player has encountered an unknown error.
                public const int NS_E_NO_ERROR_STRING_FOUND = -1072885808;

                //
                // Zusammenfassung:
                //     The Windows Media Player ActiveX control cannot connect to remote media services,
                //     but will continue with local media services.
                public const int NS_E_WMPOCX_NO_REMOTE_CORE = -1072885807;

                //
                // Zusammenfassung:
                //     The requested method or property is not available because the Windows Media Player
                //     ActiveX control has not been properly activated.
                public const int NS_E_WMPOCX_NO_ACTIVE_CORE = -1072885806;

                //
                // Zusammenfassung:
                //     The Windows Media Player ActiveX control is not running in remote mode.
                public const int NS_E_WMPOCX_NOT_RUNNING_REMOTELY = -1072885805;

                //
                // Zusammenfassung:
                //     An error occurred while trying to get the remote Windows Media Player window.
                public const int NS_E_WMPOCX_NO_REMOTE_WINDOW = -1072885804;

                //
                // Zusammenfassung:
                //     Windows Media Player has encountered an unknown error.
                public const int NS_E_WMPOCX_ERRORMANAGERNOTAVAILABLE = -1072885803;

                //
                // Zusammenfassung:
                //     Windows Media Player was not closed properly. A damaged or incompatible plug-in
                //     might have caused the problem to occur. As a precaution, all optional plug-ins
                //     have been disabled.
                public const int NS_E_PLUGIN_NOTSHUTDOWN = -1072885802;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot find the specified path. Verify that the path is
                //     typed correctly. If it is, the path does not exist in the specified location,
                //     or the computer where the path is located is not available.
                public const int NS_E_WMP_CANNOT_FIND_FOLDER = -1072885801;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot save a file that is being streamed.
                public const int NS_E_WMP_STREAMING_RECORDING_NOT_ALLOWED = -1072885800;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot find the selected plug-in. The Player will try to
                //     remove it from the menu. To use this plug-in, install it again.
                public const int NS_E_WMP_PLUGINDLL_NOTFOUND = -1072885799;

                //
                // Zusammenfassung:
                //     Action requires input from the user.
                public const int NS_E_NEED_TO_ASK_USER = -1072885798;

                //
                // Zusammenfassung:
                //     The Windows Media Player ActiveX control must be in a docked state for this action
                //     to be performed.
                public const int NS_E_WMPOCX_PLAYER_NOT_DOCKED = -1072885797;

                //
                // Zusammenfassung:
                //     The Windows Media Player external object is not ready.
                public const int NS_E_WMP_EXTERNAL_NOTREADY = -1072885796;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot perform the requested action. Your computer's time
                //     and date might not be set correctly.
                public const int NS_E_WMP_MLS_STALE_DATA = -1072885795;

                //
                // Zusammenfassung:
                //     The control (%s) does not support creation of sub-controls, yet (%d) sub-controls
                //     have been specified.
                public const int NS_E_WMP_UI_SUBCONTROLSNOTSUPPORTED = -1072885794;

                //
                // Zusammenfassung:
                //     Version mismatch: (%.1f required, %.1f found).
                public const int NS_E_WMP_UI_VERSIONMISMATCH = -1072885793;

                //
                // Zusammenfassung:
                //     The layout manager was given valid XML that wasn't a theme file.
                public const int NS_E_WMP_UI_NOTATHEMEFILE = -1072885792;

                //
                // Zusammenfassung:
                //     The %s subelement could not be found on the %s object.
                public const int NS_E_WMP_UI_SUBELEMENTNOTFOUND = -1072885791;

                //
                // Zusammenfassung:
                //     An error occurred parsing the version tag. Valid version tags are of the form:
                //     .
                public const int NS_E_WMP_UI_VERSIONPARSE = -1072885790;

                //
                // Zusammenfassung:
                //     The view specified in for the 'currentViewID' property (%s) was not found in
                //     this theme file.
                public const int NS_E_WMP_UI_VIEWIDNOTFOUND = -1072885789;

                //
                // Zusammenfassung:
                //     This error used internally for hit testing.
                public const int NS_E_WMP_UI_PASSTHROUGH = -1072885788;

                //
                // Zusammenfassung:
                //     Attributes were specified for the %s object, but the object was not available
                //     to send them to.
                public const int NS_E_WMP_UI_OBJECTNOTFOUND = -1072885787;

                //
                // Zusammenfassung:
                //     The %s event already has a handler, the second handler was ignored.
                public const int NS_E_WMP_UI_SECONDHANDLER = -1072885786;

                //
                // Zusammenfassung:
                //     No .wms file found in skin archive.
                public const int NS_E_WMP_UI_NOSKININZIP = -1072885785;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered a problem while downloading the file. For additional
                //     assistance, click Web Help.
                public const int NS_E_WMP_URLDOWNLOADFAILED = -1072885782;

                //
                // Zusammenfassung:
                //     The Windows Media Player ActiveX control cannot load the requested uiMode and
                //     cannot roll back to the existing uiMode.
                public const int NS_E_WMPOCX_UNABLE_TO_LOAD_SKIN = -1072885781;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered a problem with the skin file. The skin file
                //     might not be valid.
                public const int NS_E_WMP_INVALID_SKIN = -1072885780;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot send the link because your email program is not responding.
                //     Verify that your email program is configured properly, and then try again. For
                //     more information about email, see Windows Help.
                public const int NS_E_WMP_SENDMAILFAILED = -1072885779;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot switch to full mode because your computer administrator
                //     has locked this skin.
                public const int NS_E_WMP_LOCKEDINSKINMODE = -1072885778;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered a problem while saving the file. For additional
                //     assistance, click Web Help.
                public const int NS_E_WMP_FAILED_TO_SAVE_FILE = -1072885777;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot overwrite a read-only file. Try using a different
                //     file name.
                public const int NS_E_WMP_SAVEAS_READONLY = -1072885776;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered a problem while creating or saving the playlist.
                //     For additional assistance, click Web Help.
                public const int NS_E_WMP_FAILED_TO_SAVE_PLAYLIST = -1072885775;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot open the Windows Media Download file. The file might
                //     be damaged.
                public const int NS_E_WMP_FAILED_TO_OPEN_WMD = -1072885774;

                //
                // Zusammenfassung:
                //     The file cannot be added to the library because it is a protected DVR-MS file.
                //     This content cannot be played back by Windows Media Player.
                public const int NS_E_WMP_CANT_PLAY_PROTECTED = -1072885773;

                //
                // Zusammenfassung:
                //     Media sharing has been turned off because a required Windows setting or component
                //     has changed. For additional assistance, click Web Help.
                public const int NS_E_SHARING_STATE_OUT_OF_SYNC = -1072885772;

                //
                // Zusammenfassung:
                //     Exclusive Services launch failed because the Windows Media Player is already
                //     running.
                public const int NS_E_WMPOCX_REMOTE_PLAYER_ALREADY_RUNNING = -1072885766;

                //
                // Zusammenfassung:
                //     JPG Images are not recommended for use as a mappingImage.
                public const int NS_E_WMP_RBC_JPGMAPPINGIMAGE = -1072885756;

                //
                // Zusammenfassung:
                //     JPG Images are not recommended when using a transparencyColor.
                public const int NS_E_WMP_JPGTRANSPARENCY = -1072885755;

                //
                // Zusammenfassung:
                //     The Max property cannot be less than Min property.
                public const int NS_E_WMP_INVALID_MAX_VAL = -1072885751;

                //
                // Zusammenfassung:
                //     The Min property cannot be greater than Max property.
                public const int NS_E_WMP_INVALID_MIN_VAL = -1072885750;

                //
                // Zusammenfassung:
                //     JPG Images are not recommended for use as a positionImage.
                public const int NS_E_WMP_CS_JPGPOSITIONIMAGE = -1072885746;

                //
                // Zusammenfassung:
                //     The (%s) image's size is not evenly divisible by the positionImage's size.
                public const int NS_E_WMP_CS_NOTEVENLYDIVISIBLE = -1072885745;

                //
                // Zusammenfassung:
                //     The ZIP reader opened a file and its signature did not match that of the ZIP
                //     files.
                public const int NS_E_WMPZIP_NOTAZIPFILE = -1072885736;

                //
                // Zusammenfassung:
                //     The ZIP reader has detected that the file is corrupted.
                public const int NS_E_WMPZIP_CORRUPT = -1072885735;

                //
                // Zusammenfassung:
                //     GetFileStream, SaveToFile, or SaveTemp file was called on the ZIP reader with
                //     a file name that was not found in the ZIP file.
                public const int NS_E_WMPZIP_FILENOTFOUND = -1072885734;

                //
                // Zusammenfassung:
                //     Image type not supported.
                public const int NS_E_WMP_IMAGE_FILETYPE_UNSUPPORTED = -1072885726;

                //
                // Zusammenfassung:
                //     Image file might be corrupt.
                public const int NS_E_WMP_IMAGE_INVALID_FORMAT = -1072885725;

                //
                // Zusammenfassung:
                //     Unexpected end of file. GIF file might be corrupt.
                public const int NS_E_WMP_GIF_UNEXPECTED_ENDOFFILE = -1072885724;

                //
                // Zusammenfassung:
                //     Invalid GIF file.
                public const int NS_E_WMP_GIF_INVALID_FORMAT = -1072885723;

                //
                // Zusammenfassung:
                //     Invalid GIF version. Only 87a or 89a supported.
                public const int NS_E_WMP_GIF_BAD_VERSION_NUMBER = -1072885722;

                //
                // Zusammenfassung:
                //     No images found in GIF file.
                public const int NS_E_WMP_GIF_NO_IMAGE_IN_FILE = -1072885721;

                //
                // Zusammenfassung:
                //     Invalid PNG image file format.
                public const int NS_E_WMP_PNG_INVALIDFORMAT = -1072885720;

                //
                // Zusammenfassung:
                //     PNG bitdepth not supported.
                public const int NS_E_WMP_PNG_UNSUPPORTED_BITDEPTH = -1072885719;

                //
                // Zusammenfassung:
                //     Compression format defined in PNG file not supported,
                public const int NS_E_WMP_PNG_UNSUPPORTED_COMPRESSION = -1072885718;

                //
                // Zusammenfassung:
                //     Filter method defined in PNG file not supported.
                public const int NS_E_WMP_PNG_UNSUPPORTED_FILTER = -1072885717;

                //
                // Zusammenfassung:
                //     Interlace method defined in PNG file not supported.
                public const int NS_E_WMP_PNG_UNSUPPORTED_INTERLACE = -1072885716;

                //
                // Zusammenfassung:
                //     Bad CRC in PNG file.
                public const int NS_E_WMP_PNG_UNSUPPORTED_BAD_CRC = -1072885715;

                //
                // Zusammenfassung:
                //     Invalid bitmask in BMP file.
                public const int NS_E_WMP_BMP_INVALID_BITMASK = -1072885714;

                //
                // Zusammenfassung:
                //     Topdown DIB not supported.
                public const int NS_E_WMP_BMP_TOPDOWN_DIB_UNSUPPORTED = -1072885713;

                //
                // Zusammenfassung:
                //     Bitmap could not be created.
                public const int NS_E_WMP_BMP_BITMAP_NOT_CREATED = -1072885712;

                //
                // Zusammenfassung:
                //     Compression format defined in BMP not supported.
                public const int NS_E_WMP_BMP_COMPRESSION_UNSUPPORTED = -1072885711;

                //
                // Zusammenfassung:
                //     Invalid Bitmap format.
                public const int NS_E_WMP_BMP_INVALID_FORMAT = -1072885710;

                //
                // Zusammenfassung:
                //     JPEG Arithmetic coding not supported.
                public const int NS_E_WMP_JPG_JERR_ARITHCODING_NOTIMPL = -1072885709;

                //
                // Zusammenfassung:
                //     Invalid JPEG format.
                public const int NS_E_WMP_JPG_INVALID_FORMAT = -1072885708;

                //
                // Zusammenfassung:
                //     Invalid JPEG format.
                public const int NS_E_WMP_JPG_BAD_DCTSIZE = -1072885707;

                //
                // Zusammenfassung:
                //     Internal version error. Unexpected JPEG library version.
                public const int NS_E_WMP_JPG_BAD_VERSION_NUMBER = -1072885706;

                //
                // Zusammenfassung:
                //     Internal JPEG Library error. Unsupported JPEG data precision.
                public const int NS_E_WMP_JPG_BAD_PRECISION = -1072885705;

                //
                // Zusammenfassung:
                //     JPEG CCIR601 not supported.
                public const int NS_E_WMP_JPG_CCIR601_NOTIMPL = -1072885704;

                //
                // Zusammenfassung:
                //     No image found in JPEG file.
                public const int NS_E_WMP_JPG_NO_IMAGE_IN_FILE = -1072885703;

                //
                // Zusammenfassung:
                //     Could not read JPEG file.
                public const int NS_E_WMP_JPG_READ_ERROR = -1072885702;

                //
                // Zusammenfassung:
                //     JPEG Fractional sampling not supported.
                public const int NS_E_WMP_JPG_FRACT_SAMPLE_NOTIMPL = -1072885701;

                //
                // Zusammenfassung:
                //     JPEG image too large. Maximum image size supported is 65500 X 65500.
                public const int NS_E_WMP_JPG_IMAGE_TOO_BIG = -1072885700;

                //
                // Zusammenfassung:
                //     Unexpected end of file reached in JPEG file.
                public const int NS_E_WMP_JPG_UNEXPECTED_ENDOFFILE = -1072885699;

                //
                // Zusammenfassung:
                //     Unsupported JPEG SOF marker found.
                public const int NS_E_WMP_JPG_SOF_UNSUPPORTED = -1072885698;

                //
                // Zusammenfassung:
                //     Unknown JPEG marker found.
                public const int NS_E_WMP_JPG_UNKNOWN_MARKER = -1072885697;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot display the picture file. The player either does
                //     not support the picture type or the picture is corrupted.
                public const int NS_E_WMP_FAILED_TO_OPEN_IMAGE = -1072885692;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot compute a Digital Audio Id for the song. It is too
                //     short.
                public const int NS_E_WMP_DAI_SONGTOOSHORT = -1072885687;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file at the requested speed.
                public const int NS_E_WMG_RATEUNAVAILABLE = -1072885686;

                //
                // Zusammenfassung:
                //     The rendering or digital signal processing plug-in cannot be instantiated.
                public const int NS_E_WMG_PLUGINUNAVAILABLE = -1072885685;

                //
                // Zusammenfassung:
                //     The file cannot be queued for seamless playback.
                public const int NS_E_WMG_CANNOTQUEUE = -1072885684;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot download media usage rights for a file in the playlist.
                public const int NS_E_WMG_PREROLLLICENSEACQUISITIONNOTALLOWED = -1072885683;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered an error while trying to queue a file.
                public const int NS_E_WMG_UNEXPECTEDPREROLLSTATUS = -1072885682;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the protected file. The Player cannot verify
                //     that the connection to your video card is secure. Try installing an updated device
                //     driver for your video card.
                public const int NS_E_WMG_INVALID_COPP_CERTIFICATE = -1072885679;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the protected file. The Player detected that
                //     the connection to your hardware might not be secure.
                public const int NS_E_WMG_COPP_SECURITY_INVALID = -1072885678;

                //
                // Zusammenfassung:
                //     Windows Media Player output link protection is unsupported on this system.
                public const int NS_E_WMG_COPP_UNSUPPORTED = -1072885677;

                //
                // Zusammenfassung:
                //     Operation attempted in an invalid graph state.
                public const int NS_E_WMG_INVALIDSTATE = -1072885676;

                //
                // Zusammenfassung:
                //     A renderer cannot be inserted in a stream while one already exists.
                public const int NS_E_WMG_SINKALREADYEXISTS = -1072885675;

                //
                // Zusammenfassung:
                //     The Windows Media SDK interface needed to complete the operation does not exist
                //     at this time.
                public const int NS_E_WMG_NOSDKINTERFACE = -1072885674;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play a portion of the file because it requires a
                //     codec that either could not be downloaded or that is not supported by the Player.
                public const int NS_E_WMG_NOTALLOUTPUTSRENDERED = -1072885673;

                //
                // Zusammenfassung:
                //     File transfer streams are not allowed in the standalone Player.
                public const int NS_E_WMG_FILETRANSFERNOTALLOWED = -1072885672;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file. The Player does not support the format
                //     you are trying to play.
                public const int NS_E_WMR_UNSUPPORTEDSTREAM = -1072885671;

                //
                // Zusammenfassung:
                //     An operation was attempted on a pin that does not exist in the DirectShow filter
                //     graph.
                public const int NS_E_WMR_PINNOTFOUND = -1072885670;

                //
                // Zusammenfassung:
                //     Specified operation cannot be completed while waiting for a media format change
                //     from the SDK.
                public const int NS_E_WMR_WAITINGONFORMATSWITCH = -1072885669;

                //
                // Zusammenfassung:
                //     Specified operation cannot be completed because the source filter does not exist.
                public const int NS_E_WMR_NOSOURCEFILTER = -1072885668;

                //
                // Zusammenfassung:
                //     The specified type does not match this pin.
                public const int NS_E_WMR_PINTYPENOMATCH = -1072885667;

                //
                // Zusammenfassung:
                //     The WMR Source Filter does not have a callback available.
                public const int NS_E_WMR_NOCALLBACKAVAILABLE = -1072885666;

                //
                // Zusammenfassung:
                //     The specified property has not been set on this sample.
                public const int NS_E_WMR_SAMPLEPROPERTYNOTSET = -1072885662;

                //
                // Zusammenfassung:
                //     A plug-in is required to correctly play the file. To determine if the plug-in
                //     is available to download, click Web Help.
                public const int NS_E_WMR_CANNOT_RENDER_BINARY_STREAM = -1072885661;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because your media usage rights are
                //     corrupted. If you previously backed up your media usage rights, try restoring
                //     them.
                public const int NS_E_WMG_LICENSE_TAMPERED = -1072885660;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play protected files that contain binary streams.
                public const int NS_E_WMR_WILLNOT_RENDER_BINARY_STREAM = -1072885659;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the playlist because it is not valid.
                public const int NS_E_WMX_UNRECOGNIZED_PLAYLIST_FORMAT = -1072885656;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the playlist because it is not valid.
                public const int NS_E_ASX_INVALIDFORMAT = -1072885655;

                //
                // Zusammenfassung:
                //     A later version of Windows Media Player might be required to play this playlist.
                public const int NS_E_ASX_INVALIDVERSION = -1072885654;

                //
                // Zusammenfassung:
                //     The format of a REPEAT loop within the current playlist file is not valid.
                public const int NS_E_ASX_INVALID_REPEAT_BLOCK = -1072885653;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot save the playlist because it does not contain any
                //     items.
                public const int NS_E_ASX_NOTHING_TO_WRITE = -1072885652;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the playlist because it is not valid.
                public const int NS_E_URLLIST_INVALIDFORMAT = -1072885651;

                //
                // Zusammenfassung:
                //     The specified attribute does not exist.
                public const int NS_E_WMX_ATTRIBUTE_DOES_NOT_EXIST = -1072885650;

                //
                // Zusammenfassung:
                //     The specified attribute already exists.
                public const int NS_E_WMX_ATTRIBUTE_ALREADY_EXISTS = -1072885649;

                //
                // Zusammenfassung:
                //     Cannot retrieve the specified attribute.
                public const int NS_E_WMX_ATTRIBUTE_UNRETRIEVABLE = -1072885648;

                //
                // Zusammenfassung:
                //     The specified item does not exist in the current playlist.
                public const int NS_E_WMX_ITEM_DOES_NOT_EXIST = -1072885647;

                //
                // Zusammenfassung:
                //     Items of the specified type cannot be created within the current playlist.
                public const int NS_E_WMX_ITEM_TYPE_ILLEGAL = -1072885646;

                //
                // Zusammenfassung:
                //     The specified item cannot be set in the current playlist.
                public const int NS_E_WMX_ITEM_UNSETTABLE = -1072885645;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot perform the requested action because the playlist
                //     does not contain any items.
                public const int NS_E_WMX_PLAYLIST_EMPTY = -1072885644;

                //
                // Zusammenfassung:
                //     The specified auto playlist contains a filter type that is either not valid or
                //     is not installed on this computer.
                public const int NS_E_MLS_SMARTPLAYLIST_FILTER_NOT_REGISTERED = -1072885643;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because the associated playlist contains
                //     too many nested playlists.
                public const int NS_E_WMX_INVALID_FORMAT_OVER_NESTING = -1072885642;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot find the file. Verify that the path is typed correctly.
                //     If it is, the file might not exist in the specified location, or the computer
                //     where the file is stored might not be available.
                public const int NS_E_WMPCORE_NOSOURCEURLSTRING = -1072885636;

                //
                // Zusammenfassung:
                //     Failed to create the Global Interface Table.
                public const int NS_E_WMPCORE_COCREATEFAILEDFORGITOBJECT = -1072885635;

                //
                // Zusammenfassung:
                //     Failed to get the marshaled graph event handler interface.
                public const int NS_E_WMPCORE_FAILEDTOGETMARSHALLEDEVENTHANDLERINTERFACE = -1072885634;

                //
                // Zusammenfassung:
                //     Buffer is too small for copying media type.
                public const int NS_E_WMPCORE_BUFFERTOOSMALL = -1072885633;

                //
                // Zusammenfassung:
                //     The current state of the Player does not allow this operation.
                public const int NS_E_WMPCORE_UNAVAILABLE = -1072885632;

                //
                // Zusammenfassung:
                //     The playlist manager does not understand the current play mode (for example,
                //     shuffle or normal).
                public const int NS_E_WMPCORE_INVALIDPLAYLISTMODE = -1072885631;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because it is not in the current playlist.
                public const int NS_E_WMPCORE_ITEMNOTINPLAYLIST = -1072885626;

                //
                // Zusammenfassung:
                //     There are no items in the playlist. Add items to the playlist, and then try again.
                public const int NS_E_WMPCORE_PLAYLISTEMPTY = -1072885625;

                //
                // Zusammenfassung:
                //     The web page cannot be displayed because no web browser is installed on your
                //     computer.
                public const int NS_E_WMPCORE_NOBROWSER = -1072885624;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot find the specified file. Verify the path is typed
                //     correctly. If it is, the file does not exist in the specified location, or the
                //     computer where the file is stored is not available.
                public const int NS_E_WMPCORE_UNRECOGNIZED_MEDIA_URL = -1072885623;

                //
                // Zusammenfassung:
                //     Graph with the specified URL was not found in the prerolled graph list.
                public const int NS_E_WMPCORE_GRAPH_NOT_IN_LIST = -1072885622;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot perform the requested operation because there is
                //     only one item in the playlist.
                public const int NS_E_WMPCORE_PLAYLIST_EMPTY_OR_SINGLE_MEDIA = -1072885621;

                //
                // Zusammenfassung:
                //     An error sink was never registered for the calling object.
                public const int NS_E_WMPCORE_ERRORSINKNOTREGISTERED = -1072885620;

                //
                // Zusammenfassung:
                //     The error manager is not available to respond to errors.
                public const int NS_E_WMPCORE_ERRORMANAGERNOTAVAILABLE = -1072885619;

                //
                // Zusammenfassung:
                //     The Web Help URL cannot be opened.
                public const int NS_E_WMPCORE_WEBHELPFAILED = -1072885618;

                //
                // Zusammenfassung:
                //     Could not resume playing next item in playlist.
                public const int NS_E_WMPCORE_MEDIA_ERROR_RESUME_FAILED = -1072885617;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because the associated playlist does
                //     not contain any items or the playlist is not valid.
                public const int NS_E_WMPCORE_NO_REF_IN_ENTRY = -1072885616;

                //
                // Zusammenfassung:
                //     An empty string for playlist attribute name was found.
                public const int NS_E_WMPCORE_WMX_LIST_ATTRIBUTE_NAME_EMPTY = -1072885615;

                //
                // Zusammenfassung:
                //     A playlist attribute name that is not valid was found.
                public const int NS_E_WMPCORE_WMX_LIST_ATTRIBUTE_NAME_ILLEGAL = -1072885614;

                //
                // Zusammenfassung:
                //     An empty string for a playlist attribute value was found.
                public const int NS_E_WMPCORE_WMX_LIST_ATTRIBUTE_VALUE_EMPTY = -1072885613;

                //
                // Zusammenfassung:
                //     An illegal value for a playlist attribute was found.
                public const int NS_E_WMPCORE_WMX_LIST_ATTRIBUTE_VALUE_ILLEGAL = -1072885612;

                //
                // Zusammenfassung:
                //     An empty string for a playlist item attribute name was found.
                public const int NS_E_WMPCORE_WMX_LIST_ITEM_ATTRIBUTE_NAME_EMPTY = -1072885611;

                //
                // Zusammenfassung:
                //     An illegal value for a playlist item attribute name was found.
                public const int NS_E_WMPCORE_WMX_LIST_ITEM_ATTRIBUTE_NAME_ILLEGAL = -1072885610;

                //
                // Zusammenfassung:
                //     An illegal value for a playlist item attribute was found.
                public const int NS_E_WMPCORE_WMX_LIST_ITEM_ATTRIBUTE_VALUE_EMPTY = -1072885609;

                //
                // Zusammenfassung:
                //     The playlist does not contain any items.
                public const int NS_E_WMPCORE_LIST_ENTRY_NO_REF = -1072885608;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file. The file is either corrupted or the
                //     Player does not support the format you are trying to play.
                public const int NS_E_WMPCORE_MISNAMED_FILE = -1072885607;

                //
                // Zusammenfassung:
                //     The codec downloaded for this file does not appear to be properly signed, so
                //     it cannot be installed.
                public const int NS_E_WMPCORE_CODEC_NOT_TRUSTED = -1072885606;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file. One or more codecs required to play
                //     the file could not be found.
                public const int NS_E_WMPCORE_CODEC_NOT_FOUND = -1072885605;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because a required codec is not installed
                //     on your computer. To try downloading the codec, turn on the "Download codecs
                //     automatically" option.
                public const int NS_E_WMPCORE_CODEC_DOWNLOAD_NOT_ALLOWED = -1072885604;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered a problem while downloading the playlist. For
                //     additional assistance, click Web Help.
                public const int NS_E_WMPCORE_ERROR_DOWNLOADING_PLAYLIST = -1072885603;

                //
                // Zusammenfassung:
                //     Failed to build the playlist.
                public const int NS_E_WMPCORE_FAILED_TO_BUILD_PLAYLIST = -1072885602;

                //
                // Zusammenfassung:
                //     Playlist has no alternates to switch into.
                public const int NS_E_WMPCORE_PLAYLIST_ITEM_ALTERNATE_NONE = -1072885601;

                //
                // Zusammenfassung:
                //     No more playlist alternates available to switch to.
                public const int NS_E_WMPCORE_PLAYLIST_ITEM_ALTERNATE_EXHAUSTED = -1072885600;

                //
                // Zusammenfassung:
                //     Could not find the name of the alternate playlist to switch into.
                public const int NS_E_WMPCORE_PLAYLIST_ITEM_ALTERNATE_NAME_NOT_FOUND = -1072885599;

                //
                // Zusammenfassung:
                //     Failed to switch to an alternate for this media.
                public const int NS_E_WMPCORE_PLAYLIST_ITEM_ALTERNATE_MORPH_FAILED = -1072885598;

                //
                // Zusammenfassung:
                //     Failed to initialize an alternate for the media.
                public const int NS_E_WMPCORE_PLAYLIST_ITEM_ALTERNATE_INIT_FAILED = -1072885597;

                //
                // Zusammenfassung:
                //     No URL specified for the roll over Refs in the playlist file.
                public const int NS_E_WMPCORE_MEDIA_ALTERNATE_REF_EMPTY = -1072885596;

                //
                // Zusammenfassung:
                //     Encountered a playlist with no name.
                public const int NS_E_WMPCORE_PLAYLIST_NO_EVENT_NAME = -1072885595;

                //
                // Zusammenfassung:
                //     A required attribute in the event block of the playlist was not found.
                public const int NS_E_WMPCORE_PLAYLIST_EVENT_ATTRIBUTE_ABSENT = -1072885594;

                //
                // Zusammenfassung:
                //     No items were found in the event block of the playlist.
                public const int NS_E_WMPCORE_PLAYLIST_EVENT_EMPTY = -1072885593;

                //
                // Zusammenfassung:
                //     No playlist was found while returning from a nested playlist.
                public const int NS_E_WMPCORE_PLAYLIST_STACK_EMPTY = -1072885592;

                //
                // Zusammenfassung:
                //     The media item is not active currently.
                public const int NS_E_WMPCORE_CURRENT_MEDIA_NOT_ACTIVE = -1072885591;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot perform the requested action because you chose to
                //     cancel it.
                public const int NS_E_WMPCORE_USER_CANCEL = -1072885589;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered a problem with the playlist. The format of the
                //     playlist is not valid.
                public const int NS_E_WMPCORE_PLAYLIST_REPEAT_EMPTY = -1072885588;

                //
                // Zusammenfassung:
                //     Media object corresponding to start of a playlist repeat block was not found.
                public const int NS_E_WMPCORE_PLAYLIST_REPEAT_START_MEDIA_NONE = -1072885587;

                //
                // Zusammenfassung:
                //     Media object corresponding to the end of a playlist repeat block was not found.
                public const int NS_E_WMPCORE_PLAYLIST_REPEAT_END_MEDIA_NONE = -1072885586;

                //
                // Zusammenfassung:
                //     The playlist URL supplied to the playlist manager is not valid.
                public const int NS_E_WMPCORE_INVALID_PLAYLIST_URL = -1072885585;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because it is corrupted.
                public const int NS_E_WMPCORE_MISMATCHED_RUNTIME = -1072885584;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot add the playlist to the library because the playlist
                //     does not contain any items.
                public const int NS_E_WMPCORE_PLAYLIST_IMPORT_FAILED_NO_ITEMS = -1072885583;

                //
                // Zusammenfassung:
                //     An error has occurred that could prevent the changing of the video contrast on
                //     this media.
                public const int NS_E_WMPCORE_VIDEO_TRANSFORM_FILTER_INSERTION = -1072885582;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file. If the file is located on the Internet,
                //     connect to the Internet. If the file is located on a removable storage card,
                //     insert the storage card.
                public const int NS_E_WMPCORE_MEDIA_UNAVAILABLE = -1072885581;

                //
                // Zusammenfassung:
                //     The playlist contains an ENTRYREF for which no href was parsed. Check the syntax
                //     of playlist file.
                public const int NS_E_WMPCORE_WMX_ENTRYREF_NO_REF = -1072885580;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play any items in the playlist. To find information
                //     about the problem, click the Now Playing tab, and then click the icon next to
                //     each file in the List pane.
                public const int NS_E_WMPCORE_NO_PLAYABLE_MEDIA_IN_PLAYLIST = -1072885579;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play some or all of the items in the playlist because
                //     the playlist is nested.
                public const int NS_E_WMPCORE_PLAYLIST_EMPTY_NESTED_PLAYLIST_SKIPPED_ITEMS = -1072885578;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file at this time. Try again later.
                public const int NS_E_WMPCORE_BUSY = -1072885577;

                //
                // Zusammenfassung:
                //     There is no child playlist available for this media item at this time.
                public const int NS_E_WMPCORE_MEDIA_CHILD_PLAYLIST_UNAVAILABLE = -1072885576;

                //
                // Zusammenfassung:
                //     There is no child playlist for this media item.
                public const int NS_E_WMPCORE_MEDIA_NO_CHILD_PLAYLIST = -1072885575;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot find the file. The link from the item in the library
                //     to its associated digital media file might be broken. To fix the problem, try
                //     repairing the link or removing the item from the library.
                public const int NS_E_WMPCORE_FILE_NOT_FOUND = -1072885574;

                //
                // Zusammenfassung:
                //     The temporary file was not found.
                public const int NS_E_WMPCORE_TEMP_FILE_NOT_FOUND = -1072885573;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot sync the file because the device needs to be updated.
                public const int NS_E_WMDM_REVOKED = -1072885572;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the video because there is a problem with your
                //     video card.
                public const int NS_E_DDRAW_GENERIC = -1072885571;

                //
                // Zusammenfassung:
                //     Windows Media Player failed to change the screen mode for full-screen video playback.
                public const int NS_E_DISPLAY_MODE_CHANGE_FAILED = -1072885570;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play one or more files. For additional information,
                //     right-click an item that cannot be played, and then click Error Details.
                public const int NS_E_PLAYLIST_CONTAINS_ERRORS = -1072885569;

                //
                // Zusammenfassung:
                //     Cannot change the proxy name if the proxy setting is not set to custom.
                public const int NS_E_CHANGING_PROXY_NAME = -1072885568;

                //
                // Zusammenfassung:
                //     Cannot change the proxy port if the proxy setting is not set to custom.
                public const int NS_E_CHANGING_PROXY_PORT = -1072885567;

                //
                // Zusammenfassung:
                //     Cannot change the proxy exception list if the proxy setting is not set to custom.
                public const int NS_E_CHANGING_PROXY_EXCEPTIONLIST = -1072885566;

                //
                // Zusammenfassung:
                //     Cannot change the proxy bypass flag if the proxy setting is not set to custom.
                public const int NS_E_CHANGING_PROXYBYPASS = -1072885565;

                //
                // Zusammenfassung:
                //     Cannot find the specified protocol.
                public const int NS_E_CHANGING_PROXY_PROTOCOL_NOT_FOUND = -1072885564;

                //
                // Zusammenfassung:
                //     Cannot change the language settings. Either the graph has no audio or the audio
                //     only supports one language.
                public const int NS_E_GRAPH_NOAUDIOLANGUAGE = -1072885563;

                //
                // Zusammenfassung:
                //     The graph has no audio language selected.
                public const int NS_E_GRAPH_NOAUDIOLANGUAGESELECTED = -1072885562;

                //
                // Zusammenfassung:
                //     This is not a media CD.
                public const int NS_E_CORECD_NOTAMEDIACD = -1072885561;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because the URL is too long.
                public const int NS_E_WMPCORE_MEDIA_URL_TOO_LONG = -1072885560;

                //
                // Zusammenfassung:
                //     To play the selected item, you must install the Macromedia Flash Player. To download
                //     the Macromedia Flash Player, go to the Adobe website.
                public const int NS_E_WMPFLASH_CANT_FIND_COM_SERVER = -1072885559;

                //
                // Zusammenfassung:
                //     To play the selected item, you must install a later version of the Macromedia
                //     Flash Player. To download the Macromedia Flash Player, go to the Adobe website.
                public const int NS_E_WMPFLASH_INCOMPATIBLEVERSION = -1072885558;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because your Internet security settings
                //     prohibit the use of ActiveX controls.
                public const int NS_E_WMPOCXGRAPH_IE_DISALLOWS_ACTIVEX_CONTROLS = -1072885557;

                //
                // Zusammenfassung:
                //     The use of this method requires an existing reference to the Player object.
                public const int NS_E_NEED_CORE_REFERENCE = -1072885556;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the CD. The disc might be dirty or damaged.
                public const int NS_E_MEDIACD_READ_ERROR = -1072885555;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because your Internet security settings
                //     prohibit the use of ActiveX controls.
                public const int NS_E_IE_DISALLOWS_ACTIVEX_CONTROLS = -1072885554;

                //
                // Zusammenfassung:
                //     Flash playback has been turned off in Windows Media Player.
                public const int NS_E_FLASH_PLAYBACK_NOT_ALLOWED = -1072885553;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot rip the CD because a valid rip location cannot be
                //     created.
                public const int NS_E_UNABLE_TO_CREATE_RIP_LOCATION = -1072885552;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because a required codec is not installed
                //     on your computer.
                public const int NS_E_WMPCORE_SOME_CODECS_MISSING = -1072885551;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot rip one or more tracks from the CD.
                public const int NS_E_WMP_RIP_FAILED = -1072885550;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered a problem while ripping the track from the CD.
                //     For additional assistance, click Web Help.
                public const int NS_E_WMP_FAILED_TO_RIP_TRACK = -1072885549;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered a problem while erasing the disc. For additional
                //     assistance, click Web Help.
                public const int NS_E_WMP_ERASE_FAILED = -1072885548;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered a problem while formatting the device. For additional
                //     assistance, click Web Help.
                public const int NS_E_WMP_FORMAT_FAILED = -1072885547;

                //
                // Zusammenfassung:
                //     This file cannot be burned to a CD because it is not located on your computer.
                public const int NS_E_WMP_CANNOT_BURN_NON_LOCAL_FILE = -1072885546;

                //
                // Zusammenfassung:
                //     It is not possible to burn this file type to an audio CD. Windows Media Player
                //     can burn the following file types to an audio CD: WMA, MP3, or WAV.
                public const int NS_E_WMP_FILE_TYPE_CANNOT_BURN_TO_AUDIO_CD = -1072885545;

                //
                // Zusammenfassung:
                //     This file is too large to fit on a disc.
                public const int NS_E_WMP_FILE_DOES_NOT_FIT_ON_CD = -1072885544;

                //
                // Zusammenfassung:
                //     It is not possible to determine if this file can fit on a disc because Windows
                //     Media Player cannot detect the length of the file. Playing the file before burning
                //     might enable the Player to detect the file length.
                public const int NS_E_WMP_FILE_NO_DURATION = -1072885543;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered a problem while burning the file to the disc.
                //     For additional assistance, click Web Help.
                public const int NS_E_PDA_FAILED_TO_BURN = -1072885542;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot burn the audio CD because some items in the list
                //     that you chose to buy could not be downloaded from the online store.
                public const int NS_E_FAILED_DOWNLOAD_ABORT_BURN = -1072885540;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file. Try using Windows Update or Device
                //     Manager to update the device drivers for your audio and video cards. For information
                //     about using Windows Update or Device Manager, see Windows Help.
                public const int NS_E_WMPCORE_DEVICE_DRIVERS_MISSING = -1072885539;

                //
                // Zusammenfassung:
                //     Windows Media Player has detected that you are not connected to the Internet.
                //     Connect to the Internet, and then try again.
                public const int NS_E_WMPIM_USEROFFLINE = -1072885466;

                //
                // Zusammenfassung:
                //     The attempt to connect to the Internet was canceled.
                public const int NS_E_WMPIM_USERCANCELED = -1072885465;

                //
                // Zusammenfassung:
                //     The attempt to connect to the Internet failed.
                public const int NS_E_WMPIM_DIALUPFAILED = -1072885464;

                //
                // Zusammenfassung:
                //     Windows Media Player has encountered an unknown network error.
                public const int NS_E_WINSOCK_ERROR_STRING = -1072885463;

                //
                // Zusammenfassung:
                //     No window is currently listening to Backup and Restore events.
                public const int NS_E_WMPBR_NOLISTENER = -1072885456;

                //
                // Zusammenfassung:
                //     Your media usage rights were not backed up because the backup was canceled.
                public const int NS_E_WMPBR_BACKUPCANCEL = -1072885455;

                //
                // Zusammenfassung:
                //     Your media usage rights were not restored because the restoration was canceled.
                public const int NS_E_WMPBR_RESTORECANCEL = -1072885454;

                //
                // Zusammenfassung:
                //     An error occurred while backing up or restoring your media usage rights. A required
                //     web page cannot be displayed.
                public const int NS_E_WMPBR_ERRORWITHURL = -1072885453;

                //
                // Zusammenfassung:
                //     Your media usage rights were not backed up because the backup was canceled.
                public const int NS_E_WMPBR_NAMECOLLISION = -1072885452;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot restore your media usage rights from the specified
                //     location. Choose another location, and then try again.
                public const int NS_E_WMPBR_DRIVE_INVALID = -1072885449;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot backup or restore your media usage rights.
                public const int NS_E_WMPBR_BACKUPRESTOREFAILED = -1072885448;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot add the file to the library.
                public const int NS_E_WMP_CONVERT_FILE_FAILED = -1072885416;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot add the file to the library because the content provider
                //     prohibits it. For assistance, contact the company that provided the file.
                public const int NS_E_WMP_CONVERT_NO_RIGHTS_ERRORURL = -1072885415;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot add the file to the library because the content provider
                //     prohibits it. For assistance, contact the company that provided the file.
                public const int NS_E_WMP_CONVERT_NO_RIGHTS_NOERRORURL = -1072885414;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot add the file to the library. The file might not be
                //     valid.
                public const int NS_E_WMP_CONVERT_FILE_CORRUPT = -1072885413;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot add the file to the library. The plug-in required
                //     to add the file is not installed properly. For assistance, click Web Help to
                //     display the website of the company that provided the file.
                public const int NS_E_WMP_CONVERT_PLUGIN_UNAVAILABLE_ERRORURL = -1072885412;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot add the file to the library. The plug-in required
                //     to add the file is not installed properly. For assistance, contact the company
                //     that provided the file.
                public const int NS_E_WMP_CONVERT_PLUGIN_UNAVAILABLE_NOERRORURL = -1072885411;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot add the file to the library. The plug-in required
                //     to add the file is not installed properly. For assistance, contact the company
                //     that provided the file.
                public const int NS_E_WMP_CONVERT_PLUGIN_UNKNOWN_FILE_OWNER = -1072885410;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play this DVD. Try installing an updated driver for
                //     your video card or obtaining a newer video card.
                public const int NS_E_DVD_DISC_COPY_PROTECT_OUTPUT_NS = -1072885408;

                //
                // Zusammenfassung:
                //     This DVD's resolution exceeds the maximum allowed by your component video outputs.
                //     Try reducing your screen resolution to 640 x 480, or turn off analog component
                //     outputs and use a VGA connection to your monitor.
                public const int NS_E_DVD_DISC_COPY_PROTECT_OUTPUT_FAILED = -1072885407;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot display subtitles or highlights in DVD menus. Reinstall
                //     the DVD decoder or contact the DVD drive manufacturer to obtain an updated decoder.
                public const int NS_E_DVD_NO_SUBPICTURE_STREAM = -1072885406;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play this DVD because there is a problem with digital
                //     copy protection between your DVD drive, decoder, and video card. Try installing
                //     an updated driver for your video card.
                public const int NS_E_DVD_COPY_PROTECT = -1072885405;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the DVD. The disc was created in a manner that
                //     the Player does not support.
                public const int NS_E_DVD_AUTHORING_PROBLEM = -1072885404;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the DVD because the disc prohibits playback
                //     in your region of the world. You must obtain a disc that is intended for your
                //     geographic region.
                public const int NS_E_DVD_INVALID_DISC_REGION = -1072885403;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the DVD because your video card does not support
                //     DVD playback.
                public const int NS_E_DVD_COMPATIBLE_VIDEO_CARD = -1072885402;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play this DVD because it is not possible to turn
                //     on analog copy protection on the output display. Try installing an updated driver
                //     for your video card.
                public const int NS_E_DVD_MACROVISION = -1072885401;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the DVD because the region assigned to your
                //     DVD drive does not match the region assigned to your DVD decoder.
                public const int NS_E_DVD_SYSTEM_DECODER_REGION = -1072885400;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the DVD because the disc prohibits playback
                //     in your region of the world. You must obtain a disc that is intended for your
                //     geographic region.
                public const int NS_E_DVD_DISC_DECODER_REGION = -1072885399;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play DVD video. You might need to adjust your Windows
                //     display settings. Open display settings in Control Panel, and then try lowering
                //     your screen resolution and color quality settings.
                public const int NS_E_DVD_NO_VIDEO_STREAM = -1072885398;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play DVD audio. Verify that your sound card is set
                //     up correctly, and then try again.
                public const int NS_E_DVD_NO_AUDIO_STREAM = -1072885397;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play DVD video. Close any open files and quit any
                //     other programs, and then try again. If the problem persists, restart your computer.
                public const int NS_E_DVD_GRAPH_BUILDING = -1072885396;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the DVD because a compatible DVD decoder is
                //     not installed on your computer.
                public const int NS_E_DVD_NO_DECODER = -1072885395;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the scene because it has a parental rating higher
                //     than the rating that you are authorized to view.
                public const int NS_E_DVD_PARENTAL = -1072885394;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot skip to the requested location on the DVD.
                public const int NS_E_DVD_CANNOT_JUMP = -1072885393;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the DVD because it is currently in use by another
                //     program. Quit the other program that is using the DVD, and then try again.
                public const int NS_E_DVD_DEVICE_CONTENTION = -1072885392;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play DVD video. You might need to adjust your Windows
                //     display settings. Open display settings in Control Panel, and then try lowering
                //     your screen resolution and color quality settings.
                public const int NS_E_DVD_NO_VIDEO_MEMORY = -1072885391;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot rip the DVD because it is copy protected.
                public const int NS_E_DVD_CANNOT_COPY_PROTECTED = -1072885390;

                //
                // Zusammenfassung:
                //     One of more of the required properties has not been set.
                public const int NS_E_DVD_REQUIRED_PROPERTY_NOT_SET = -1072885389;

                //
                // Zusammenfassung:
                //     The specified title and/or chapter number does not exist on this DVD.
                public const int NS_E_DVD_INVALID_TITLE_CHAPTER = -1072885388;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot burn the files because the Player cannot find a burner.
                //     If the burner is connected properly, try using Windows Update to install the
                //     latest device driver.
                public const int NS_E_NO_CD_BURNER = -1072885386;

                //
                // Zusammenfassung:
                //     Windows Media Player does not detect storage media in the selected device. Insert
                //     storage media into the device, and then try again.
                public const int NS_E_DEVICE_IS_NOT_READY = -1072885385;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot sync this file. The Player might not support the
                //     file type.
                public const int NS_E_PDA_UNSUPPORTED_FORMAT = -1072885384;

                //
                // Zusammenfassung:
                //     Windows Media Player does not detect a portable device. Connect your portable
                //     device, and then try again.
                public const int NS_E_NO_PDA = -1072885383;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered an error while communicating with the device.
                //     The storage card on the device might be full, the device might be turned off,
                //     or the device might not allow playlists or folders to be created on it.
                public const int NS_E_PDA_UNSPECIFIED_ERROR = -1072885382;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered an error while burning a CD.
                public const int NS_E_MEMSTORAGE_BAD_DATA = -1072885381;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered an error while communicating with a portable
                //     device or CD drive.
                public const int NS_E_PDA_FAIL_SELECT_DEVICE = -1072885380;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot open the WAV file.
                public const int NS_E_PDA_FAIL_READ_WAVE_FILE = -1072885379;

                //
                // Zusammenfassung:
                //     Windows Media Player failed to burn all the files to the CD. Select a slower
                //     recording speed, and then try again.
                public const int NS_E_IMAPI_LOSSOFSTREAMING = -1072885378;

                //
                // Zusammenfassung:
                //     There is not enough storage space on the portable device to complete this operation.
                //     Delete some unneeded files on the portable device, and then try again.
                public const int NS_E_PDA_DEVICE_FULL = -1072885377;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot burn the files. Verify that your burner is connected
                //     properly, and then try again. If the problem persists, reinstall the Player.
                public const int NS_E_FAIL_LAUNCH_ROXIO_PLUGIN = -1072885376;

                //
                // Zusammenfassung:
                //     Windows Media Player did not sync some files to the device because there is not
                //     enough storage space on the device.
                public const int NS_E_PDA_DEVICE_FULL_IN_SESSION = -1072885375;

                //
                // Zusammenfassung:
                //     The disc in the burner is not valid. Insert a blank disc into the burner, and
                //     then try again.
                public const int NS_E_IMAPI_MEDIUM_INVALIDTYPE = -1072885374;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot perform the requested action because the device does
                //     not support sync.
                public const int NS_E_PDA_MANUALDEVICE = -1072885373;

                //
                // Zusammenfassung:
                //     To perform the requested action, you must first set up sync with the device.
                public const int NS_E_PDA_PARTNERSHIPNOTEXIST = -1072885372;

                //
                // Zusammenfassung:
                //     You have already created sync partnerships with 16 devices. To create a new sync
                //     partnership, you must first end an existing partnership.
                public const int NS_E_PDA_CANNOT_CREATE_ADDITIONAL_SYNC_RELATIONSHIP = -1072885371;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot sync the file because protected files cannot be converted
                //     to the required quality level or file format.
                public const int NS_E_PDA_NO_TRANSCODE_OF_DRM = -1072885370;

                //
                // Zusammenfassung:
                //     The folder that stores converted files is full. Either empty the folder or increase
                //     its size, and then try again.
                public const int NS_E_PDA_TRANSCODECACHEFULL = -1072885369;

                //
                // Zusammenfassung:
                //     There are too many files with the same name in the folder on the device. Change
                //     the file name or sync to a different folder.
                public const int NS_E_PDA_TOO_MANY_FILE_COLLISIONS = -1072885368;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot convert the file to the format required by the device.
                public const int NS_E_PDA_CANNOT_TRANSCODE = -1072885367;

                //
                // Zusammenfassung:
                //     You have reached the maximum number of files your device allows in a folder.
                //     If your device supports playback from subfolders, try creating subfolders on
                //     the device and storing some files in them.
                public const int NS_E_PDA_TOO_MANY_FILES_IN_DIRECTORY = -1072885366;

                //
                // Zusammenfassung:
                //     Windows Media Player is already trying to start the Device Setup Wizard.
                public const int NS_E_PROCESSINGSHOWSYNCWIZARD = -1072885365;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot convert this file format. If an updated version of
                //     the codec used to compress this file is available, install it and then try to
                //     sync the file again.
                public const int NS_E_PDA_TRANSCODE_NOT_PERMITTED = -1072885364;

                //
                // Zusammenfassung:
                //     Windows Media Player is busy setting up devices. Try again later.
                public const int NS_E_PDA_INITIALIZINGDEVICES = -1072885363;

                //
                // Zusammenfassung:
                //     Your device is using an outdated driver that is no longer supported by Windows
                //     Media Player. For additional assistance, click Web Help.
                public const int NS_E_PDA_OBSOLETE_SP = -1072885362;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot sync the file because a file with the same name already
                //     exists on the device. Change the file name or try to sync the file to a different
                //     folder.
                public const int NS_E_PDA_TITLE_COLLISION = -1072885361;

                //
                // Zusammenfassung:
                //     Automatic and manual sync have been turned off temporarily. To sync to a device,
                //     restart Windows Media Player.
                public const int NS_E_PDA_DEVICESUPPORTDISABLED = -1072885360;

                //
                // Zusammenfassung:
                //     This device is not available. Connect the device to the computer, and then try
                //     again.
                public const int NS_E_PDA_NO_LONGER_AVAILABLE = -1072885359;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot sync the file because an error occurred while converting
                //     the file to another quality level or format. If the problem persists, remove
                //     the file from the list of files to sync.
                public const int NS_E_PDA_ENCODER_NOT_RESPONDING = -1072885358;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot sync the file to your device. The file might be stored
                //     in a location that is not supported. Copy the file from its current location
                //     to your hard disk, add it to your library, and then try to sync the file again.
                public const int NS_E_PDA_CANNOT_SYNC_FROM_LOCATION = -1072885357;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot open the specified URL. Verify that the Player is
                //     configured to use all available protocols, and then try again.
                public const int NS_E_WMP_PROTOCOL_PROBLEM = -1072885356;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot perform the requested action because there is not
                //     enough storage space on your computer. Delete some unneeded files on your hard
                //     disk, and then try again.
                public const int NS_E_WMP_NO_DISK_SPACE = -1072885355;

                //
                // Zusammenfassung:
                //     The server denied access to the file. Verify that you are using the correct user
                //     name and password.
                public const int NS_E_WMP_LOGON_FAILURE = -1072885354;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot find the file. If you are trying to play, burn, or
                //     sync an item that is in your library, the item might point to a file that has
                //     been moved, renamed, or deleted.
                public const int NS_E_WMP_CANNOT_FIND_FILE = -1072885353;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot connect to the server. The server name might not
                //     be correct, the server might not be available, or your proxy settings might not
                //     be correct.
                public const int NS_E_WMP_SERVER_INACCESSIBLE = -1072885352;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file. The Player might not support the file
                //     type or might not support the codec that was used to compress the file.
                public const int NS_E_WMP_UNSUPPORTED_FORMAT = -1072885351;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file. The Player might not support the file
                //     type or a required codec might not be installed on your computer.
                public const int NS_E_WMP_DSHOW_UNSUPPORTED_FORMAT = -1072885350;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot create the playlist because the name already exists.
                //     Type a different playlist name.
                public const int NS_E_WMP_PLAYLIST_EXISTS = -1072885349;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot delete the playlist because it contains items that
                //     are not digital media files. Any digital media files in the playlist were deleted.
                public const int NS_E_WMP_NONMEDIA_FILES = -1072885348;

                //
                // Zusammenfassung:
                //     The playlist cannot be opened because it is stored in a shared folder on another
                //     computer. If possible, move the playlist to the playlists folder on your computer.
                public const int NS_E_WMP_INVALID_ASX = -1072885347;

                //
                // Zusammenfassung:
                //     Windows Media Player is already in use. Stop playing any items, close all Player
                //     dialog boxes, and then try again.
                public const int NS_E_WMP_ALREADY_IN_USE = -1072885346;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered an error while burning. Verify that the burner
                //     is connected properly and that the disc is clean and not damaged.
                public const int NS_E_WMP_IMAPI_FAILURE = -1072885345;

                //
                // Zusammenfassung:
                //     Windows Media Player has encountered an unknown error with your portable device.
                //     Reconnect your portable device, and then try again.
                public const int NS_E_WMP_WMDM_FAILURE = -1072885344;

                //
                // Zusammenfassung:
                //     A codec is required to play this file. To determine if this codec is available
                //     to download from the web, click Web Help.
                public const int NS_E_WMP_CODEC_NEEDED_WITH_4CC = -1072885343;

                //
                // Zusammenfassung:
                //     An audio codec is needed to play this file. To determine if this codec is available
                //     to download from the web, click Web Help.
                public const int NS_E_WMP_CODEC_NEEDED_WITH_FORMATTAG = -1072885342;

                //
                // Zusammenfassung:
                //     To play the file, you must install the latest Windows service pack. To install
                //     the service pack from the Windows Update website, click Web Help.
                public const int NS_E_WMP_MSSAP_NOT_AVAILABLE = -1072885341;

                //
                // Zusammenfassung:
                //     Windows Media Player no longer detects a portable device. Reconnect your portable
                //     device, and then try again.
                public const int NS_E_WMP_WMDM_INTERFACEDEAD = -1072885340;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot sync the file because the portable device does not
                //     support protected files.
                public const int NS_E_WMP_WMDM_NOTCERTIFIED = -1072885339;

                //
                // Zusammenfassung:
                //     This file does not have sync rights. If you obtained this file from an online
                //     store, go to the online store to get sync rights.
                public const int NS_E_WMP_WMDM_LICENSE_NOTEXIST = -1072885338;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot sync the file because the sync rights have expired.
                //     Go to the content provider's online store to get new sync rights.
                public const int NS_E_WMP_WMDM_LICENSE_EXPIRED = -1072885337;

                //
                // Zusammenfassung:
                //     The portable device is already in use. Wait until the current task finishes or
                //     quit other programs that might be using the portable device, and then try again.
                public const int NS_E_WMP_WMDM_BUSY = -1072885336;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot sync the file because the content provider or device
                //     prohibits it. You might be able to resolve this problem by going to the content
                //     provider's online store to get sync rights.
                public const int NS_E_WMP_WMDM_NORIGHTS = -1072885335;

                //
                // Zusammenfassung:
                //     The content provider has not granted you the right to sync this file. Go to the
                //     content provider's online store to get sync rights.
                public const int NS_E_WMP_WMDM_INCORRECT_RIGHTS = -1072885334;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot burn the files to the CD. Verify that the disc is
                //     clean and not damaged. If necessary, select a slower recording speed or try a
                //     different brand of blank discs.
                public const int NS_E_WMP_IMAPI_GENERIC = -1072885333;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot burn the files. Verify that the burner is connected
                //     properly, and then try again.
                public const int NS_E_WMP_IMAPI_DEVICE_NOTPRESENT = -1072885331;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot burn the files. Verify that the burner is connected
                //     properly and that the disc is clean and not damaged. If the burner is already
                //     in use, wait until the current task finishes or quit other programs that might
                //     be using the burner.
                public const int NS_E_WMP_IMAPI_DEVICE_BUSY = -1072885330;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot burn the files to the CD.
                public const int NS_E_WMP_IMAPI_LOSS_OF_STREAMING = -1072885329;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file. The server might not be available
                //     or there might be a problem with your network or firewall settings.
                public const int NS_E_WMP_SERVER_UNAVAILABLE = -1072885328;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered a problem while playing the file. For additional
                //     assistance, click Web Help.
                public const int NS_E_WMP_FILE_OPEN_FAILED = -1072885327;

                //
                // Zusammenfassung:
                //     Windows Media Player must connect to the Internet to verify the file's media
                //     usage rights. Connect to the Internet, and then try again.
                public const int NS_E_WMP_VERIFY_ONLINE = -1072885326;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because a network error occurred. The
                //     server might not be available. Verify that you are connected to the network and
                //     that your proxy settings are correct.
                public const int NS_E_WMP_SERVER_NOT_RESPONDING = -1072885325;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot restore your media usage rights because it could
                //     not find any backed up rights on your computer.
                public const int NS_E_WMP_DRM_CORRUPT_BACKUP = -1072885324;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot download media usage rights because the server is
                //     not available (for example, the server might be busy or not online).
                public const int NS_E_WMP_DRM_LICENSE_SERVER_UNAVAILABLE = -1072885323;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file. A network firewall might be preventing
                //     the Player from opening the file by using the UDP transport protocol. If you
                //     typed a URL in the Open URL dialog box, try using a different transport protocol
                //     (for example, "http:").
                public const int NS_E_WMP_NETWORK_FIREWALL = -1072885322;

                //
                // Zusammenfassung:
                //     Insert the removable media, and then try again.
                public const int NS_E_WMP_NO_REMOVABLE_MEDIA = -1072885321;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because the proxy server is not responding.
                //     The proxy server might be temporarily unavailable or your Player proxy settings
                //     might not be valid.
                public const int NS_E_WMP_PROXY_CONNECT_TIMEOUT = -1072885320;

                //
                // Zusammenfassung:
                //     To play the file, you might need to install a later version of Windows Media
                //     Player. On the Help menu, click Check for Updates, and then follow the instructions.
                //     For additional assistance, click Web Help.
                public const int NS_E_WMP_NEED_UPGRADE = -1072885319;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because there is a problem with your
                //     sound device. There might not be a sound device installed on your computer, it
                //     might be in use by another program, or it might not be functioning properly.
                public const int NS_E_WMP_AUDIO_HW_PROBLEM = -1072885318;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because the specified protocol is not
                //     supported. If you typed a URL in the Open URL dialog box, try using a different
                //     transport protocol (for example, "http:" or "rtsp:").
                public const int NS_E_WMP_INVALID_PROTOCOL = -1072885317;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot add the file to the library because the file format
                //     is not supported.
                public const int NS_E_WMP_INVALID_LIBRARY_ADD = -1072885316;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because the specified protocol is not
                //     supported. If you typed a URL in the Open URL dialog box, try using a different
                //     transport protocol (for example, "mms:").
                public const int NS_E_WMP_MMS_NOT_SUPPORTED = -1072885315;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because there are no streaming protocols
                //     selected. Select one or more protocols, and then try again.
                public const int NS_E_WMP_NO_PROTOCOLS_SELECTED = -1072885314;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot switch to Full Screen. You might need to adjust your
                //     Windows display settings. Open display settings in Control Panel, and then try
                //     setting Hardware acceleration to Full.
                public const int NS_E_WMP_GOFULLSCREEN_FAILED = -1072885313;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because a network error occurred. The
                //     server might not be available (for example, the server is busy or not online)
                //     or you might not be connected to the network.
                public const int NS_E_WMP_NETWORK_ERROR = -1072885312;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because the server is not responding.
                //     Verify that you are connected to the network, and then try again later.
                public const int NS_E_WMP_CONNECT_TIMEOUT = -1072885311;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because the multicast protocol is not
                //     enabled. On the Tools menu, click Options, click the Network tab, and then select
                //     the Multicast check box. For additional assistance, click Web Help.
                public const int NS_E_WMP_MULTICAST_DISABLED = -1072885310;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because a network problem occurred.
                //     Verify that you are connected to the network, and then try again later.
                public const int NS_E_WMP_SERVER_DNS_TIMEOUT = -1072885309;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because the network proxy server cannot
                //     be found. Verify that your proxy settings are correct, and then try again.
                public const int NS_E_WMP_PROXY_NOT_FOUND = -1072885308;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because it is corrupted.
                public const int NS_E_WMP_TAMPERED_CONTENT = -1072885307;

                //
                // Zusammenfassung:
                //     Your computer is running low on memory. Quit other programs, and then try again.
                public const int NS_E_WMP_OUTOFMEMORY = -1072885306;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play, burn, rip, or sync the file because a required
                //     audio codec is not installed on your computer.
                public const int NS_E_WMP_AUDIO_CODEC_NOT_INSTALLED = -1072885305;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because the required video codec is
                //     not installed on your computer.
                public const int NS_E_WMP_VIDEO_CODEC_NOT_INSTALLED = -1072885304;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot burn the files. If the burner is busy, wait for the
                //     current task to finish. If necessary, verify that the burner is connected properly
                //     and that you have installed the latest device driver.
                public const int NS_E_WMP_IMAPI_DEVICE_INVALIDTYPE = -1072885303;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the protected file because there is a problem
                //     with your sound device. Try installing a new device driver or use a different
                //     sound device.
                public const int NS_E_WMP_DRM_DRIVER_AUTH_FAILURE = -1072885302;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered a network error. Restart the Player.
                public const int NS_E_WMP_NETWORK_RESOURCE_FAILURE = -1072885301;

                //
                // Zusammenfassung:
                //     Windows Media Player is not installed properly. Reinstall the Player.
                public const int NS_E_WMP_UPGRADE_APPLICATION = -1072885300;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered an unknown error. For additional assistance,
                //     click Web Help.
                public const int NS_E_WMP_UNKNOWN_ERROR = -1072885299;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because the required codec is not valid.
                public const int NS_E_WMP_INVALID_KEY = -1072885298;

                //
                // Zusammenfassung:
                //     The CD drive is in use by another user. Wait for the task to complete, and then
                //     try again.
                public const int NS_E_WMP_CD_ANOTHER_USER = -1072885297;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play, sync, or burn the protected file because a
                //     problem occurred with the Windows Media Digital Rights Management (DRM) system.
                //     You might need to connect to the Internet to update your DRM components. For
                //     additional assistance, click Web Help.
                public const int NS_E_WMP_DRM_NEEDS_AUTHORIZATION = -1072885296;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file because there might be a problem with
                //     your sound or video device. Try installing an updated device driver.
                public const int NS_E_WMP_BAD_DRIVER = -1072885295;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot access the file. The file might be in use, you might
                //     not have access to the computer where the file is stored, or your proxy settings
                //     might not be correct.
                public const int NS_E_WMP_ACCESS_DENIED = -1072885294;

                //
                // Zusammenfassung:
                //     The content provider prohibits this action. Go to the content provider's online
                //     store to get new media usage rights.
                public const int NS_E_WMP_LICENSE_RESTRICTS = -1072885293;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot perform the requested action at this time.
                public const int NS_E_WMP_INVALID_REQUEST = -1072885292;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot burn the files because there is not enough free disk
                //     space to store the temporary files. Delete some unneeded files on your hard disk,
                //     and then try again.
                public const int NS_E_WMP_CD_STASH_NO_SPACE = -1072885291;

                //
                // Zusammenfassung:
                //     Your media usage rights have become corrupted or are no longer valid. This might
                //     happen if you have replaced hardware components in your computer.
                public const int NS_E_WMP_DRM_NEW_HARDWARE = -1072885290;

                //
                // Zusammenfassung:
                //     The required Windows Media Digital Rights Management (DRM) component cannot be
                //     validated. You might be able resolve the problem by reinstalling the Player.
                public const int NS_E_WMP_DRM_INVALID_SIG = -1072885289;

                //
                // Zusammenfassung:
                //     You have exceeded your restore limit for the day. Try restoring your media usage
                //     rights tomorrow.
                public const int NS_E_WMP_DRM_CANNOT_RESTORE = -1072885288;

                //
                // Zusammenfassung:
                //     Some files might not fit on the CD. The required space cannot be calculated accurately
                //     because some files might be missing duration information. To ensure the calculation
                //     is accurate, play the files that are missing duration information.
                public const int NS_E_WMP_BURN_DISC_OVERFLOW = -1072885287;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot verify the file's media usage rights. If you obtained
                //     this file from an online store, go to the online store to get the necessary rights.
                public const int NS_E_WMP_DRM_GENERIC_LICENSE_FAILURE = -1072885286;

                //
                // Zusammenfassung:
                //     It is not possible to sync because this device's internal clock is not set correctly.
                //     To set the clock, select the option to set the device clock on the Privacy tab
                //     of the Options dialog box, connect to the Internet, and then sync the device
                //     again. For additional assistance, click Web Help.
                public const int NS_E_WMP_DRM_NO_SECURE_CLOCK = -1072885285;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play, burn, rip, or sync the protected file because
                //     you do not have the appropriate rights.
                public const int NS_E_WMP_DRM_NO_RIGHTS = -1072885284;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered an error during upgrade.
                public const int NS_E_WMP_DRM_INDIV_FAILED = -1072885283;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot connect to the server because it is not accepting
                //     any new connections. This could be because it has reached its maximum connection
                //     limit. Please try again later.
                public const int NS_E_WMP_SERVER_NONEWCONNECTIONS = -1072885282;

                //
                // Zusammenfassung:
                //     A number of queued files cannot be played. To find information about the problem,
                //     click the Now Playing tab, and then click the icon next to each file in the List
                //     pane.
                public const int NS_E_WMP_MULTIPLE_ERROR_IN_PLAYLIST = -1072885281;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered an error while erasing the rewritable CD or
                //     DVD. Verify that the CD or DVD burner is connected properly and that the disc
                //     is clean and not damaged.
                public const int NS_E_WMP_IMAPI2_ERASE_FAIL = -1072885280;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot erase the rewritable CD or DVD. Verify that the CD
                //     or DVD burner is connected properly and that the disc is clean and not damaged.
                //     If the burner is already in use, wait until the current task finishes or quit
                //     other programs that might be using the burner.
                public const int NS_E_WMP_IMAPI2_ERASE_DEVICE_BUSY = -1072885279;

                //
                // Zusammenfassung:
                //     A Windows Media Digital Rights Management (DRM) component encountered a problem.
                //     If you are trying to use a file that you obtained from an online store, try going
                //     to the online store and getting the appropriate usage rights.
                public const int NS_E_WMP_DRM_COMPONENT_FAILURE = -1072885278;

                //
                // Zusammenfassung:
                //     It is not possible to obtain device's certificate. Please contact the device
                //     manufacturer for a firmware update or for other steps to resolve this problem.
                public const int NS_E_WMP_DRM_NO_DEVICE_CERT = -1072885277;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered an error when connecting to the server. The
                //     security information from the server could not be validated.
                public const int NS_E_WMP_SERVER_SECURITY_ERROR = -1072885276;

                //
                // Zusammenfassung:
                //     An audio device was disconnected or reconfigured. Verify that the audio device
                //     is connected, and then try to play the item again.
                public const int NS_E_WMP_AUDIO_DEVICE_LOST = -1072885275;

                //
                // Zusammenfassung:
                //     Windows Media Player could not complete burning because the disc is not compatible
                //     with your drive. Try inserting a different kind of recordable media or use a
                //     disc that supports a write speed that is compatible with your drive.
                public const int NS_E_WMP_IMAPI_MEDIA_INCOMPATIBLE = -1072885274;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot save the sync settings because your device is full.
                //     Delete some unneeded files on your device and then try again.
                public const int NS_E_SYNCWIZ_DEVICE_FULL = -1072885266;

                //
                // Zusammenfassung:
                //     It is not possible to change sync settings at this time. Try again later.
                public const int NS_E_SYNCWIZ_CANNOT_CHANGE_SETTINGS = -1072885265;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot delete these files currently. If the Player is synchronizing,
                //     wait until it is complete and then try again.
                public const int NS_E_TRANSCODE_DELETECACHEERROR = -1072885264;

                //
                // Zusammenfassung:
                //     Windows Media Player could not use digital mode to read the CD. The Player has
                //     automatically switched the CD drive to analog mode. To switch back to digital
                //     mode, use the Devices tab. For additional assistance, click Web Help.
                public const int NS_E_CD_NO_BUFFERS_READ = -1072885256;

                //
                // Zusammenfassung:
                //     No CD track was specified for playback.
                public const int NS_E_CD_EMPTY_TRACK_QUEUE = -1072885255;

                //
                // Zusammenfassung:
                //     The CD filter was not able to create the CD reader.
                public const int NS_E_CD_NO_READER = -1072885254;

                //
                // Zusammenfassung:
                //     Invalid ISRC code.
                public const int NS_E_CD_ISRC_INVALID = -1072885253;

                //
                // Zusammenfassung:
                //     Invalid Media Catalog Number.
                public const int NS_E_CD_MEDIA_CATALOG_NUMBER_INVALID = -1072885252;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play audio CDs correctly because the CD drive is
                //     slow and error correction is turned on. To increase performance, turn off playback
                //     error correction for this drive.
                public const int NS_E_SLOW_READ_DIGITAL_WITH_ERRORCORRECTION = -1072885251;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot estimate the CD drive's playback speed because the
                //     CD track is too short.
                public const int NS_E_CD_SPEEDDETECT_NOT_ENOUGH_READS = -1072885250;

                //
                // Zusammenfassung:
                //     Cannot queue the CD track because queuing is not enabled.
                public const int NS_E_CD_QUEUEING_DISABLED = -1072885249;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot download additional media usage rights until the
                //     current download is complete.
                public const int NS_E_WMP_DRM_ACQUIRING_LICENSE = -1072885246;

                //
                // Zusammenfassung:
                //     The media usage rights for this file have expired or are no longer valid. If
                //     you obtained the file from an online store, sign in to the store, and then try
                //     again.
                public const int NS_E_WMP_DRM_LICENSE_EXPIRED = -1072885245;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot download the media usage rights for the file. If
                //     you obtained the file from an online store, sign in to the store, and then try
                //     again.
                public const int NS_E_WMP_DRM_LICENSE_NOTACQUIRED = -1072885244;

                //
                // Zusammenfassung:
                //     The media usage rights for this file are not yet valid. To see when they will
                //     become valid, right-click the file in the library, click Properties, and then
                //     click the Media Usage Rights tab.
                public const int NS_E_WMP_DRM_LICENSE_NOTENABLED = -1072885243;

                //
                // Zusammenfassung:
                //     The media usage rights for this file are not valid. If you obtained this file
                //     from an online store, contact the store for assistance.
                public const int NS_E_WMP_DRM_LICENSE_UNUSABLE = -1072885242;

                //
                // Zusammenfassung:
                //     The content provider has revoked the media usage rights for this file. If you
                //     obtained this file from an online store, ask the store if a new version of the
                //     file is available.
                public const int NS_E_WMP_DRM_LICENSE_CONTENT_REVOKED = -1072885241;

                //
                // Zusammenfassung:
                //     The media usage rights for this file require a feature that is not supported
                //     in your current version of Windows Media Player or your current version of Windows.
                //     Try installing the latest version of the Player. If you obtained this file from
                //     an online store, contact the store for further assistance.
                public const int NS_E_WMP_DRM_LICENSE_NOSAP = -1072885240;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot download media usage rights at this time. Try again
                //     later.
                public const int NS_E_WMP_DRM_UNABLE_TO_ACQUIRE_LICENSE = -1072885239;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play, burn, or sync the file because the media usage
                //     rights are missing. If you obtained the file from an online store, sign in to
                //     the store, and then try again.
                public const int NS_E_WMP_LICENSE_REQUIRED = -1072885238;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play, burn, or sync the file because the media usage
                //     rights are missing. If you obtained the file from an online store, sign in to
                //     the store, and then try again.
                public const int NS_E_WMP_PROTECTED_CONTENT = -1072885237;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot read a policy. This can occur when the policy does
                //     not exist in the registry or when the registry cannot be read.
                public const int NS_E_WMP_POLICY_VALUE_NOT_CONFIGURED = -1072885206;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot sync content streamed directly from the Internet.
                //     If possible, download the file to your computer, and then try to sync the file.
                public const int NS_E_PDA_CANNOT_SYNC_FROM_INTERNET = -1072885196;

                //
                // Zusammenfassung:
                //     This playlist is not valid or is corrupted. Create a new playlist using Windows
                //     Media Player, then sync the new playlist instead.
                public const int NS_E_PDA_CANNOT_SYNC_INVALID_PLAYLIST = -1072885195;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered a problem while synchronizing the file to the
                //     device. For additional assistance, click Web Help.
                public const int NS_E_PDA_FAILED_TO_SYNCHRONIZE_FILE = -1072885194;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered an error while synchronizing to the device.
                public const int NS_E_PDA_SYNC_FAILED = -1072885193;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot delete a file from the device.
                public const int NS_E_PDA_DELETE_FAILED = -1072885192;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot copy a file from the device to your library.
                public const int NS_E_PDA_FAILED_TO_RETRIEVE_FILE = -1072885191;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot communicate with the device because the device is
                //     not responding. Try reconnecting the device, resetting the device, or contacting
                //     the device manufacturer for updated firmware.
                public const int NS_E_PDA_DEVICE_NOT_RESPONDING = -1072885190;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot sync the picture to the device because a problem
                //     occurred while converting the file to another quality level or format. The original
                //     file might be damaged or corrupted.
                public const int NS_E_PDA_FAILED_TO_TRANSCODE_PHOTO = -1072885189;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot convert the file. The file might have been encrypted
                //     by the Encrypted File System (EFS). Try decrypting the file first and then synchronizing
                //     it. For information about how to decrypt a file, see Windows Help and Support.
                public const int NS_E_PDA_FAILED_TO_ENCRYPT_TRANSCODED_FILE = -1072885188;

                //
                // Zusammenfassung:
                //     Your device requires that this file be converted in order to play on the device.
                //     However, the device either does not support playing audio, or Windows Media Player
                //     cannot convert the file to an audio format that is supported by the device.
                public const int NS_E_PDA_CANNOT_TRANSCODE_TO_AUDIO = -1072885187;

                //
                // Zusammenfassung:
                //     Your device requires that this file be converted in order to play on the device.
                //     However, the device either does not support playing video, or Windows Media Player
                //     cannot convert the file to a video format that is supported by the device.
                public const int NS_E_PDA_CANNOT_TRANSCODE_TO_VIDEO = -1072885186;

                //
                // Zusammenfassung:
                //     Your device requires that this file be converted in order to play on the device.
                //     However, the device either does not support displaying pictures, or Windows Media
                //     Player cannot convert the file to a picture format that is supported by the device.
                public const int NS_E_PDA_CANNOT_TRANSCODE_TO_IMAGE = -1072885185;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot sync the file to your computer because the file name
                //     is too long. Try renaming the file on the device.
                public const int NS_E_PDA_RETRIEVED_FILE_FILENAME_TOO_LONG = -1072885184;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot sync the file because the device is not responding.
                //     This typically occurs when there is a problem with the device firmware. For additional
                //     assistance, click Web Help.
                public const int NS_E_PDA_CEWMDM_DRM_ERROR = -1072885183;

                //
                // Zusammenfassung:
                //     Incomplete playlist.
                public const int NS_E_INCOMPLETE_PLAYLIST = -1072885182;

                //
                // Zusammenfassung:
                //     It is not possible to perform the requested action because sync is in progress.
                //     You can either stop sync or wait for it to complete, and then try again.
                public const int NS_E_PDA_SYNC_RUNNING = -1072885181;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot sync the subscription content because you are not
                //     signed in to the online store that provided it. Sign in to the online store,
                //     and then try again.
                public const int NS_E_PDA_SYNC_LOGIN_ERROR = -1072885180;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot convert the file to the format required by the device.
                //     One or more codecs required to convert the file could not be found.
                public const int NS_E_PDA_TRANSCODE_CODEC_NOT_FOUND = -1072885179;

                //
                // Zusammenfassung:
                //     It is not possible to sync subscription files to this device.
                public const int NS_E_CANNOT_SYNC_DRM_TO_NON_JANUS_DEVICE = -1072885178;

                //
                // Zusammenfassung:
                //     Your device is operating slowly or is not responding. Until the device responds,
                //     it is not possible to sync again. To return the device to normal operation, try
                //     disconnecting it from the computer or resetting it.
                public const int NS_E_CANNOT_SYNC_PREVIOUS_SYNC_RUNNING = -1072885177;

                //
                // Zusammenfassung:
                //     The Windows Media Player download manager cannot function properly because the
                //     Player main window cannot be found. Try restarting the Player.
                public const int NS_E_WMP_HWND_NOTFOUND = -1072885156;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered a download that has the wrong number of files.
                //     This might occur if another program is trying to create jobs with the same signature
                //     as the Player.
                public const int NS_E_BKGDOWNLOAD_WRONG_NO_FILES = -1072885155;

                //
                // Zusammenfassung:
                //     Windows Media Player tried to complete a download that was already canceled.
                //     The file will not be available.
                public const int NS_E_BKGDOWNLOAD_COMPLETECANCELLEDJOB = -1072885154;

                //
                // Zusammenfassung:
                //     Windows Media Player tried to cancel a download that was already completed. The
                //     file will not be removed.
                public const int NS_E_BKGDOWNLOAD_CANCELCOMPLETEDJOB = -1072885153;

                //
                // Zusammenfassung:
                //     Windows Media Player is trying to access a download that is not valid.
                public const int NS_E_BKGDOWNLOAD_NOJOBPOINTER = -1072885152;

                //
                // Zusammenfassung:
                //     This download was not created by Windows Media Player.
                public const int NS_E_BKGDOWNLOAD_INVALIDJOBSIGNATURE = -1072885151;

                //
                // Zusammenfassung:
                //     The Windows Media Player download manager cannot create a temporary file name.
                //     This might occur if the path is not valid or if the disk is full.
                public const int NS_E_BKGDOWNLOAD_FAILED_TO_CREATE_TEMPFILE = -1072885150;

                //
                // Zusammenfassung:
                //     The Windows Media Player download manager plug-in cannot start. This might occur
                //     if the system is out of resources.
                public const int NS_E_BKGDOWNLOAD_PLUGIN_FAILEDINITIALIZE = -1072885149;

                //
                // Zusammenfassung:
                //     The Windows Media Player download manager cannot move the file.
                public const int NS_E_BKGDOWNLOAD_PLUGIN_FAILEDTOMOVEFILE = -1072885148;

                //
                // Zusammenfassung:
                //     The Windows Media Player download manager cannot perform a task because the system
                //     has no resources to allocate.
                public const int NS_E_BKGDOWNLOAD_CALLFUNCFAILED = -1072885147;

                //
                // Zusammenfassung:
                //     The Windows Media Player download manager cannot perform a task because the task
                //     took too long to run.
                public const int NS_E_BKGDOWNLOAD_CALLFUNCTIMEOUT = -1072885146;

                //
                // Zusammenfassung:
                //     The Windows Media Player download manager cannot perform a task because the Player
                //     is terminating the service. The task will be recovered when the Player restarts.
                public const int NS_E_BKGDOWNLOAD_CALLFUNCENDED = -1072885145;

                //
                // Zusammenfassung:
                //     The Windows Media Player download manager cannot expand a WMD file. The file
                //     will be deleted and the operation will not be completed successfully.
                public const int NS_E_BKGDOWNLOAD_WMDUNPACKFAILED = -1072885144;

                //
                // Zusammenfassung:
                //     The Windows Media Player download manager cannot start. This might occur if the
                //     system is out of resources.
                public const int NS_E_BKGDOWNLOAD_FAILEDINITIALIZE = -1072885143;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot access a required functionality. This might occur
                //     if the wrong system files or Player DLLs are loaded.
                public const int NS_E_INTERFACE_NOT_REGISTERED_IN_GIT = -1072885142;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot get the file name of the requested download. The
                //     requested download will be canceled.
                public const int NS_E_BKGDOWNLOAD_INVALID_FILE_NAME = -1072885141;

                //
                // Zusammenfassung:
                //     Windows Media Player encountered an error while downloading an image.
                public const int NS_E_IMAGE_DOWNLOAD_FAILED = -1072885106;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot update your media usage rights because the Player
                //     cannot verify the list of activated users of this computer.
                public const int NS_E_WMP_UDRM_NOUSERLIST = -1072885056;

                //
                // Zusammenfassung:
                //     Windows Media Player is trying to acquire media usage rights for a file that
                //     is no longer being used. Rights acquisition will stop.
                public const int NS_E_WMP_DRM_NOT_ACQUIRING = -1072885055;

                //
                // Zusammenfassung:
                //     The parameter is not valid.
                public const int NS_E_WMP_BSTR_TOO_LONG = -1072885006;

                //
                // Zusammenfassung:
                //     The state is not valid for this request.
                public const int NS_E_WMP_AUTOPLAY_INVALID_STATE = -1072884996;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play this file until you complete the software component
                //     upgrade. After the component has been upgraded, try to play the file again.
                public const int NS_E_WMP_COMPONENT_REVOKED = -1072884986;

                //
                // Zusammenfassung:
                //     The URL is not safe for the operation specified.
                public const int NS_E_CURL_NOTSAFE = -1072884956;

                //
                // Zusammenfassung:
                //     The URL contains one or more characters that are not valid.
                public const int NS_E_CURL_INVALIDCHAR = -1072884955;

                //
                // Zusammenfassung:
                //     The URL contains a host name that is not valid.
                public const int NS_E_CURL_INVALIDHOSTNAME = -1072884954;

                //
                // Zusammenfassung:
                //     The URL contains a path that is not valid.
                public const int NS_E_CURL_INVALIDPATH = -1072884953;

                //
                // Zusammenfassung:
                //     The URL contains a scheme that is not valid.
                public const int NS_E_CURL_INVALIDSCHEME = -1072884952;

                //
                // Zusammenfassung:
                //     The URL is not valid.
                public const int NS_E_CURL_INVALIDURL = -1072884951;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the file. If you clicked a link on a web page,
                //     the link might not be valid.
                public const int NS_E_CURL_CANTWALK = -1072884949;

                //
                // Zusammenfassung:
                //     The URL port is not valid.
                public const int NS_E_CURL_INVALIDPORT = -1072884948;

                //
                // Zusammenfassung:
                //     The URL is not a directory.
                public const int NS_E_CURLHELPER_NOTADIRECTORY = -1072884947;

                //
                // Zusammenfassung:
                //     The URL is not a file.
                public const int NS_E_CURLHELPER_NOTAFILE = -1072884946;

                //
                // Zusammenfassung:
                //     The URL contains characters that cannot be decoded. The URL might be truncated
                //     or incomplete.
                public const int NS_E_CURL_CANTDECODE = -1072884945;

                //
                // Zusammenfassung:
                //     The specified URL is not a relative URL.
                public const int NS_E_CURLHELPER_NOTRELATIVE = -1072884944;

                //
                // Zusammenfassung:
                //     The buffer is smaller than the size specified.
                public const int NS_E_CURL_INVALIDBUFFERSIZE = -1072884943;

                //
                // Zusammenfassung:
                //     The content provider has not granted you the right to play this file. Go to the
                //     content provider's online store to get play rights.
                public const int NS_E_SUBSCRIPTIONSERVICE_PLAYBACK_DISALLOWED = -1072884906;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot purchase or download content from multiple online
                //     stores.
                public const int NS_E_CANNOT_BUY_OR_DOWNLOAD_FROM_MULTIPLE_SERVICES = -1072884905;

                //
                // Zusammenfassung:
                //     The file cannot be purchased or downloaded. The file might not be available from
                //     the online store.
                public const int NS_E_CANNOT_BUY_OR_DOWNLOAD_CONTENT = -1072884904;

                //
                // Zusammenfassung:
                //     The provider of this file cannot be identified.
                public const int NS_E_NOT_CONTENT_PARTNER_TRACK = -1072884902;

                //
                // Zusammenfassung:
                //     The file is only available for download when you buy the entire album.
                public const int NS_E_TRACK_DOWNLOAD_REQUIRES_ALBUM_PURCHASE = -1072884901;

                //
                // Zusammenfassung:
                //     You must buy the file before you can download it.
                public const int NS_E_TRACK_DOWNLOAD_REQUIRES_PURCHASE = -1072884900;

                //
                // Zusammenfassung:
                //     You have exceeded the maximum number of files that can be purchased in a single
                //     transaction.
                public const int NS_E_TRACK_PURCHASE_MAXIMUM_EXCEEDED = -1072884899;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot sign in to the online store. Verify that you are
                //     using the correct user name and password. If the problem persists, the store
                //     might be temporarily unavailable.
                public const int NS_E_SUBSCRIPTIONSERVICE_LOGIN_FAILED = -1072884897;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot download this item because the server is not responding.
                //     The server might be temporarily unavailable or the Internet connection might
                //     be lost.
                public const int NS_E_SUBSCRIPTIONSERVICE_DOWNLOAD_TIMEOUT = -1072884896;

                //
                // Zusammenfassung:
                //     Content Partner still initializing.
                public const int NS_E_CONTENT_PARTNER_STILL_INITIALIZING = -1072884894;

                //
                // Zusammenfassung:
                //     The folder could not be opened. The folder might have been moved or deleted.
                public const int NS_E_OPEN_CONTAINING_FOLDER_FAILED = -1072884893;

                //
                // Zusammenfassung:
                //     Windows Media Player could not add all of the images to the file because the
                //     images exceeded the 7 megabyte (MB) limit.
                public const int NS_E_ADVANCEDEDIT_TOO_MANY_PICTURES = -1072884886;

                //
                // Zusammenfassung:
                //     The client redirected to another server.
                public const int NS_E_REDIRECT = -1072884856;

                //
                // Zusammenfassung:
                //     The streaming media description is no longer current.
                public const int NS_E_STALE_PRESENTATION = -1072884855;

                //
                // Zusammenfassung:
                //     It is not possible to create a persistent namespace node under a transient parent
                //     node.
                public const int NS_E_NAMESPACE_WRONG_PERSIST = -1072884854;

                //
                // Zusammenfassung:
                //     It is not possible to store a value in a namespace node that has a different
                //     value type.
                public const int NS_E_NAMESPACE_WRONG_TYPE = -1072884853;

                //
                // Zusammenfassung:
                //     It is not possible to remove the root namespace node.
                public const int NS_E_NAMESPACE_NODE_CONFLICT = -1072884852;

                //
                // Zusammenfassung:
                //     The specified namespace node could not be found.
                public const int NS_E_NAMESPACE_NODE_NOT_FOUND = -1072884851;

                //
                // Zusammenfassung:
                //     The buffer supplied to hold namespace node string is too small.
                public const int NS_E_NAMESPACE_BUFFER_TOO_SMALL = -1072884850;

                //
                // Zusammenfassung:
                //     The callback list on a namespace node is at the maximum size.
                public const int NS_E_NAMESPACE_TOO_MANY_CALLBACKS = -1072884849;

                //
                // Zusammenfassung:
                //     It is not possible to register an already-registered callback on a namespace
                //     node.
                public const int NS_E_NAMESPACE_DUPLICATE_CALLBACK = -1072884848;

                //
                // Zusammenfassung:
                //     Cannot find the callback in the namespace when attempting to remove the callback.
                public const int NS_E_NAMESPACE_CALLBACK_NOT_FOUND = -1072884847;

                //
                // Zusammenfassung:
                //     The namespace node name exceeds the allowed maximum length.
                public const int NS_E_NAMESPACE_NAME_TOO_LONG = -1072884846;

                //
                // Zusammenfassung:
                //     Cannot create a namespace node that already exists.
                public const int NS_E_NAMESPACE_DUPLICATE_NAME = -1072884845;

                //
                // Zusammenfassung:
                //     The namespace node name cannot be a null string.
                public const int NS_E_NAMESPACE_EMPTY_NAME = -1072884844;

                //
                // Zusammenfassung:
                //     Finding a child namespace node by index failed because the index exceeded the
                //     number of children.
                public const int NS_E_NAMESPACE_INDEX_TOO_LARGE = -1072884843;

                //
                // Zusammenfassung:
                //     The namespace node name is invalid.
                public const int NS_E_NAMESPACE_BAD_NAME = -1072884842;

                //
                // Zusammenfassung:
                //     It is not possible to store a value in a namespace node that has a different
                //     security type.
                public const int NS_E_NAMESPACE_WRONG_SECURITY = -1072884841;

                //
                // Zusammenfassung:
                //     The archive request conflicts with other requests in progress.
                public const int NS_E_CACHE_ARCHIVE_CONFLICT = -1072884756;

                //
                // Zusammenfassung:
                //     The specified origin server cannot be found.
                public const int NS_E_CACHE_ORIGIN_SERVER_NOT_FOUND = -1072884755;

                //
                // Zusammenfassung:
                //     The specified origin server is not responding.
                public const int NS_E_CACHE_ORIGIN_SERVER_TIMEOUT = -1072884754;

                //
                // Zusammenfassung:
                //     The internal code for HTTP status code 412 Precondition Failed due to not broadcast
                //     type.
                public const int NS_E_CACHE_NOT_BROADCAST = -1072884753;

                //
                // Zusammenfassung:
                //     The internal code for HTTP status code 403 Forbidden due to not cacheable.
                public const int NS_E_CACHE_CANNOT_BE_CACHED = -1072884752;

                //
                // Zusammenfassung:
                //     The internal code for HTTP status code 304 Not Modified.
                public const int NS_E_CACHE_NOT_MODIFIED = -1072884751;

                //
                // Zusammenfassung:
                //     It is not possible to remove a cache or proxy publishing point.
                public const int NS_E_CANNOT_REMOVE_PUBLISHING_POINT = -1072884656;

                //
                // Zusammenfassung:
                //     It is not possible to remove the last instance of a type of plug-in.
                public const int NS_E_CANNOT_REMOVE_PLUGIN = -1072884655;

                //
                // Zusammenfassung:
                //     Cache and proxy publishing points do not support this property or method.
                public const int NS_E_WRONG_PUBLISHING_POINT_TYPE = -1072884654;

                //
                // Zusammenfassung:
                //     The plug-in does not support the specified load type.
                public const int NS_E_UNSUPPORTED_LOAD_TYPE = -1072884653;

                //
                // Zusammenfassung:
                //     The plug-in does not support any load types. The plug-in must support at least
                //     one load type.
                public const int NS_E_INVALID_PLUGIN_LOAD_TYPE_CONFIGURATION = -1072884652;

                //
                // Zusammenfassung:
                //     The publishing point name is invalid.
                public const int NS_E_INVALID_PUBLISHING_POINT_NAME = -1072884651;

                //
                // Zusammenfassung:
                //     Only one multicast data writer plug-in can be enabled for a publishing point.
                public const int NS_E_TOO_MANY_MULTICAST_SINKS = -1072884650;

                //
                // Zusammenfassung:
                //     The requested operation cannot be completed while the publishing point is started.
                public const int NS_E_PUBLISHING_POINT_INVALID_REQUEST_WHILE_STARTED = -1072884649;

                //
                // Zusammenfassung:
                //     A multicast data writer plug-in must be enabled in order for this operation to
                //     be completed.
                public const int NS_E_MULTICAST_PLUGIN_NOT_ENABLED = -1072884648;

                //
                // Zusammenfassung:
                //     This feature requires Windows Server 2003, Enterprise Edition.
                public const int NS_E_INVALID_OPERATING_SYSTEM_VERSION = -1072884647;

                //
                // Zusammenfassung:
                //     The requested operation cannot be completed because the specified publishing
                //     point has been removed.
                public const int NS_E_PUBLISHING_POINT_REMOVED = -1072884646;

                //
                // Zusammenfassung:
                //     Push publishing points are started when the encoder starts pushing the stream.
                //     This publishing point cannot be started by the server administrator.
                public const int NS_E_INVALID_PUSH_PUBLISHING_POINT_START_REQUEST = -1072884645;

                //
                // Zusammenfassung:
                //     The specified language is not supported.
                public const int NS_E_UNSUPPORTED_LANGUAGE = -1072884644;

                //
                // Zusammenfassung:
                //     Windows Media Services will only run on Windows Server 2003, Standard Edition
                //     and Windows Server 2003, Enterprise Edition.
                public const int NS_E_WRONG_OS_VERSION = -1072884643;

                //
                // Zusammenfassung:
                //     The operation cannot be completed because the publishing point has been stopped.
                public const int NS_E_PUBLISHING_POINT_STOPPED = -1072884642;

                //
                // Zusammenfassung:
                //     The playlist entry is already playing.
                public const int NS_E_PLAYLIST_ENTRY_ALREADY_PLAYING = -1072884556;

                //
                // Zusammenfassung:
                //     The playlist or directory you are requesting does not contain content.
                public const int NS_E_EMPTY_PLAYLIST = -1072884555;

                //
                // Zusammenfassung:
                //     The server was unable to parse the requested playlist file.
                public const int NS_E_PLAYLIST_PARSE_FAILURE = -1072884554;

                //
                // Zusammenfassung:
                //     The requested operation is not supported for this type of playlist entry.
                public const int NS_E_PLAYLIST_UNSUPPORTED_ENTRY = -1072884553;

                //
                // Zusammenfassung:
                //     Cannot jump to a playlist entry that is not inserted in the playlist.
                public const int NS_E_PLAYLIST_ENTRY_NOT_IN_PLAYLIST = -1072884552;

                //
                // Zusammenfassung:
                //     Cannot seek to the desired playlist entry.
                public const int NS_E_PLAYLIST_ENTRY_SEEK = -1072884551;

                //
                // Zusammenfassung:
                //     Cannot play recursive playlist.
                public const int NS_E_PLAYLIST_RECURSIVE_PLAYLISTS = -1072884550;

                //
                // Zusammenfassung:
                //     The number of nested playlists exceeded the limit the server can handle.
                public const int NS_E_PLAYLIST_TOO_MANY_NESTED_PLAYLISTS = -1072884549;

                //
                // Zusammenfassung:
                //     Cannot execute the requested operation because the playlist has been shut down
                //     by the Media Server.
                public const int NS_E_PLAYLIST_SHUTDOWN = -1072884548;

                //
                // Zusammenfassung:
                //     The playlist has ended while receding.
                public const int NS_E_PLAYLIST_END_RECEDING = -1072884547;

                //
                // Zusammenfassung:
                //     The data path does not have an associated data writer plug-in.
                public const int NS_E_DATAPATH_NO_SINK = -1072884456;

                //
                // Zusammenfassung:
                //     The specified push template is invalid.
                public const int NS_E_INVALID_PUSH_TEMPLATE = -1072884454;

                //
                // Zusammenfassung:
                //     The specified push publishing point is invalid.
                public const int NS_E_INVALID_PUSH_PUBLISHING_POINT = -1072884453;

                //
                // Zusammenfassung:
                //     The requested operation cannot be performed because the server or publishing
                //     point is in a critical error state.
                public const int NS_E_CRITICAL_ERROR = -1072884452;

                //
                // Zusammenfassung:
                //     The content cannot be played because the server is not currently accepting connections.
                //     Try connecting at a later time.
                public const int NS_E_NO_NEW_CONNECTIONS = -1072884451;

                //
                // Zusammenfassung:
                //     The version of this playlist is not supported by the server.
                public const int NS_E_WSX_INVALID_VERSION = -1072884450;

                //
                // Zusammenfassung:
                //     The command does not apply to the current media header user by a server component.
                public const int NS_E_HEADER_MISMATCH = -1072884449;

                //
                // Zusammenfassung:
                //     The specified publishing point name is already in use.
                public const int NS_E_PUSH_DUPLICATE_PUBLISHING_POINT_NAME = -1072884448;

                //
                // Zusammenfassung:
                //     There is no script engine available for this file.
                public const int NS_E_NO_SCRIPT_ENGINE = -1072884356;

                //
                // Zusammenfassung:
                //     The plug-in has reported an error. See the Troubleshooting tab or the NT Application
                //     Event Log for details.
                public const int NS_E_PLUGIN_ERROR_REPORTED = -1072884355;

                //
                // Zusammenfassung:
                //     No enabled data source plug-in is available to access the requested content.
                public const int NS_E_SOURCE_PLUGIN_NOT_FOUND = -1072884354;

                //
                // Zusammenfassung:
                //     No enabled playlist parser plug-in is available to access the requested content.
                public const int NS_E_PLAYLIST_PLUGIN_NOT_FOUND = -1072884353;

                //
                // Zusammenfassung:
                //     The data source plug-in does not support enumeration.
                public const int NS_E_DATA_SOURCE_ENUMERATION_NOT_SUPPORTED = -1072884352;

                //
                // Zusammenfassung:
                //     The server cannot stream the selected file because it is either damaged or corrupt.
                //     Select a different file.
                public const int NS_E_MEDIA_PARSER_INVALID_FORMAT = -1072884351;

                //
                // Zusammenfassung:
                //     The plug-in cannot be enabled because a compatible script debugger is not installed
                //     on this system. Install a script debugger, or disable the script debugger option
                //     on the general tab of the plug-in's properties page and try again.
                public const int NS_E_SCRIPT_DEBUGGER_NOT_INSTALLED = -1072884350;

                //
                // Zusammenfassung:
                //     The plug-in cannot be loaded because it requires Windows Server 2003, Enterprise
                //     Edition.
                public const int NS_E_FEATURE_REQUIRES_ENTERPRISE_SERVER = -1072884349;

                //
                // Zusammenfassung:
                //     Another wizard is currently running. Please close the other wizard or wait until
                //     it finishes before attempting to run this wizard again.
                public const int NS_E_WIZARD_RUNNING = -1072884348;

                //
                // Zusammenfassung:
                //     Invalid log URL. Multicast logging URL must look like "http://servername/isapibackend.dll".
                public const int NS_E_INVALID_LOG_URL = -1072884347;

                //
                // Zusammenfassung:
                //     Invalid MTU specified. The valid range for maximum packet size is between 36
                //     and 65507 bytes.
                public const int NS_E_INVALID_MTU_RANGE = -1072884346;

                //
                // Zusammenfassung:
                //     Invalid play statistics for logging.
                public const int NS_E_INVALID_PLAY_STATISTICS = -1072884345;

                //
                // Zusammenfassung:
                //     The log needs to be skipped.
                public const int NS_E_LOG_NEED_TO_BE_SKIPPED = -1072884344;

                //
                // Zusammenfassung:
                //     The size of the data exceeded the limit the WMS HTTP Download Data Source plugin
                //     can handle.
                public const int NS_E_HTTP_TEXT_DATACONTAINER_SIZE_LIMIT_EXCEEDED = -1072884343;

                //
                // Zusammenfassung:
                //     One usage of each socket address (protocol/network address/port) is permitted.
                //     Verify that other services or applications are not attempting to use the same
                //     port and then try to enable the plug-in again.
                public const int NS_E_PORT_IN_USE = -1072884342;

                //
                // Zusammenfassung:
                //     One usage of each socket address (protocol/network address/port) is permitted.
                //     Verify that other services (such as IIS) or applications are not attempting to
                //     use the same port and then try to enable the plug-in again.
                public const int NS_E_PORT_IN_USE_HTTP = -1072884341;

                //
                // Zusammenfassung:
                //     The WMS HTTP Download Data Source plugin was unable to receive the remote server's
                //     response.
                public const int NS_E_HTTP_TEXT_DATACONTAINER_INVALID_SERVER_RESPONSE = -1072884340;

                //
                // Zusammenfassung:
                //     The archive plug-in has reached its quota.
                public const int NS_E_ARCHIVE_REACH_QUOTA = -1072884339;

                //
                // Zusammenfassung:
                //     The archive plug-in aborted because the source was from broadcast.
                public const int NS_E_ARCHIVE_ABORT_DUE_TO_BCAST = -1072884338;

                //
                // Zusammenfassung:
                //     The archive plug-in detected an interrupt in the source.
                public const int NS_E_ARCHIVE_GAP_DETECTED = -1072884337;

                //
                // Zusammenfassung:
                //     The system cannot find the file specified.
                public const int NS_E_AUTHORIZATION_FILE_NOT_FOUND = -1072884336;

                //
                // Zusammenfassung:
                //     The mark-in time should be greater than 0 and less than the mark-out time.
                public const int NS_E_BAD_MARKIN = -1072882856;

                //
                // Zusammenfassung:
                //     The mark-out time should be greater than the mark-in time and less than the file
                //     duration.
                public const int NS_E_BAD_MARKOUT = -1072882855;

                //
                // Zusammenfassung:
                //     No matching media type is found in the source %1.
                public const int NS_E_NOMATCHING_MEDIASOURCE = -1072882854;

                //
                // Zusammenfassung:
                //     The specified source type is not supported.
                public const int NS_E_UNSUPPORTED_SOURCETYPE = -1072882853;

                //
                // Zusammenfassung:
                //     It is not possible to specify more than one audio input.
                public const int NS_E_TOO_MANY_AUDIO = -1072882852;

                //
                // Zusammenfassung:
                //     It is not possible to specify more than two video inputs.
                public const int NS_E_TOO_MANY_VIDEO = -1072882851;

                //
                // Zusammenfassung:
                //     No matching element is found in the list.
                public const int NS_E_NOMATCHING_ELEMENT = -1072882850;

                //
                // Zusammenfassung:
                //     The profile's media types must match the media types defined for the session.
                public const int NS_E_MISMATCHED_MEDIACONTENT = -1072882849;

                //
                // Zusammenfassung:
                //     It is not possible to remove an active source while encoding.
                public const int NS_E_CANNOT_DELETE_ACTIVE_SOURCEGROUP = -1072882848;

                //
                // Zusammenfassung:
                //     It is not possible to open the specified audio capture device because it is currently
                //     in use.
                public const int NS_E_AUDIODEVICE_BUSY = -1072882847;

                //
                // Zusammenfassung:
                //     It is not possible to open the specified audio capture device because an unexpected
                //     error has occurred.
                public const int NS_E_AUDIODEVICE_UNEXPECTED = -1072882846;

                //
                // Zusammenfassung:
                //     The audio capture device does not support the specified audio format.
                public const int NS_E_AUDIODEVICE_BADFORMAT = -1072882845;

                //
                // Zusammenfassung:
                //     It is not possible to open the specified video capture device because it is currently
                //     in use.
                public const int NS_E_VIDEODEVICE_BUSY = -1072882844;

                //
                // Zusammenfassung:
                //     It is not possible to open the specified video capture device because an unexpected
                //     error has occurred.
                public const int NS_E_VIDEODEVICE_UNEXPECTED = -1072882843;

                //
                // Zusammenfassung:
                //     This operation is not allowed while encoding.
                public const int NS_E_INVALIDCALL_WHILE_ENCODER_RUNNING = -1072882842;

                //
                // Zusammenfassung:
                //     No profile is set for the source.
                public const int NS_E_NO_PROFILE_IN_SOURCEGROUP = -1072882841;

                //
                // Zusammenfassung:
                //     The video capture driver returned an unrecoverable error. It is now in an unstable
                //     state.
                public const int NS_E_VIDEODRIVER_UNSTABLE = -1072882840;

                //
                // Zusammenfassung:
                //     It was not possible to start the video device.
                public const int NS_E_VIDCAPSTARTFAILED = -1072882839;

                //
                // Zusammenfassung:
                //     The video source does not support the requested output format or color depth.
                public const int NS_E_VIDSOURCECOMPRESSION = -1072882838;

                //
                // Zusammenfassung:
                //     The video source does not support the requested capture size.
                public const int NS_E_VIDSOURCESIZE = -1072882837;

                //
                // Zusammenfassung:
                //     It was not possible to obtain output information from the video compressor.
                public const int NS_E_ICMQUERYFORMAT = -1072882836;

                //
                // Zusammenfassung:
                //     It was not possible to create a video capture window.
                public const int NS_E_VIDCAPCREATEWINDOW = -1072882835;

                //
                // Zusammenfassung:
                //     There is already a stream active on this video device.
                public const int NS_E_VIDCAPDRVINUSE = -1072882834;

                //
                // Zusammenfassung:
                //     No media format is set in source.
                public const int NS_E_NO_MEDIAFORMAT_IN_SOURCE = -1072882833;

                //
                // Zusammenfassung:
                //     Cannot find a valid output stream from the source.
                public const int NS_E_NO_VALID_OUTPUT_STREAM = -1072882832;

                //
                // Zusammenfassung:
                //     It was not possible to find a valid source plug-in for the specified source.
                public const int NS_E_NO_VALID_SOURCE_PLUGIN = -1072882831;

                //
                // Zusammenfassung:
                //     No source is currently active.
                public const int NS_E_NO_ACTIVE_SOURCEGROUP = -1072882830;

                //
                // Zusammenfassung:
                //     No script stream is set in the current source.
                public const int NS_E_NO_SCRIPT_STREAM = -1072882829;

                //
                // Zusammenfassung:
                //     This operation is not allowed while archiving.
                public const int NS_E_INVALIDCALL_WHILE_ARCHIVAL_RUNNING = -1072882828;

                //
                // Zusammenfassung:
                //     The setting for the maximum packet size is not valid.
                public const int NS_E_INVALIDPACKETSIZE = -1072882827;

                //
                // Zusammenfassung:
                //     The plug-in CLSID specified is not valid.
                public const int NS_E_PLUGIN_CLSID_INVALID = -1072882826;

                //
                // Zusammenfassung:
                //     This archive type is not supported.
                public const int NS_E_UNSUPPORTED_ARCHIVETYPE = -1072882825;

                //
                // Zusammenfassung:
                //     This archive operation is not supported.
                public const int NS_E_UNSUPPORTED_ARCHIVEOPERATION = -1072882824;

                //
                // Zusammenfassung:
                //     The local archive file name was not set.
                public const int NS_E_ARCHIVE_FILENAME_NOTSET = -1072882823;

                //
                // Zusammenfassung:
                //     The source is not yet prepared.
                public const int NS_E_SOURCEGROUP_NOTPREPARED = -1072882822;

                //
                // Zusammenfassung:
                //     Profiles on the sources do not match.
                public const int NS_E_PROFILE_MISMATCH = -1072882821;

                //
                // Zusammenfassung:
                //     The specified crop values are not valid.
                public const int NS_E_INCORRECTCLIPSETTINGS = -1072882820;

                //
                // Zusammenfassung:
                //     No statistics are available at this time.
                public const int NS_E_NOSTATSAVAILABLE = -1072882819;

                //
                // Zusammenfassung:
                //     The encoder is not archiving.
                public const int NS_E_NOTARCHIVING = -1072882818;

                //
                // Zusammenfassung:
                //     This operation is only allowed during encoding.
                public const int NS_E_INVALIDCALL_WHILE_ENCODER_STOPPED = -1072882817;

                //
                // Zusammenfassung:
                //     This SourceGroupCollection doesn't contain any SourceGroups.
                public const int NS_E_NOSOURCEGROUPS = -1072882816;

                //
                // Zusammenfassung:
                //     This source does not have a frame rate of 30 fps. Therefore, it is not possible
                //     to apply the inverse telecine filter to the source.
                public const int NS_E_INVALIDINPUTFPS = -1072882815;

                //
                // Zusammenfassung:
                //     It is not possible to display your source or output video in the Video panel.
                public const int NS_E_NO_DATAVIEW_SUPPORT = -1072882814;

                //
                // Zusammenfassung:
                //     One or more codecs required to open this content could not be found.
                public const int NS_E_CODEC_UNAVAILABLE = -1072882813;

                //
                // Zusammenfassung:
                //     The archive file has the same name as an input file. Change one of the names
                //     before continuing.
                public const int NS_E_ARCHIVE_SAME_AS_INPUT = -1072882812;

                //
                // Zusammenfassung:
                //     The source has not been set up completely.
                public const int NS_E_SOURCE_NOTSPECIFIED = -1072882811;

                //
                // Zusammenfassung:
                //     It is not possible to apply time compression to a broadcast session.
                public const int NS_E_NO_REALTIME_TIMECOMPRESSION = -1072882810;

                //
                // Zusammenfassung:
                //     It is not possible to open this device.
                public const int NS_E_UNSUPPORTED_ENCODER_DEVICE = -1072882809;

                //
                // Zusammenfassung:
                //     It is not possible to start encoding because the display size or color has changed
                //     since the current session was defined. Restore the previous settings or create
                //     a new session.
                public const int NS_E_UNEXPECTED_DISPLAY_SETTINGS = -1072882808;

                //
                // Zusammenfassung:
                //     No audio data has been received for several seconds. Check the audio source and
                //     restart the encoder.
                public const int NS_E_NO_AUDIODATA = -1072882807;

                //
                // Zusammenfassung:
                //     One or all of the specified sources are not working properly. Check that the
                //     sources are configured correctly.
                public const int NS_E_INPUTSOURCE_PROBLEM = -1072882806;

                //
                // Zusammenfassung:
                //     The supplied configuration file is not supported by this version of the encoder.
                public const int NS_E_WME_VERSION_MISMATCH = -1072882805;

                //
                // Zusammenfassung:
                //     It is not possible to use image preprocessing with live encoding.
                public const int NS_E_NO_REALTIME_PREPROCESS = -1072882804;

                //
                // Zusammenfassung:
                //     It is not possible to use two-pass encoding when the source is set to loop.
                public const int NS_E_NO_REPEAT_PREPROCESS = -1072882803;

                //
                // Zusammenfassung:
                //     It is not possible to pause encoding during a broadcast.
                public const int NS_E_CANNOT_PAUSE_LIVEBROADCAST = -1072882802;

                //
                // Zusammenfassung:
                //     A DRM profile has not been set for the current session.
                public const int NS_E_DRM_PROFILE_NOT_SET = -1072882801;

                //
                // Zusammenfassung:
                //     The profile ID is already used by a DRM profile. Specify a different profile
                //     ID.
                public const int NS_E_DUPLICATE_DRMPROFILE = -1072882800;

                //
                // Zusammenfassung:
                //     The setting of the selected device does not support control for playing back
                //     tapes.
                public const int NS_E_INVALID_DEVICE = -1072882799;

                //
                // Zusammenfassung:
                //     You must specify a mixed voice and audio mode in order to use an optimization
                //     definition file.
                public const int NS_E_SPEECHEDL_ON_NON_MIXEDMODE = -1072882798;

                //
                // Zusammenfassung:
                //     The specified password is too long. Type a password with fewer than 8 characters.
                public const int NS_E_DRM_PASSWORD_TOO_LONG = -1072882797;

                //
                // Zusammenfassung:
                //     It is not possible to seek to the specified mark-in point.
                public const int NS_E_DEVCONTROL_FAILED_SEEK = -1072882796;

                //
                // Zusammenfassung:
                //     When you choose to maintain the interlacing in your video, the output video size
                //     must match the input video size.
                public const int NS_E_INTERLACE_REQUIRE_SAMESIZE = -1072882795;

                //
                // Zusammenfassung:
                //     Only one device control plug-in can control a device.
                public const int NS_E_TOO_MANY_DEVICECONTROL = -1072882794;

                //
                // Zusammenfassung:
                //     You must also enable storing content to hard disk temporarily in order to use
                //     two-pass encoding with the input device.
                public const int NS_E_NO_MULTIPASS_FOR_LIVEDEVICE = -1072882793;

                //
                // Zusammenfassung:
                //     An audience is missing from the output stream configuration.
                public const int NS_E_MISSING_AUDIENCE = -1072882792;

                //
                // Zusammenfassung:
                //     All audiences in the output tree must have the same content type.
                public const int NS_E_AUDIENCE_CONTENTTYPE_MISMATCH = -1072882791;

                //
                // Zusammenfassung:
                //     A source index is missing from the output stream configuration.
                public const int NS_E_MISSING_SOURCE_INDEX = -1072882790;

                //
                // Zusammenfassung:
                //     The same source index in different audiences should have the same number of languages.
                public const int NS_E_NUM_LANGUAGE_MISMATCH = -1072882789;

                //
                // Zusammenfassung:
                //     The same source index in different audiences should have the same languages.
                public const int NS_E_LANGUAGE_MISMATCH = -1072882788;

                //
                // Zusammenfassung:
                //     The same source index in different audiences should use the same VBR encoding
                //     mode.
                public const int NS_E_VBRMODE_MISMATCH = -1072882787;

                //
                // Zusammenfassung:
                //     The bit rate index specified is not valid.
                public const int NS_E_INVALID_INPUT_AUDIENCE_INDEX = -1072882786;

                //
                // Zusammenfassung:
                //     The specified language is not valid.
                public const int NS_E_INVALID_INPUT_LANGUAGE = -1072882785;

                //
                // Zusammenfassung:
                //     The specified source type is not valid.
                public const int NS_E_INVALID_INPUT_STREAM = -1072882784;

                //
                // Zusammenfassung:
                //     The source must be a mono channel .wav file.
                public const int NS_E_EXPECT_MONO_WAV_INPUT = -1072882783;

                //
                // Zusammenfassung:
                //     All the source .wav files must have the same format.
                public const int NS_E_INPUT_WAVFORMAT_MISMATCH = -1072882782;

                //
                // Zusammenfassung:
                //     The hard disk being used for temporary storage of content has reached the minimum
                //     allowed disk space. Create more space on the hard disk and restart encoding.
                public const int NS_E_RECORDQ_DISK_FULL = -1072882781;

                //
                // Zusammenfassung:
                //     It is not possible to apply the inverse telecine feature to PAL content.
                public const int NS_E_NO_PAL_INVERSE_TELECINE = -1072882780;

                //
                // Zusammenfassung:
                //     A capture device in the current active source is no longer available.
                public const int NS_E_ACTIVE_SG_DEVICE_DISCONNECTED = -1072882779;

                //
                // Zusammenfassung:
                //     A device used in the current active source for device control is no longer available.
                public const int NS_E_ACTIVE_SG_DEVICE_CONTROL_DISCONNECTED = -1072882778;

                //
                // Zusammenfassung:
                //     No frames have been submitted to the analyzer for analysis.
                public const int NS_E_NO_FRAMES_SUBMITTED_TO_ANALYZER = -1072882777;

                //
                // Zusammenfassung:
                //     The source video does not support time codes.
                public const int NS_E_INPUT_DOESNOT_SUPPORT_SMPTE = -1072882776;

                //
                // Zusammenfassung:
                //     It is not possible to generate a time code when there are multiple sources in
                //     a session.
                public const int NS_E_NO_SMPTE_WITH_MULTIPLE_SOURCEGROUPS = -1072882775;

                //
                // Zusammenfassung:
                //     The voice codec optimization definition file cannot be found or is corrupted.
                public const int NS_E_BAD_CONTENTEDL = -1072882774;

                //
                // Zusammenfassung:
                //     The same source index in different audiences should have the same interlace mode.
                public const int NS_E_INTERLACEMODE_MISMATCH = -1072882773;

                //
                // Zusammenfassung:
                //     The same source index in different audiences should have the same nonsquare pixel
                //     mode.
                public const int NS_E_NONSQUAREPIXELMODE_MISMATCH = -1072882772;

                //
                // Zusammenfassung:
                //     The same source index in different audiences should have the same time code mode.
                public const int NS_E_SMPTEMODE_MISMATCH = -1072882771;

                //
                // Zusammenfassung:
                //     Either the end of the tape has been reached or there is no tape. Check the device
                //     and tape.
                public const int NS_E_END_OF_TAPE = -1072882770;

                //
                // Zusammenfassung:
                //     No audio or video input has been specified.
                public const int NS_E_NO_MEDIA_IN_AUDIENCE = -1072882769;

                //
                // Zusammenfassung:
                //     The profile must contain a bit rate.
                public const int NS_E_NO_AUDIENCES = -1072882768;

                //
                // Zusammenfassung:
                //     You must specify at least one audio stream to be compatible with Windows Media
                //     Player 7.1.
                public const int NS_E_NO_AUDIO_COMPAT = -1072882767;

                //
                // Zusammenfassung:
                //     Using a VBR encoding mode is not compatible with Windows Media Player 7.1.
                public const int NS_E_INVALID_VBR_COMPAT = -1072882766;

                //
                // Zusammenfassung:
                //     You must specify a profile name.
                public const int NS_E_NO_PROFILE_NAME = -1072882765;

                //
                // Zusammenfassung:
                //     It is not possible to use a VBR encoding mode with uncompressed audio or video.
                public const int NS_E_INVALID_VBR_WITH_UNCOMP = -1072882764;

                //
                // Zusammenfassung:
                //     It is not possible to use MBR encoding with VBR encoding.
                public const int NS_E_MULTIPLE_VBR_AUDIENCES = -1072882763;

                //
                // Zusammenfassung:
                //     It is not possible to mix uncompressed and compressed content in a session.
                public const int NS_E_UNCOMP_COMP_COMBINATION = -1072882762;

                //
                // Zusammenfassung:
                //     All audiences must use the same audio codec.
                public const int NS_E_MULTIPLE_AUDIO_CODECS = -1072882761;

                //
                // Zusammenfassung:
                //     All audiences should use the same audio format to be compatible with Windows
                //     Media Player 7.1.
                public const int NS_E_MULTIPLE_AUDIO_FORMATS = -1072882760;

                //
                // Zusammenfassung:
                //     The audio bit rate for an audience with a higher total bit rate must be greater
                //     than one with a lower total bit rate.
                public const int NS_E_AUDIO_BITRATE_STEPDOWN = -1072882759;

                //
                // Zusammenfassung:
                //     The audio peak bit rate setting is not valid.
                public const int NS_E_INVALID_AUDIO_PEAKRATE = -1072882758;

                //
                // Zusammenfassung:
                //     The audio peak bit rate setting must be greater than the audio bit rate setting.
                public const int NS_E_INVALID_AUDIO_PEAKRATE_2 = -1072882757;

                //
                // Zusammenfassung:
                //     The setting for the maximum buffer size for audio is not valid.
                public const int NS_E_INVALID_AUDIO_BUFFERMAX = -1072882756;

                //
                // Zusammenfassung:
                //     All audiences must use the same video codec.
                public const int NS_E_MULTIPLE_VIDEO_CODECS = -1072882755;

                //
                // Zusammenfassung:
                //     All audiences should use the same video size to be compatible with Windows Media
                //     Player 7.1.
                public const int NS_E_MULTIPLE_VIDEO_SIZES = -1072882754;

                //
                // Zusammenfassung:
                //     The video bit rate setting is not valid.
                public const int NS_E_INVALID_VIDEO_BITRATE = -1072882753;

                //
                // Zusammenfassung:
                //     The video bit rate for an audience with a higher total bit rate must be greater
                //     than one with a lower total bit rate.
                public const int NS_E_VIDEO_BITRATE_STEPDOWN = -1072882752;

                //
                // Zusammenfassung:
                //     The video peak bit rate setting is not valid.
                public const int NS_E_INVALID_VIDEO_PEAKRATE = -1072882751;

                //
                // Zusammenfassung:
                //     The video peak bit rate setting must be greater than the video bit rate setting.
                public const int NS_E_INVALID_VIDEO_PEAKRATE_2 = -1072882750;

                //
                // Zusammenfassung:
                //     The video width setting is not valid.
                public const int NS_E_INVALID_VIDEO_WIDTH = -1072882749;

                //
                // Zusammenfassung:
                //     The video height setting is not valid.
                public const int NS_E_INVALID_VIDEO_HEIGHT = -1072882748;

                //
                // Zusammenfassung:
                //     The video frame rate setting is not valid.
                public const int NS_E_INVALID_VIDEO_FPS = -1072882747;

                //
                // Zusammenfassung:
                //     The video key frame setting is not valid.
                public const int NS_E_INVALID_VIDEO_KEYFRAME = -1072882746;

                //
                // Zusammenfassung:
                //     The video image quality setting is not valid.
                public const int NS_E_INVALID_VIDEO_IQUALITY = -1072882745;

                //
                // Zusammenfassung:
                //     The video codec quality setting is not valid.
                public const int NS_E_INVALID_VIDEO_CQUALITY = -1072882744;

                //
                // Zusammenfassung:
                //     The video buffer setting is not valid.
                public const int NS_E_INVALID_VIDEO_BUFFER = -1072882743;

                //
                // Zusammenfassung:
                //     The setting for the maximum buffer size for video is not valid.
                public const int NS_E_INVALID_VIDEO_BUFFERMAX = -1072882742;

                //
                // Zusammenfassung:
                //     The value of the video maximum buffer size setting must be greater than the video
                //     buffer size setting.
                public const int NS_E_INVALID_VIDEO_BUFFERMAX_2 = -1072882741;

                //
                // Zusammenfassung:
                //     The alignment of the video width is not valid.
                public const int NS_E_INVALID_VIDEO_WIDTH_ALIGN = -1072882740;

                //
                // Zusammenfassung:
                //     The alignment of the video height is not valid.
                public const int NS_E_INVALID_VIDEO_HEIGHT_ALIGN = -1072882739;

                //
                // Zusammenfassung:
                //     All bit rates must have the same script bit rate.
                public const int NS_E_MULTIPLE_SCRIPT_BITRATES = -1072882738;

                //
                // Zusammenfassung:
                //     The script bit rate specified is not valid.
                public const int NS_E_INVALID_SCRIPT_BITRATE = -1072882737;

                //
                // Zusammenfassung:
                //     All bit rates must have the same file transfer bit rate.
                public const int NS_E_MULTIPLE_FILE_BITRATES = -1072882736;

                //
                // Zusammenfassung:
                //     The file transfer bit rate is not valid.
                public const int NS_E_INVALID_FILE_BITRATE = -1072882735;

                //
                // Zusammenfassung:
                //     All audiences in a profile should either be same as input or have video width
                //     and height specified.
                public const int NS_E_SAME_AS_INPUT_COMBINATION = -1072882734;

                //
                // Zusammenfassung:
                //     This source type does not support looping.
                public const int NS_E_SOURCE_CANNOT_LOOP = -1072882733;

                //
                // Zusammenfassung:
                //     The fold-down value needs to be between -144 and 0.
                public const int NS_E_INVALID_FOLDDOWN_COEFFICIENTS = -1072882732;

                //
                // Zusammenfassung:
                //     The specified DRM profile does not exist in the system.
                public const int NS_E_DRMPROFILE_NOTFOUND = -1072882731;

                //
                // Zusammenfassung:
                //     The specified time code is not valid.
                public const int NS_E_INVALID_TIMECODE = -1072882730;

                //
                // Zusammenfassung:
                //     It is not possible to apply time compression to a video-only session.
                public const int NS_E_NO_AUDIO_TIMECOMPRESSION = -1072882729;

                //
                // Zusammenfassung:
                //     It is not possible to apply time compression to a session that is using two-pass
                //     encoding.
                public const int NS_E_NO_TWOPASS_TIMECOMPRESSION = -1072882728;

                //
                // Zusammenfassung:
                //     It is not possible to generate a time code for an audio-only session.
                public const int NS_E_TIMECODE_REQUIRES_VIDEOSTREAM = -1072882727;

                //
                // Zusammenfassung:
                //     It is not possible to generate a time code when you are encoding content at multiple
                //     bit rates.
                public const int NS_E_NO_MBR_WITH_TIMECODE = -1072882726;

                //
                // Zusammenfassung:
                //     The video codec selected does not support maintaining interlacing in video.
                public const int NS_E_INVALID_INTERLACEMODE = -1072882725;

                //
                // Zusammenfassung:
                //     Maintaining interlacing in video is not compatible with Windows Media Player
                //     7.1.
                public const int NS_E_INVALID_INTERLACE_COMPAT = -1072882724;

                //
                // Zusammenfassung:
                //     Allowing nonsquare pixel output is not compatible with Windows Media Player 7.1.
                public const int NS_E_INVALID_NONSQUAREPIXEL_COMPAT = -1072882723;

                //
                // Zusammenfassung:
                //     Only capture devices can be used with device control.
                public const int NS_E_INVALID_SOURCE_WITH_DEVICE_CONTROL = -1072882722;

                //
                // Zusammenfassung:
                //     It is not possible to generate the stream format file if you are using quality-based
                //     VBR encoding for the audio or video stream. Instead use the Windows Media file
                //     generated after encoding to create the announcement file.
                public const int NS_E_CANNOT_GENERATE_BROADCAST_INFO_FOR_QUALITYVBR = -1072882721;

                //
                // Zusammenfassung:
                //     It is not possible to create a DRM profile because the maximum number of profiles
                //     has been reached. You must delete some DRM profiles before creating new ones.
                public const int NS_E_EXCEED_MAX_DRM_PROFILE_LIMIT = -1072882720;

                //
                // Zusammenfassung:
                //     The device is in an unstable state. Check that the device is functioning properly
                //     and a tape is in place.
                public const int NS_E_DEVICECONTROL_UNSTABLE = -1072882719;

                //
                // Zusammenfassung:
                //     The pixel aspect ratio value must be between 1 and 255.
                public const int NS_E_INVALID_PIXEL_ASPECT_RATIO = -1072882718;

                //
                // Zusammenfassung:
                //     All streams with different languages in the same audience must have same properties.
                public const int NS_E_AUDIENCE__LANGUAGE_CONTENTTYPE_MISMATCH = -1072882717;

                //
                // Zusammenfassung:
                //     The profile must contain at least one audio or video stream.
                public const int NS_E_INVALID_PROFILE_CONTENTTYPE = -1072882716;

                //
                // Zusammenfassung:
                //     The transform plug-in could not be found.
                public const int NS_E_TRANSFORM_PLUGIN_NOT_FOUND = -1072882715;

                //
                // Zusammenfassung:
                //     The transform plug-in is not valid. It might be damaged or you might not have
                //     the required permissions to access the plug-in.
                public const int NS_E_TRANSFORM_PLUGIN_INVALID = -1072882714;

                //
                // Zusammenfassung:
                //     To use two-pass encoding, you must enable device control and setup an edit decision
                //     list (EDL) that has at least one entry.
                public const int NS_E_EDL_REQUIRED_FOR_DEVICE_MULTIPASS = -1072882713;

                //
                // Zusammenfassung:
                //     When you choose to maintain the interlacing in your video, the output video size
                //     must be a multiple of 4.
                public const int NS_E_INVALID_VIDEO_WIDTH_FOR_INTERLACED_ENCODING = -1072882712;

                //
                // Zusammenfassung:
                //     Markin/Markout is unsupported with this source type.
                public const int NS_E_MARKIN_UNSUPPORTED = -1072882711;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact product
                //     support for this application.
                public const int NS_E_DRM_INVALID_APPLICATION = -1072879855;

                //
                // Zusammenfassung:
                //     License storage is not working. Contact Microsoft product support.
                public const int NS_E_DRM_LICENSE_STORE_ERROR = -1072879854;

                //
                // Zusammenfassung:
                //     Secure storage is not working. Contact Microsoft product support.
                public const int NS_E_DRM_SECURE_STORE_ERROR = -1072879853;

                //
                // Zusammenfassung:
                //     License acquisition did not work. Acquire a new license or contact the content
                //     provider for further assistance.
                public const int NS_E_DRM_LICENSE_STORE_SAVE_ERROR = -1072879852;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_SECURE_STORE_UNLOCK_ERROR = -1072879851;

                //
                // Zusammenfassung:
                //     The media file is corrupted. Contact the content provider to get a new file.
                public const int NS_E_DRM_INVALID_CONTENT = -1072879850;

                //
                // Zusammenfassung:
                //     The license is corrupted. Acquire a new license.
                public const int NS_E_DRM_UNABLE_TO_OPEN_LICENSE = -1072879849;

                //
                // Zusammenfassung:
                //     The license is corrupted or invalid. Acquire a new license
                public const int NS_E_DRM_INVALID_LICENSE = -1072879848;

                //
                // Zusammenfassung:
                //     Licenses cannot be copied from one computer to another. Use License Management
                //     to transfer licenses, or get a new license for the media file.
                public const int NS_E_DRM_INVALID_MACHINE = -1072879847;

                //
                // Zusammenfassung:
                //     License storage is not working. Contact Microsoft product support.
                public const int NS_E_DRM_ENUM_LICENSE_FAILED = -1072879845;

                //
                // Zusammenfassung:
                //     The media file is corrupted. Contact the content provider to get a new file.
                public const int NS_E_DRM_INVALID_LICENSE_REQUEST = -1072879844;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_INITIALIZE = -1072879843;

                //
                // Zusammenfassung:
                //     The license could not be acquired. Try again later.
                public const int NS_E_DRM_UNABLE_TO_ACQUIRE_LICENSE = -1072879842;

                //
                // Zusammenfassung:
                //     License acquisition did not work. Acquire a new license or contact the content
                //     provider for further assistance.
                public const int NS_E_DRM_INVALID_LICENSE_ACQUIRED = -1072879841;

                //
                // Zusammenfassung:
                //     The requested operation cannot be performed on this file.
                public const int NS_E_DRM_NO_RIGHTS = -1072879840;

                //
                // Zusammenfassung:
                //     The requested action cannot be performed because a problem occurred with the
                //     Windows Media Digital Rights Management (DRM) components on your computer.
                public const int NS_E_DRM_KEY_ERROR = -1072879839;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_ENCRYPT_ERROR = -1072879838;

                //
                // Zusammenfassung:
                //     The media file is corrupted. Contact the content provider to get a new file.
                public const int NS_E_DRM_DECRYPT_ERROR = -1072879837;

                //
                // Zusammenfassung:
                //     The license is corrupted. Acquire a new license.
                public const int NS_E_DRM_LICENSE_INVALID_XML = -1072879835;

                //
                // Zusammenfassung:
                //     A security upgrade is required to perform the operation on this media file.
                public const int NS_E_DRM_NEEDS_INDIVIDUALIZATION = -1072879832;

                //
                // Zusammenfassung:
                //     You already have the latest security components. No upgrade is necessary at this
                //     time.
                public const int NS_E_DRM_ALREADY_INDIVIDUALIZED = -1072879831;

                //
                // Zusammenfassung:
                //     The application cannot perform this action. Contact product support for this
                //     application.
                public const int NS_E_DRM_ACTION_NOT_QUERIED = -1072879830;

                //
                // Zusammenfassung:
                //     You cannot begin a new license acquisition process until the current one has
                //     been completed.
                public const int NS_E_DRM_ACQUIRING_LICENSE = -1072879829;

                //
                // Zusammenfassung:
                //     You cannot begin a new security upgrade until the current one has been completed.
                public const int NS_E_DRM_INDIVIDUALIZING = -1072879828;

                //
                // Zusammenfassung:
                //     Failure in Backup-Restore.
                public const int NS_E_BACKUP_RESTORE_FAILURE = -1072879827;

                //
                // Zusammenfassung:
                //     Bad Request ID in Backup-Restore.
                public const int NS_E_BACKUP_RESTORE_BAD_REQUEST_ID = -1072879826;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_PARAMETERS_MISMATCHED = -1072879825;

                //
                // Zusammenfassung:
                //     A license cannot be created for this media file. Reinstall the application.
                public const int NS_E_DRM_UNABLE_TO_CREATE_LICENSE_OBJECT = -1072879824;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_CREATE_INDI_OBJECT = -1072879823;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_CREATE_ENCRYPT_OBJECT = -1072879822;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_CREATE_DECRYPT_OBJECT = -1072879821;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_CREATE_PROPERTIES_OBJECT = -1072879820;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_CREATE_BACKUP_OBJECT = -1072879819;

                //
                // Zusammenfassung:
                //     The security upgrade failed. Try again later.
                public const int NS_E_DRM_INDIVIDUALIZE_ERROR = -1072879818;

                //
                // Zusammenfassung:
                //     License storage is not working. Contact Microsoft product support.
                public const int NS_E_DRM_LICENSE_OPEN_ERROR = -1072879817;

                //
                // Zusammenfassung:
                //     License storage is not working. Contact Microsoft product support.
                public const int NS_E_DRM_LICENSE_CLOSE_ERROR = -1072879816;

                //
                // Zusammenfassung:
                //     License storage is not working. Contact Microsoft product support.
                public const int NS_E_DRM_GET_LICENSE_ERROR = -1072879815;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_QUERY_ERROR = -1072879814;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact product
                //     support for this application.
                public const int NS_E_DRM_REPORT_ERROR = -1072879813;

                //
                // Zusammenfassung:
                //     License storage is not working. Contact Microsoft product support.
                public const int NS_E_DRM_GET_LICENSESTRING_ERROR = -1072879812;

                //
                // Zusammenfassung:
                //     The media file is corrupted. Contact the content provider to get a new file.
                public const int NS_E_DRM_GET_CONTENTSTRING_ERROR = -1072879811;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Try again
                //     later.
                public const int NS_E_DRM_MONITOR_ERROR = -1072879810;

                //
                // Zusammenfassung:
                //     The application has made an invalid call to the Digital Rights Management component.
                //     Contact product support for this application.
                public const int NS_E_DRM_UNABLE_TO_SET_PARAMETER = -1072879809;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_INVALID_APPDATA = -1072879808;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact product
                //     support for this application.
                public const int NS_E_DRM_INVALID_APPDATA_VERSION = -1072879807;

                //
                // Zusammenfassung:
                //     Licenses are already backed up in this location.
                public const int NS_E_DRM_BACKUP_EXISTS = -1072879806;

                //
                // Zusammenfassung:
                //     One or more backed-up licenses are missing or corrupt.
                public const int NS_E_DRM_BACKUP_CORRUPT = -1072879805;

                //
                // Zusammenfassung:
                //     You cannot begin a new backup process until the current process has been completed.
                public const int NS_E_DRM_BACKUPRESTORE_BUSY = -1072879804;

                //
                // Zusammenfassung:
                //     Bad Data sent to Backup-Restore.
                public const int NS_E_BACKUP_RESTORE_BAD_DATA = -1072879803;

                //
                // Zusammenfassung:
                //     The license is invalid. Contact the content provider for further assistance.
                public const int NS_E_DRM_LICENSE_UNUSABLE = -1072879800;

                //
                // Zusammenfassung:
                //     A required property was not set by the application. Contact product support for
                //     this application.
                public const int NS_E_DRM_INVALID_PROPERTY = -1072879799;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component of this application.
                //     Try to acquire a license again.
                public const int NS_E_DRM_SECURE_STORE_NOT_FOUND = -1072879798;

                //
                // Zusammenfassung:
                //     A license cannot be found for this media file. Use License Management to transfer
                //     a license for this file from the original computer, or acquire a new license.
                public const int NS_E_DRM_CACHED_CONTENT_ERROR = -1072879797;

                //
                // Zusammenfassung:
                //     A problem occurred during the security upgrade. Try again later.
                public const int NS_E_DRM_INDIVIDUALIZATION_INCOMPLETE = -1072879796;

                //
                // Zusammenfassung:
                //     Certified driver components are required to play this media file. Contact Windows
                //     Update to see whether updated drivers are available for your hardware.
                public const int NS_E_DRM_DRIVER_AUTH_FAILURE = -1072879795;

                //
                // Zusammenfassung:
                //     One or more of the Secure Audio Path components were not found or an entry point
                //     in those components was not found.
                public const int NS_E_DRM_NEED_UPGRADE_MSSAP = -1072879794;

                //
                // Zusammenfassung:
                //     Status message: Reopen the file.
                public const int NS_E_DRM_REOPEN_CONTENT = -1072879793;

                //
                // Zusammenfassung:
                //     Certain driver functionality is required to play this media file. Contact Windows
                //     Update to see whether updated drivers are available for your hardware.
                public const int NS_E_DRM_DRIVER_DIGIOUT_FAILURE = -1072879792;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_INVALID_SECURESTORE_PASSWORD = -1072879791;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_APPCERT_REVOKED = -1072879790;

                //
                // Zusammenfassung:
                //     You cannot restore your license(s).
                public const int NS_E_DRM_RESTORE_FRAUD = -1072879789;

                //
                // Zusammenfassung:
                //     The licenses for your media files are corrupted. Contact Microsoft product support.
                public const int NS_E_DRM_HARDWARE_INCONSISTENT = -1072879788;

                //
                // Zusammenfassung:
                //     To transfer this media file, you must upgrade the application.
                public const int NS_E_DRM_SDMI_TRIGGER = -1072879787;

                //
                // Zusammenfassung:
                //     You cannot make any more copies of this media file.
                public const int NS_E_DRM_SDMI_NOMORECOPIES = -1072879786;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_CREATE_HEADER_OBJECT = -1072879785;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_CREATE_KEYS_OBJECT = -1072879784;

                //
                // Zusammenfassung:
                //     Unable to obtain license.
                public const int NS_E_DRM_LICENSE_NOTACQUIRED = -1072879783;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_CREATE_CODING_OBJECT = -1072879782;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_CREATE_STATE_DATA_OBJECT = -1072879781;

                //
                // Zusammenfassung:
                //     The buffer supplied is not sufficient.
                public const int NS_E_DRM_BUFFER_TOO_SMALL = -1072879780;

                //
                // Zusammenfassung:
                //     The property requested is not supported.
                public const int NS_E_DRM_UNSUPPORTED_PROPERTY = -1072879779;

                //
                // Zusammenfassung:
                //     The specified server cannot perform the requested operation.
                public const int NS_E_DRM_ERROR_BAD_NET_RESP = -1072879778;

                //
                // Zusammenfassung:
                //     Some of the licenses could not be stored.
                public const int NS_E_DRM_STORE_NOTALLSTORED = -1072879777;

                //
                // Zusammenfassung:
                //     The Digital Rights Management security upgrade component could not be validated.
                //     Contact Microsoft product support.
                public const int NS_E_DRM_SECURITY_COMPONENT_SIGNATURE_INVALID = -1072879776;

                //
                // Zusammenfassung:
                //     Invalid or corrupt data was encountered.
                public const int NS_E_DRM_INVALID_DATA = -1072879775;

                //
                // Zusammenfassung:
                //     The Windows Media Digital Rights Management system cannot perform the requested
                //     action because your computer or network administrator has enabled the group policy
                //     Prevent Windows Media DRM Internet Access. For assistance, contact your administrator.
                public const int NS_E_DRM_POLICY_DISABLE_ONLINE = -1072879774;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_CREATE_AUTHENTICATION_OBJECT = -1072879773;

                //
                // Zusammenfassung:
                //     Not all of the necessary properties for DRM have been set.
                public const int NS_E_DRM_NOT_CONFIGURED = -1072879772;

                //
                // Zusammenfassung:
                //     The portable device does not have the security required to copy protected files
                //     to it. To obtain the additional security, try to copy the file to your portable
                //     device again. When a message appears, click OK.
                public const int NS_E_DRM_DEVICE_ACTIVATION_CANCELED = -1072879771;

                //
                // Zusammenfassung:
                //     Too many resets in Backup-Restore.
                public const int NS_E_BACKUP_RESTORE_TOO_MANY_RESETS = -1072879770;

                //
                // Zusammenfassung:
                //     Running this process under a debugger while using DRM content is not allowed.
                public const int NS_E_DRM_DEBUGGING_NOT_ALLOWED = -1072879769;

                //
                // Zusammenfassung:
                //     The user canceled the DRM operation.
                public const int NS_E_DRM_OPERATION_CANCELED = -1072879768;

                //
                // Zusammenfassung:
                //     The license you are using has assocaited output restrictions. This license is
                //     unusable until these restrictions are queried.
                public const int NS_E_DRM_RESTRICTIONS_NOT_RETRIEVED = -1072879767;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_CREATE_PLAYLIST_OBJECT = -1072879766;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_CREATE_PLAYLIST_BURN_OBJECT = -1072879765;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_CREATE_DEVICE_REGISTRATION_OBJECT = -1072879764;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_CREATE_METERING_OBJECT = -1072879763;

                //
                // Zusammenfassung:
                //     The specified track has exceeded it's specified playlist burn limit in this playlist.
                public const int NS_E_DRM_TRACK_EXCEEDED_PLAYLIST_RESTICTION = -1072879760;

                //
                // Zusammenfassung:
                //     The specified track has exceeded it's track burn limit.
                public const int NS_E_DRM_TRACK_EXCEEDED_TRACKBURN_RESTRICTION = -1072879759;

                //
                // Zusammenfassung:
                //     A problem has occurred in obtaining the device's certificate. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_GET_DEVICE_CERT = -1072879758;

                //
                // Zusammenfassung:
                //     A problem has occurred in obtaining the device's secure clock. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_GET_SECURE_CLOCK = -1072879757;

                //
                // Zusammenfassung:
                //     A problem has occurred in setting the device's secure clock. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_SET_SECURE_CLOCK = -1072879756;

                //
                // Zusammenfassung:
                //     A problem has occurred in obtaining the secure clock from server. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_GET_SECURE_CLOCK_FROM_SERVER = -1072879755;

                //
                // Zusammenfassung:
                //     This content requires the metering policy to be enabled.
                public const int NS_E_DRM_POLICY_METERING_DISABLED = -1072879754;

                //
                // Zusammenfassung:
                //     Transfer of chained licenses unsupported.
                public const int NS_E_DRM_TRANSFER_CHAINED_LICENSES_UNSUPPORTED = -1072879753;

                //
                // Zusammenfassung:
                //     The Digital Rights Management component is not installed properly. Reinstall
                //     the Player.
                public const int NS_E_DRM_SDK_VERSIONMISMATCH = -1072879752;

                //
                // Zusammenfassung:
                //     The file could not be transferred because the device clock is not set.
                public const int NS_E_DRM_LIC_NEEDS_DEVICE_CLOCK_SET = -1072879751;

                //
                // Zusammenfassung:
                //     The content header is missing an acquisition URL.
                public const int NS_E_LICENSE_HEADER_MISSING_URL = -1072879750;

                //
                // Zusammenfassung:
                //     The current attached device does not support WMDRM.
                public const int NS_E_DEVICE_NOT_WMDRM_DEVICE = -1072879749;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_INVALID_APPCERT = -1072879748;

                //
                // Zusammenfassung:
                //     The client application has been forcefully terminated during a DRM petition.
                public const int NS_E_DRM_PROTOCOL_FORCEFUL_TERMINATION_ON_PETITION = -1072879747;

                //
                // Zusammenfassung:
                //     The client application has been forcefully terminated during a DRM challenge.
                public const int NS_E_DRM_PROTOCOL_FORCEFUL_TERMINATION_ON_CHALLENGE = -1072879746;

                //
                // Zusammenfassung:
                //     Secure storage protection error. Restore your licenses from a previous backup
                //     and try again.
                public const int NS_E_DRM_CHECKPOINT_FAILED = -1072879745;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management root of trust. Contact
                //     Microsoft product support.
                public const int NS_E_DRM_BB_UNABLE_TO_INITIALIZE = -1072879744;

                //
                // Zusammenfassung:
                //     A problem has occurred in retrieving the Digital Rights Management machine identification.
                //     Contact Microsoft product support.
                public const int NS_E_DRM_UNABLE_TO_LOAD_HARDWARE_ID = -1072879743;

                //
                // Zusammenfassung:
                //     A problem has occurred in opening the Digital Rights Management data storage
                //     file. Contact Microsoft product.
                public const int NS_E_DRM_UNABLE_TO_OPEN_DATA_STORE = -1072879742;

                //
                // Zusammenfassung:
                //     The Digital Rights Management data storage is not functioning properly. Contact
                //     Microsoft product support.
                public const int NS_E_DRM_DATASTORE_CORRUPT = -1072879741;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_CREATE_INMEMORYSTORE_OBJECT = -1072879740;

                //
                // Zusammenfassung:
                //     A secured library is required to access the requested functionality.
                public const int NS_E_DRM_STUBLIB_REQUIRED = -1072879739;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_UNABLE_TO_CREATE_CERTIFICATE_OBJECT = -1072879738;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component during license
                //     migration. Contact Microsoft product support.
                public const int NS_E_DRM_MIGRATION_TARGET_NOT_ONLINE = -1072879737;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component during license
                //     migration. Contact Microsoft product support.
                public const int NS_E_DRM_INVALID_MIGRATION_IMAGE = -1072879736;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component during license
                //     migration. Contact Microsoft product support.
                public const int NS_E_DRM_MIGRATION_TARGET_STATES_CORRUPTED = -1072879735;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component during license
                //     migration. Contact Microsoft product support.
                public const int NS_E_DRM_MIGRATION_IMPORTER_NOT_AVAILABLE = -1072879734;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component during license
                //     migration. Contact Microsoft product support.
                public const int NS_DRM_E_MIGRATION_UPGRADE_WITH_DIFF_SID = -1072879733;

                //
                // Zusammenfassung:
                //     The Digital Rights Management component is in use during license migration. Contact
                //     Microsoft product support.
                public const int NS_DRM_E_MIGRATION_SOURCE_MACHINE_IN_USE = -1072879732;

                //
                // Zusammenfassung:
                //     Licenses are being migrated to a machine running XP or downlevel OS. This operation
                //     can only be performed on Windows Vista or a later OS. Contact Microsoft product
                //     support.
                public const int NS_DRM_E_MIGRATION_TARGET_MACHINE_LESS_THAN_LH = -1072879731;

                //
                // Zusammenfassung:
                //     Migration Image already exists. Contact Microsoft product support.
                public const int NS_DRM_E_MIGRATION_IMAGE_ALREADY_EXISTS = -1072879730;

                //
                // Zusammenfassung:
                //     The requested action cannot be performed because a hardware configuration change
                //     has been detected by the Windows Media Digital Rights Management (DRM) components
                //     on your computer.
                public const int NS_E_DRM_HARDWAREID_MISMATCH = -1072879729;

                //
                // Zusammenfassung:
                //     The wrong stublib has been linked to an application or DLL using drmv2clt.dll.
                public const int NS_E_INVALID_DRMV2CLT_STUBLIB = -1072879728;

                //
                // Zusammenfassung:
                //     The legacy V2 data being imported is invalid.
                public const int NS_E_DRM_MIGRATION_INVALID_LEGACYV2_DATA = -1072879727;

                //
                // Zusammenfassung:
                //     The license being imported already exists.
                public const int NS_E_DRM_MIGRATION_LICENSE_ALREADY_EXISTS = -1072879726;

                //
                // Zusammenfassung:
                //     The password of the Legacy V2 SST entry being imported is incorrect.
                public const int NS_E_DRM_MIGRATION_INVALID_LEGACYV2_SST_PASSWORD = -1072879725;

                //
                // Zusammenfassung:
                //     Migration is not supported by the plugin.
                public const int NS_E_DRM_MIGRATION_NOT_SUPPORTED = -1072879724;

                //
                // Zusammenfassung:
                //     A migration importer cannot be created for this media file. Reinstall the application.
                public const int NS_E_DRM_UNABLE_TO_CREATE_MIGRATION_IMPORTER_OBJECT = -1072879723;

                //
                // Zusammenfassung:
                //     The requested action cannot be performed because a problem occurred with the
                //     Windows Media Digital Rights Management (DRM) components on your computer.
                public const int NS_E_DRM_CHECKPOINT_MISMATCH = -1072879722;

                //
                // Zusammenfassung:
                //     The requested action cannot be performed because a problem occurred with the
                //     Windows Media Digital Rights Management (DRM) components on your computer.
                public const int NS_E_DRM_CHECKPOINT_CORRUPT = -1072879721;

                //
                // Zusammenfassung:
                //     The requested action cannot be performed because a problem occurred with the
                //     Windows Media Digital Rights Management (DRM) components on your computer.
                public const int NS_E_REG_FLUSH_FAILURE = -1072879720;

                //
                // Zusammenfassung:
                //     The requested action cannot be performed because a problem occurred with the
                //     Windows Media Digital Rights Management (DRM) components on your computer.
                public const int NS_E_HDS_KEY_MISMATCH = -1072879719;

                //
                // Zusammenfassung:
                //     Migration was canceled by the user.
                public const int NS_E_DRM_MIGRATION_OPERATION_CANCELLED = -1072879718;

                //
                // Zusammenfassung:
                //     Migration object is already in use and cannot be called until the current operation
                //     completes.
                public const int NS_E_DRM_MIGRATION_OBJECT_IN_USE = -1072879717;

                //
                // Zusammenfassung:
                //     The content header does not comply with DRM requirements and cannot be used.
                public const int NS_E_DRM_MALFORMED_CONTENT_HEADER = -1072879716;

                //
                // Zusammenfassung:
                //     The license for this file has expired and is no longer valid. Contact your content
                //     provider for further assistance.
                public const int NS_E_DRM_LICENSE_EXPIRED = -1072879656;

                //
                // Zusammenfassung:
                //     The license for this file is not valid yet, but will be at a future date.
                public const int NS_E_DRM_LICENSE_NOTENABLED = -1072879655;

                //
                // Zusammenfassung:
                //     The license for this file requires a higher level of security than the player
                //     you are currently using has. Try using a different player or download a newer
                //     version of your current player.
                public const int NS_E_DRM_LICENSE_APPSECLOW = -1072879654;

                //
                // Zusammenfassung:
                //     The license cannot be stored as it requires security upgrade of Digital Rights
                //     Management component.
                public const int NS_E_DRM_STORE_NEEDINDI = -1072879653;

                //
                // Zusammenfassung:
                //     Your machine does not meet the requirements for storing the license.
                public const int NS_E_DRM_STORE_NOTALLOWED = -1072879652;

                //
                // Zusammenfassung:
                //     The license for this file requires an upgraded version of your player or a different
                //     player.
                public const int NS_E_DRM_LICENSE_APP_NOTALLOWED = -1072879651;

                //
                // Zusammenfassung:
                //     The license server's certificate expired. Make sure your system clock is set
                //     correctly. Contact your content provider for further assistance.
                public const int NS_E_DRM_LICENSE_CERT_EXPIRED = -1072879649;

                //
                // Zusammenfassung:
                //     The license for this file requires a higher level of security than the player
                //     you are currently using has. Try using a different player or download a newer
                //     version of your current player.
                public const int NS_E_DRM_LICENSE_SECLOW = -1072879648;

                //
                // Zusammenfassung:
                //     The content owner for the license you just acquired is no longer supporting their
                //     content. Contact the content owner for a newer version of the content.
                public const int NS_E_DRM_LICENSE_CONTENT_REVOKED = -1072879647;

                //
                // Zusammenfassung:
                //     The content owner for the license you just acquired requires your device to register
                //     to the current machine.
                public const int NS_E_DRM_DEVICE_NOT_REGISTERED = -1072879646;

                //
                // Zusammenfassung:
                //     The license for this file requires a feature that is not supported in your current
                //     player or operating system. You can try with newer version of your current player
                //     or contact your content provider for further assistance.
                public const int NS_E_DRM_LICENSE_NOSAP = -1072879606;

                //
                // Zusammenfassung:
                //     The license for this file requires a feature that is not supported in your current
                //     player or operating system. You can try with newer version of your current player
                //     or contact your content provider for further assistance.
                public const int NS_E_DRM_LICENSE_NOSVP = -1072879605;

                //
                // Zusammenfassung:
                //     The license for this file requires Windows Driver Model (WDM) audio drivers.
                //     Contact your sound card manufacturer for further assistance.
                public const int NS_E_DRM_LICENSE_NOWDM = -1072879604;

                //
                // Zusammenfassung:
                //     The license for this file requires a higher level of security than the player
                //     you are currently using has. Try using a different player or download a newer
                //     version of your current player.
                public const int NS_E_DRM_LICENSE_NOTRUSTEDCODEC = -1072879603;

                //
                // Zusammenfassung:
                //     The license for this file is not supported by your current player. You can try
                //     with newer version of your current player or contact your content provider for
                //     further assistance.
                public const int NS_E_DRM_SOURCEID_NOT_SUPPORTED = -1072879602;

                //
                // Zusammenfassung:
                //     An updated version of your media player is required to play the selected content.
                public const int NS_E_DRM_NEEDS_UPGRADE_TEMPFILE = -1072879555;

                //
                // Zusammenfassung:
                //     A new version of the Digital Rights Management component is required. Contact
                //     product support for this application to get the latest version.
                public const int NS_E_DRM_NEED_UPGRADE_PD = -1072879554;

                //
                // Zusammenfassung:
                //     Failed to either create or verify the content header.
                public const int NS_E_DRM_SIGNATURE_FAILURE = -1072879553;

                //
                // Zusammenfassung:
                //     Could not read the necessary information from the system registry.
                public const int NS_E_DRM_LICENSE_SERVER_INFO_MISSING = -1072879552;

                //
                // Zusammenfassung:
                //     The DRM subsystem is currently locked by another application or user. Try again
                //     later.
                public const int NS_E_DRM_BUSY = -1072879551;

                //
                // Zusammenfassung:
                //     There are too many target devices registered on the portable media.
                public const int NS_E_DRM_PD_TOO_MANY_DEVICES = -1072879550;

                //
                // Zusammenfassung:
                //     The security upgrade cannot be completed because the allowed number of daily
                //     upgrades has been exceeded. Try again tomorrow.
                public const int NS_E_DRM_INDIV_FRAUD = -1072879549;

                //
                // Zusammenfassung:
                //     The security upgrade cannot be completed because the server is unable to perform
                //     the operation. Try again later.
                public const int NS_E_DRM_INDIV_NO_CABS = -1072879548;

                //
                // Zusammenfassung:
                //     The security upgrade cannot be performed because the server is not available.
                //     Try again later.
                public const int NS_E_DRM_INDIV_SERVICE_UNAVAILABLE = -1072879547;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot restore your licenses because the server is not available.
                //     Try again later.
                public const int NS_E_DRM_RESTORE_SERVICE_UNAVAILABLE = -1072879546;

                //
                // Zusammenfassung:
                //     Windows Media Player cannot play the protected file. Verify that your computer's
                //     date is set correctly. If it is correct, on the Help menu, click Check for Player
                //     Updates to install the latest version of the Player.
                public const int NS_E_DRM_CLIENT_CODE_EXPIRED = -1072879545;

                //
                // Zusammenfassung:
                //     The chained license cannot be created because the referenced uplink license does
                //     not exist.
                public const int NS_E_DRM_NO_UPLINK_LICENSE = -1072879544;

                //
                // Zusammenfassung:
                //     The specified KID is invalid.
                public const int NS_E_DRM_INVALID_KID = -1072879543;

                //
                // Zusammenfassung:
                //     License initialization did not work. Contact Microsoft product support.
                public const int NS_E_DRM_LICENSE_INITIALIZATION_ERROR = -1072879542;

                //
                // Zusammenfassung:
                //     The uplink license of a chained license cannot itself be a chained license.
                public const int NS_E_DRM_CHAIN_TOO_LONG = -1072879540;

                //
                // Zusammenfassung:
                //     The specified encryption algorithm is unsupported.
                public const int NS_E_DRM_UNSUPPORTED_ALGORITHM = -1072879539;

                //
                // Zusammenfassung:
                //     License deletion did not work. Contact Microsoft product support.
                public const int NS_E_DRM_LICENSE_DELETION_ERROR = -1072879538;

                //
                // Zusammenfassung:
                //     The client's certificate is corrupted or the signature cannot be verified.
                public const int NS_E_DRM_INVALID_CERTIFICATE = -1072879456;

                //
                // Zusammenfassung:
                //     The client's certificate has been revoked.
                public const int NS_E_DRM_CERTIFICATE_REVOKED = -1072879455;

                //
                // Zusammenfassung:
                //     There is no license available for the requested action.
                public const int NS_E_DRM_LICENSE_UNAVAILABLE = -1072879454;

                //
                // Zusammenfassung:
                //     The maximum number of devices in use has been reached. Unable to open additional
                //     devices.
                public const int NS_E_DRM_DEVICE_LIMIT_REACHED = -1072879453;

                //
                // Zusammenfassung:
                //     The proximity detection procedure could not confirm that the receiver is near
                //     the transmitter in the network.
                public const int NS_E_DRM_UNABLE_TO_VERIFY_PROXIMITY = -1072879452;

                //
                // Zusammenfassung:
                //     The client must be registered before executing the intended operation.
                public const int NS_E_DRM_MUST_REGISTER = -1072879451;

                //
                // Zusammenfassung:
                //     The client must be approved before executing the intended operation.
                public const int NS_E_DRM_MUST_APPROVE = -1072879450;

                //
                // Zusammenfassung:
                //     The client must be revalidated before executing the intended operation.
                public const int NS_E_DRM_MUST_REVALIDATE = -1072879449;

                //
                // Zusammenfassung:
                //     The response to the proximity detection challenge is invalid.
                public const int NS_E_DRM_INVALID_PROXIMITY_RESPONSE = -1072879448;

                //
                // Zusammenfassung:
                //     The requested session is invalid.
                public const int NS_E_DRM_INVALID_SESSION = -1072879447;

                //
                // Zusammenfassung:
                //     The device must be opened before it can be used to receive content.
                public const int NS_E_DRM_DEVICE_NOT_OPEN = -1072879446;

                //
                // Zusammenfassung:
                //     Device registration failed because the device is already registered.
                public const int NS_E_DRM_DEVICE_ALREADY_REGISTERED = -1072879445;

                //
                // Zusammenfassung:
                //     Unsupported WMDRM-ND protocol version.
                public const int NS_E_DRM_UNSUPPORTED_PROTOCOL_VERSION = -1072879444;

                //
                // Zusammenfassung:
                //     The requested action is not supported.
                public const int NS_E_DRM_UNSUPPORTED_ACTION = -1072879443;

                //
                // Zusammenfassung:
                //     The certificate does not have an adequate security level for the requested action.
                public const int NS_E_DRM_CERTIFICATE_SECURITY_LEVEL_INADEQUATE = -1072879442;

                //
                // Zusammenfassung:
                //     Unable to open the specified port for receiving Proximity messages.
                public const int NS_E_DRM_UNABLE_TO_OPEN_PORT = -1072879441;

                //
                // Zusammenfassung:
                //     The message format is invalid.
                public const int NS_E_DRM_BAD_REQUEST = -1072879440;

                //
                // Zusammenfassung:
                //     The Certificate Revocation List is invalid or corrupted.
                public const int NS_E_DRM_INVALID_CRL = -1072879439;

                //
                // Zusammenfassung:
                //     The length of the attribute name or value is too long.
                public const int NS_E_DRM_ATTRIBUTE_TOO_LONG = -1072879438;

                //
                // Zusammenfassung:
                //     The license blob passed in the cardea request is expired.
                public const int NS_E_DRM_EXPIRED_LICENSEBLOB = -1072879437;

                //
                // Zusammenfassung:
                //     The license blob passed in the cardea request is invalid. Contact Microsoft product
                //     support.
                public const int NS_E_DRM_INVALID_LICENSEBLOB = -1072879436;

                //
                // Zusammenfassung:
                //     The requested operation cannot be performed because the license does not contain
                //     an inclusion list.
                public const int NS_E_DRM_INCLUSION_LIST_REQUIRED = -1072879435;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_DRMV2CLT_REVOKED = -1072879434;

                //
                // Zusammenfassung:
                //     A problem has occurred in the Digital Rights Management component. Contact Microsoft
                //     product support.
                public const int NS_E_DRM_RIV_TOO_SMALL = -1072879433;

                //
                // Zusammenfassung:
                //     Windows Media Player does not support the level of output protection required
                //     by the content.
                public const int NS_E_OUTPUT_PROTECTION_LEVEL_UNSUPPORTED = -1072879356;

                //
                // Zusammenfassung:
                //     Windows Media Player does not support the level of protection required for compressed
                //     digital video.
                public const int NS_E_COMPRESSED_DIGITAL_VIDEO_PROTECTION_LEVEL_UNSUPPORTED = -1072879355;

                //
                // Zusammenfassung:
                //     Windows Media Player does not support the level of protection required for uncompressed
                //     digital video.
                public const int NS_E_UNCOMPRESSED_DIGITAL_VIDEO_PROTECTION_LEVEL_UNSUPPORTED = -1072879354;

                //
                // Zusammenfassung:
                //     Windows Media Player does not support the level of protection required for analog
                //     video.
                public const int NS_E_ANALOG_VIDEO_PROTECTION_LEVEL_UNSUPPORTED = -1072879353;

                //
                // Zusammenfassung:
                //     Windows Media Player does not support the level of protection required for compressed
                //     digital audio.
                public const int NS_E_COMPRESSED_DIGITAL_AUDIO_PROTECTION_LEVEL_UNSUPPORTED = -1072879352;

                //
                // Zusammenfassung:
                //     Windows Media Player does not support the level of protection required for uncompressed
                //     digital audio.
                public const int NS_E_UNCOMPRESSED_DIGITAL_AUDIO_PROTECTION_LEVEL_UNSUPPORTED = -1072879351;

                //
                // Zusammenfassung:
                //     Windows Media Player does not support the scheme of output protection required
                //     by the content.
                public const int NS_E_OUTPUT_PROTECTION_SCHEME_UNSUPPORTED = -1072879350;

                //
                // Zusammenfassung:
                //     Installation was not successful and some file cleanup is not complete. For best
                //     results, restart your computer.
                public const int NS_E_REBOOT_RECOMMENDED = -1072878854;

                //
                // Zusammenfassung:
                //     Installation was not successful. To continue, you must restart your computer.
                public const int NS_E_REBOOT_REQUIRED = -1072878853;

                //
                // Zusammenfassung:
                //     Installation was not successful.
                public const int NS_E_SETUP_INCOMPLETE = -1072878852;

                //
                // Zusammenfassung:
                //     Setup cannot migrate the Windows Media Digital Rights Management (DRM) components.
                public const int NS_E_SETUP_DRM_MIGRATION_FAILED = -1072878851;

                //
                // Zusammenfassung:
                //     Some skin or playlist components cannot be installed.
                public const int NS_E_SETUP_IGNORABLE_FAILURE = -1072878850;

                //
                // Zusammenfassung:
                //     Setup cannot migrate the Windows Media Digital Rights Management (DRM) components.
                //     In addition, some skin or playlist components cannot be installed.
                public const int NS_E_SETUP_DRM_MIGRATION_FAILED_AND_IGNORABLE_FAILURE = -1072878849;

                //
                // Zusammenfassung:
                //     Installation is blocked because your computer does not meet one or more of the
                //     setup requirements.
                public const int NS_E_SETUP_BLOCKED = -1072878848;

                //
                // Zusammenfassung:
                //     The specified protocol is not supported.
                public const int NS_E_UNKNOWN_PROTOCOL = -1072877856;

                //
                // Zusammenfassung:
                //     The client is redirected to a proxy server.
                public const int NS_E_REDIRECT_TO_PROXY = -1072877855;

                //
                // Zusammenfassung:
                //     The server encountered an unexpected condition which prevented it from fulfilling
                //     the request.
                public const int NS_E_INTERNAL_SERVER_ERROR = -1072877854;

                //
                // Zusammenfassung:
                //     The request could not be understood by the server.
                public const int NS_E_BAD_REQUEST = -1072877853;

                //
                // Zusammenfassung:
                //     The proxy experienced an error while attempting to contact the media server.
                public const int NS_E_ERROR_FROM_PROXY = -1072877852;

                //
                // Zusammenfassung:
                //     The proxy did not receive a timely response while attempting to contact the media
                //     server.
                public const int NS_E_PROXY_TIMEOUT = -1072877851;

                //
                // Zusammenfassung:
                //     The server is currently unable to handle the request due to a temporary overloading
                //     or maintenance of the server.
                public const int NS_E_SERVER_UNAVAILABLE = -1072877850;

                //
                // Zusammenfassung:
                //     The server is refusing to fulfill the requested operation.
                public const int NS_E_REFUSED_BY_SERVER = -1072877849;

                //
                // Zusammenfassung:
                //     The server is not a compatible streaming media server.
                public const int NS_E_INCOMPATIBLE_SERVER = -1072877848;

                //
                // Zusammenfassung:
                //     The content cannot be streamed because the Multicast protocol has been disabled.
                public const int NS_E_MULTICAST_DISABLED = -1072877847;

                //
                // Zusammenfassung:
                //     The server redirected the player to an invalid location.
                public const int NS_E_INVALID_REDIRECT = -1072877846;

                //
                // Zusammenfassung:
                //     The content cannot be streamed because all protocols have been disabled.
                public const int NS_E_ALL_PROTOCOLS_DISABLED = -1072877845;

                //
                // Zusammenfassung:
                //     The MSBD protocol is no longer supported. Please use HTTP to connect to the Windows
                //     Media stream.
                public const int NS_E_MSBD_NO_LONGER_SUPPORTED = -1072877844;

                //
                // Zusammenfassung:
                //     The proxy server could not be located. Please check your proxy server configuration.
                public const int NS_E_PROXY_NOT_FOUND = -1072877843;

                //
                // Zusammenfassung:
                //     Unable to establish a connection to the proxy server. Please check your proxy
                //     server configuration.
                public const int NS_E_CANNOT_CONNECT_TO_PROXY = -1072877842;

                //
                // Zusammenfassung:
                //     Unable to locate the media server. The operation timed out.
                public const int NS_E_SERVER_DNS_TIMEOUT = -1072877841;

                //
                // Zusammenfassung:
                //     Unable to locate the proxy server. The operation timed out.
                public const int NS_E_PROXY_DNS_TIMEOUT = -1072877840;

                //
                // Zusammenfassung:
                //     Media closed because Windows was shut down.
                public const int NS_E_CLOSED_ON_SUSPEND = -1072877839;

                //
                // Zusammenfassung:
                //     Unable to read the contents of a playlist file from a media server.
                public const int NS_E_CANNOT_READ_PLAYLIST_FROM_MEDIASERVER = -1072877838;

                //
                // Zusammenfassung:
                //     Session not found.
                public const int NS_E_SESSION_NOT_FOUND = -1072877837;

                //
                // Zusammenfassung:
                //     Content requires a streaming media client.
                public const int NS_E_REQUIRE_STREAMING_CLIENT = -1072877836;

                //
                // Zusammenfassung:
                //     A command applies to a previous playlist entry.
                public const int NS_E_PLAYLIST_ENTRY_HAS_CHANGED = -1072877835;

                //
                // Zusammenfassung:
                //     The proxy server is denying access. The username and/or password might be incorrect.
                public const int NS_E_PROXY_ACCESSDENIED = -1072877834;

                //
                // Zusammenfassung:
                //     The proxy could not provide valid authentication credentials to the media server.
                public const int NS_E_PROXY_SOURCE_ACCESSDENIED = -1072877833;

                //
                // Zusammenfassung:
                //     The network sink failed to write data to the network.
                public const int NS_E_NETWORK_SINK_WRITE = -1072877832;

                //
                // Zusammenfassung:
                //     Packets are not being received from the server. The packets might be blocked
                //     by a filtering device, such as a network firewall.
                public const int NS_E_FIREWALL = -1072877831;

                //
                // Zusammenfassung:
                //     The MMS protocol is not supported. Please use HTTP or RTSP to connect to the
                //     Windows Media stream.
                public const int NS_E_MMS_NOT_SUPPORTED = -1072877830;

                //
                // Zusammenfassung:
                //     The Windows Media server is denying access. The username and/or password might
                //     be incorrect.
                public const int NS_E_SERVER_ACCESSDENIED = -1072877829;

                //
                // Zusammenfassung:
                //     The Publishing Point or file on the Windows Media Server is no longer available.
                public const int NS_E_RESOURCE_GONE = -1072877828;

                //
                // Zusammenfassung:
                //     There is no existing packetizer plugin for a stream.
                public const int NS_E_NO_EXISTING_PACKETIZER = -1072877827;

                //
                // Zusammenfassung:
                //     The response from the media server could not be understood. This might be caused
                //     by an incompatible proxy server or media server.
                public const int NS_E_BAD_SYNTAX_IN_SERVER_RESPONSE = -1072877826;

                //
                // Zusammenfassung:
                //     The Windows Media Server reset the network connection.
                public const int NS_E_RESET_SOCKET_CONNECTION = -1072877824;

                //
                // Zusammenfassung:
                //     The request could not reach the media server (too many hops).
                public const int NS_E_TOO_MANY_HOPS = -1072877822;

                //
                // Zusammenfassung:
                //     The server is sending too much data. The connection has been terminated.
                public const int NS_E_TOO_MUCH_DATA_FROM_SERVER = -1072877819;

                //
                // Zusammenfassung:
                //     It was not possible to establish a connection to the media server in a timely
                //     manner. The media server might be down for maintenance, or it might be necessary
                //     to use a proxy server to access this media server.
                public const int NS_E_CONNECT_TIMEOUT = -1072877818;

                //
                // Zusammenfassung:
                //     It was not possible to establish a connection to the proxy server in a timely
                //     manner. Please check your proxy server configuration.
                public const int NS_E_PROXY_CONNECT_TIMEOUT = -1072877817;

                //
                // Zusammenfassung:
                //     Session not found.
                public const int NS_E_SESSION_INVALID = -1072877816;

                //
                // Zusammenfassung:
                //     Unknown packet sink stream.
                public const int NS_E_PACKETSINK_UNKNOWN_FEC_STREAM = -1072877814;

                //
                // Zusammenfassung:
                //     Unable to establish a connection to the server. Ensure Windows Media Services
                //     is started and the HTTP Server control protocol is properly enabled.
                public const int NS_E_PUSH_CANNOTCONNECT = -1072877813;

                //
                // Zusammenfassung:
                //     The Server service that received the HTTP push request is not a compatible version
                //     of Windows Media Services (WMS). This error might indicate the push request was
                //     received by IIS instead of WMS. Ensure WMS is started and has the HTTP Server
                //     control protocol properly enabled and try again.
                public const int NS_E_INCOMPATIBLE_PUSH_SERVER = -1072877812;

                //
                // Zusammenfassung:
                //     The playlist has reached its end.
                public const int NS_E_END_OF_PLAYLIST = -1072876856;

                //
                // Zusammenfassung:
                //     Use file source.
                public const int NS_E_USE_FILE_SOURCE = -1072876855;

                //
                // Zusammenfassung:
                //     The property was not found.
                public const int NS_E_PROPERTY_NOT_FOUND = -1072876854;

                //
                // Zusammenfassung:
                //     The property is read only.
                public const int NS_E_PROPERTY_READ_ONLY = -1072876852;

                //
                // Zusammenfassung:
                //     The table key was not found.
                public const int NS_E_TABLE_KEY_NOT_FOUND = -1072876851;

                //
                // Zusammenfassung:
                //     Invalid query operator.
                public const int NS_E_INVALID_QUERY_OPERATOR = -1072876849;

                //
                // Zusammenfassung:
                //     Invalid query property.
                public const int NS_E_INVALID_QUERY_PROPERTY = -1072876848;

                //
                // Zusammenfassung:
                //     The property is not supported.
                public const int NS_E_PROPERTY_NOT_SUPPORTED = -1072876846;

                //
                // Zusammenfassung:
                //     Schema classification failure.
                public const int NS_E_SCHEMA_CLASSIFY_FAILURE = -1072876844;

                //
                // Zusammenfassung:
                //     The metadata format is not supported.
                public const int NS_E_METADATA_FORMAT_NOT_SUPPORTED = -1072876843;

                //
                // Zusammenfassung:
                //     Cannot edit the metadata.
                public const int NS_E_METADATA_NO_EDITING_CAPABILITY = -1072876842;

                //
                // Zusammenfassung:
                //     Cannot set the locale id.
                public const int NS_E_METADATA_CANNOT_SET_LOCALE = -1072876841;

                //
                // Zusammenfassung:
                //     The language is not supported in the format.
                public const int NS_E_METADATA_LANGUAGE_NOT_SUPORTED = -1072876840;

                //
                // Zusammenfassung:
                //     There is no RFC1766 name translation for the supplied locale id.
                public const int NS_E_METADATA_NO_RFC1766_NAME_FOR_LOCALE = -1072876839;

                //
                // Zusammenfassung:
                //     The metadata (or metadata item) is not available.
                public const int NS_E_METADATA_NOT_AVAILABLE = -1072876838;

                //
                // Zusammenfassung:
                //     The cached metadata (or metadata item) is not available.
                public const int NS_E_METADATA_CACHE_DATA_NOT_AVAILABLE = -1072876837;

                //
                // Zusammenfassung:
                //     The metadata document is invalid.
                public const int NS_E_METADATA_INVALID_DOCUMENT_TYPE = -1072876836;

                //
                // Zusammenfassung:
                //     The metadata content identifier is not available.
                public const int NS_E_METADATA_IDENTIFIER_NOT_AVAILABLE = -1072876835;

                //
                // Zusammenfassung:
                //     Cannot retrieve metadata from the offline metadata cache.
                public const int NS_E_METADATA_CANNOT_RETRIEVE_FROM_OFFLINE_CACHE = -1072876834;

                //
                // Zusammenfassung:
                //     Checksum of the obtained monitor descriptor is invalid.
                public const int ERROR_MONITOR_INVALID_DESCRIPTOR_CHECKSUM = -1071247357;

                //
                // Zusammenfassung:
                //     Monitor descriptor contains an invalid standard timing block.
                public const int ERROR_MONITOR_INVALID_STANDARD_TIMING_BLOCK = -1071247356;

                //
                // Zusammenfassung:
                //     Windows Management Instrumentation (WMI) data block registration failed for one
                //     of the MSMonitorClass WMI subclasses.
                public const int ERROR_MONITOR_WMI_DATABLOCK_REGISTRATION_FAILED = -1071247355;

                //
                // Zusammenfassung:
                //     Provided monitor descriptor block is either corrupted or does not contain the
                //     monitor's detailed serial number.
                public const int ERROR_MONITOR_INVALID_SERIAL_NUMBER_MONDSC_BLOCK = -1071247354;

                //
                // Zusammenfassung:
                //     Provided monitor descriptor block is either corrupted or does not contain the
                //     monitor's user-friendly name.
                public const int ERROR_MONITOR_INVALID_USER_FRIENDLY_MONDSC_BLOCK = -1071247353;

                //
                // Zusammenfassung:
                //     There is no monitor descriptor data at the specified (offset, size) region.
                public const int ERROR_MONITOR_NO_MORE_DESCRIPTOR_DATA = -1071247352;

                //
                // Zusammenfassung:
                //     Monitor descriptor contains an invalid detailed timing block.
                public const int ERROR_MONITOR_INVALID_DETAILED_TIMING_BLOCK = -1071247351;

                //
                // Zusammenfassung:
                //     Exclusive mode ownership is needed to create unmanaged primary allocation.
                public const int ERROR_GRAPHICS_NOT_EXCLUSIVE_MODE_OWNER = -1071243264;

                //
                // Zusammenfassung:
                //     The driver needs more direct memory access (DMA) buffer space to complete the
                //     requested operation.
                public const int ERROR_GRAPHICS_INSUFFICIENT_DMA_BUFFER = -1071243263;

                //
                // Zusammenfassung:
                //     Specified display adapter handle is invalid.
                public const int ERROR_GRAPHICS_INVALID_DISPLAY_ADAPTER = -1071243262;

                //
                // Zusammenfassung:
                //     Specified display adapter and all of its state has been reset.
                public const int ERROR_GRAPHICS_ADAPTER_WAS_RESET = -1071243261;

                //
                // Zusammenfassung:
                //     The driver stack does not match the expected driver model.
                public const int ERROR_GRAPHICS_INVALID_DRIVER_MODEL = -1071243260;

                //
                // Zusammenfassung:
                //     Present happened but ended up into the changed desktop mode.
                public const int ERROR_GRAPHICS_PRESENT_MODE_CHANGED = -1071243259;

                //
                // Zusammenfassung:
                //     Nothing to present due to desktop occlusion.
                public const int ERROR_GRAPHICS_PRESENT_OCCLUDED = -1071243258;

                //
                // Zusammenfassung:
                //     Not able to present due to denial of desktop access.
                public const int ERROR_GRAPHICS_PRESENT_DENIED = -1071243257;

                //
                // Zusammenfassung:
                //     Not able to present with color conversion.
                public const int ERROR_GRAPHICS_CANNOTCOLORCONVERT = -1071243256;

                //
                // Zusammenfassung:
                //     Not enough video memory available to complete the operation.
                public const int ERROR_GRAPHICS_NO_VIDEO_MEMORY = -1071243008;

                //
                // Zusammenfassung:
                //     Could not probe and lock the underlying memory of an allocation.
                public const int ERROR_GRAPHICS_CANT_LOCK_MEMORY = -1071243007;

                //
                // Zusammenfassung:
                //     The allocation is currently busy.
                public const int ERROR_GRAPHICS_ALLOCATION_BUSY = -1071243006;

                //
                // Zusammenfassung:
                //     An object being referenced has reach the maximum reference count already and
                //     cannot be referenced further.
                public const int ERROR_GRAPHICS_TOO_MANY_REFERENCES = -1071243005;

                //
                // Zusammenfassung:
                //     A problem could not be solved due to some currently existing condition. The problem
                //     should be tried again later.
                public const int ERROR_GRAPHICS_TRY_AGAIN_LATER = -1071243004;

                //
                // Zusammenfassung:
                //     A problem could not be solved due to some currently existing condition. The problem
                //     should be tried again immediately.
                public const int ERROR_GRAPHICS_TRY_AGAIN_NOW = -1071243003;

                //
                // Zusammenfassung:
                //     The allocation is invalid.
                public const int ERROR_GRAPHICS_ALLOCATION_INVALID = -1071243002;

                //
                // Zusammenfassung:
                //     No more unswizzling apertures are currently available.
                public const int ERROR_GRAPHICS_UNSWIZZLING_APERTURE_UNAVAILABLE = -1071243001;

                //
                // Zusammenfassung:
                //     The current allocation cannot be unswizzled by an aperture.
                public const int ERROR_GRAPHICS_UNSWIZZLING_APERTURE_UNSUPPORTED = -1071243000;

                //
                // Zusammenfassung:
                //     The request failed because a pinned allocation cannot be evicted.
                public const int ERROR_GRAPHICS_CANT_EVICT_PINNED_ALLOCATION = -1071242999;

                //
                // Zusammenfassung:
                //     The allocation cannot be used from its current segment location for the specified
                //     operation.
                public const int ERROR_GRAPHICS_INVALID_ALLOCATION_USAGE = -1071242992;

                //
                // Zusammenfassung:
                //     A locked allocation cannot be used in the current command buffer.
                public const int ERROR_GRAPHICS_CANT_RENDER_LOCKED_ALLOCATION = -1071242991;

                //
                // Zusammenfassung:
                //     The allocation being referenced has been closed permanently.
                public const int ERROR_GRAPHICS_ALLOCATION_CLOSED = -1071242990;

                //
                // Zusammenfassung:
                //     An invalid allocation instance is being referenced.
                public const int ERROR_GRAPHICS_INVALID_ALLOCATION_INSTANCE = -1071242989;

                //
                // Zusammenfassung:
                //     An invalid allocation handle is being referenced.
                public const int ERROR_GRAPHICS_INVALID_ALLOCATION_HANDLE = -1071242988;

                //
                // Zusammenfassung:
                //     The allocation being referenced does not belong to the current device.
                public const int ERROR_GRAPHICS_WRONG_ALLOCATION_DEVICE = -1071242987;

                //
                // Zusammenfassung:
                //     The specified allocation lost its content.
                public const int ERROR_GRAPHICS_ALLOCATION_CONTENT_LOST = -1071242986;

                //
                // Zusammenfassung:
                //     Graphics processing unit (GPU) exception is detected on the given device. The
                //     device is not able to be scheduled.
                public const int ERROR_GRAPHICS_GPU_EXCEPTION_ON_DEVICE = -1071242752;

                //
                // Zusammenfassung:
                //     Specified video present network (VidPN) topology is invalid.
                public const int ERROR_GRAPHICS_INVALID_VIDPN_TOPOLOGY = -1071242496;

                //
                // Zusammenfassung:
                //     Specified VidPN topology is valid but is not supported by this model of the display
                //     adapter.
                public const int ERROR_GRAPHICS_VIDPN_TOPOLOGY_NOT_SUPPORTED = -1071242495;

                //
                // Zusammenfassung:
                //     Specified VidPN topology is valid but is not supported by the display adapter
                //     at this time, due to current allocation of its resources.
                public const int ERROR_GRAPHICS_VIDPN_TOPOLOGY_CURRENTLY_NOT_SUPPORTED = -1071242494;

                //
                // Zusammenfassung:
                //     Specified VidPN handle is invalid.
                public const int ERROR_GRAPHICS_INVALID_VIDPN = -1071242493;

                //
                // Zusammenfassung:
                //     Specified video present source is invalid.
                public const int ERROR_GRAPHICS_INVALID_VIDEO_PRESENT_SOURCE = -1071242492;

                //
                // Zusammenfassung:
                //     Specified video present target is invalid.
                public const int ERROR_GRAPHICS_INVALID_VIDEO_PRESENT_TARGET = -1071242491;

                //
                // Zusammenfassung:
                //     Specified VidPN modality is not supported (for example, at least two of the pinned
                //     modes are not cofunctional).
                public const int ERROR_GRAPHICS_VIDPN_MODALITY_NOT_SUPPORTED = -1071242490;

                //
                // Zusammenfassung:
                //     Specified VidPN source mode set is invalid.
                public const int ERROR_GRAPHICS_INVALID_VIDPN_SOURCEMODESET = -1071242488;

                //
                // Zusammenfassung:
                //     Specified VidPN target mode set is invalid.
                public const int ERROR_GRAPHICS_INVALID_VIDPN_TARGETMODESET = -1071242487;

                //
                // Zusammenfassung:
                //     Specified video signal frequency is invalid.
                public const int ERROR_GRAPHICS_INVALID_FREQUENCY = -1071242486;

                //
                // Zusammenfassung:
                //     Specified video signal active region is invalid.
                public const int ERROR_GRAPHICS_INVALID_ACTIVE_REGION = -1071242485;

                //
                // Zusammenfassung:
                //     Specified video signal total region is invalid.
                public const int ERROR_GRAPHICS_INVALID_TOTAL_REGION = -1071242484;

                //
                // Zusammenfassung:
                //     Specified video present source mode is invalid.
                public const int ERROR_GRAPHICS_INVALID_VIDEO_PRESENT_SOURCE_MODE = -1071242480;

                //
                // Zusammenfassung:
                //     Specified video present target mode is invalid.
                public const int ERROR_GRAPHICS_INVALID_VIDEO_PRESENT_TARGET_MODE = -1071242479;

                //
                // Zusammenfassung:
                //     Pinned mode must remain in the set on VidPN's cofunctional modality enumeration.
                public const int ERROR_GRAPHICS_PINNED_MODE_MUST_REMAIN_IN_SET = -1071242478;

                //
                // Zusammenfassung:
                //     Specified video present path is already in the VidPN topology.
                public const int ERROR_GRAPHICS_PATH_ALREADY_IN_TOPOLOGY = -1071242477;

                //
                // Zusammenfassung:
                //     Specified mode is already in the mode set.
                public const int ERROR_GRAPHICS_MODE_ALREADY_IN_MODESET = -1071242476;

                //
                // Zusammenfassung:
                //     Specified video present source set is invalid.
                public const int ERROR_GRAPHICS_INVALID_VIDEOPRESENTSOURCESET = -1071242475;

                //
                // Zusammenfassung:
                //     Specified video present target set is invalid.
                public const int ERROR_GRAPHICS_INVALID_VIDEOPRESENTTARGETSET = -1071242474;

                //
                // Zusammenfassung:
                //     Specified video present source is already in the video present source set.
                public const int ERROR_GRAPHICS_SOURCE_ALREADY_IN_SET = -1071242473;

                //
                // Zusammenfassung:
                //     Specified video present target is already in the video present target set.
                public const int ERROR_GRAPHICS_TARGET_ALREADY_IN_SET = -1071242472;

                //
                // Zusammenfassung:
                //     Specified VidPN present path is invalid.
                public const int ERROR_GRAPHICS_INVALID_VIDPN_PRESENT_PATH = -1071242471;

                //
                // Zusammenfassung:
                //     Miniport has no recommendation for augmentation of the specified VidPN topology.
                public const int ERROR_GRAPHICS_NO_RECOMMENDED_VIDPN_TOPOLOGY = -1071242470;

                //
                // Zusammenfassung:
                //     Specified monitor frequency range set is invalid.
                public const int ERROR_GRAPHICS_INVALID_MONITOR_FREQUENCYRANGESET = -1071242469;

                //
                // Zusammenfassung:
                //     Specified monitor frequency range is invalid.
                public const int ERROR_GRAPHICS_INVALID_MONITOR_FREQUENCYRANGE = -1071242468;

                //
                // Zusammenfassung:
                //     Specified frequency range is not in the specified monitor frequency range set.
                public const int ERROR_GRAPHICS_FREQUENCYRANGE_NOT_IN_SET = -1071242467;

                //
                // Zusammenfassung:
                //     Specified frequency range is already in the specified monitor frequency range
                //     set.
                public const int ERROR_GRAPHICS_FREQUENCYRANGE_ALREADY_IN_SET = -1071242465;

                //
                // Zusammenfassung:
                //     Specified mode set is stale. Reacquire the new mode set.
                public const int ERROR_GRAPHICS_STALE_MODESET = -1071242464;

                //
                // Zusammenfassung:
                //     Specified monitor source mode set is invalid.
                public const int ERROR_GRAPHICS_INVALID_MONITOR_SOURCEMODESET = -1071242463;

                //
                // Zusammenfassung:
                //     Specified monitor source mode is invalid.
                public const int ERROR_GRAPHICS_INVALID_MONITOR_SOURCE_MODE = -1071242462;

                //
                // Zusammenfassung:
                //     Miniport does not have any recommendation regarding the request to provide a
                //     functional VidPN given the current display adapter configuration.
                public const int ERROR_GRAPHICS_NO_RECOMMENDED_FUNCTIONAL_VIDPN = -1071242461;

                //
                // Zusammenfassung:
                //     ID of the specified mode is already used by another mode in the set.
                public const int ERROR_GRAPHICS_MODE_ID_MUST_BE_UNIQUE = -1071242460;

                //
                // Zusammenfassung:
                //     System failed to determine a mode that is supported by both the display adapter
                //     and the monitor connected to it.
                public const int ERROR_GRAPHICS_EMPTY_ADAPTER_MONITOR_MODE_SUPPORT_INTERSECTION = -1071242459;

                //
                // Zusammenfassung:
                //     Number of video present targets must be greater than or equal to the number of
                //     video present sources.
                public const int ERROR_GRAPHICS_VIDEO_PRESENT_TARGETS_LESS_THAN_SOURCES = -1071242458;

                //
                // Zusammenfassung:
                //     Specified present path is not in the VidPN topology.
                public const int ERROR_GRAPHICS_PATH_NOT_IN_TOPOLOGY = -1071242457;

                //
                // Zusammenfassung:
                //     Display adapter must have at least one video present source.
                public const int ERROR_GRAPHICS_ADAPTER_MUST_HAVE_AT_LEAST_ONE_SOURCE = -1071242456;

                //
                // Zusammenfassung:
                //     Display adapter must have at least one video present target.
                public const int ERROR_GRAPHICS_ADAPTER_MUST_HAVE_AT_LEAST_ONE_TARGET = -1071242455;

                //
                // Zusammenfassung:
                //     Specified monitor descriptor set is invalid.
                public const int ERROR_GRAPHICS_INVALID_MONITORDESCRIPTORSET = -1071242454;

                //
                // Zusammenfassung:
                //     Specified monitor descriptor is invalid.
                public const int ERROR_GRAPHICS_INVALID_MONITORDESCRIPTOR = -1071242453;

                //
                // Zusammenfassung:
                //     Specified descriptor is not in the specified monitor descriptor set.
                public const int ERROR_GRAPHICS_MONITORDESCRIPTOR_NOT_IN_SET = -1071242452;

                //
                // Zusammenfassung:
                //     Specified descriptor is already in the specified monitor descriptor set.
                public const int ERROR_GRAPHICS_MONITORDESCRIPTOR_ALREADY_IN_SET = -1071242451;

                //
                // Zusammenfassung:
                //     ID of the specified monitor descriptor is already used by another descriptor
                //     in the set.
                public const int ERROR_GRAPHICS_MONITORDESCRIPTOR_ID_MUST_BE_UNIQUE = -1071242450;

                //
                // Zusammenfassung:
                //     Specified video present target subset type is invalid.
                public const int ERROR_GRAPHICS_INVALID_VIDPN_TARGET_SUBSET_TYPE = -1071242449;

                //
                // Zusammenfassung:
                //     Two or more of the specified resources are not related to each other, as defined
                //     by the interface semantics.
                public const int ERROR_GRAPHICS_RESOURCES_NOT_RELATED = -1071242448;

                //
                // Zusammenfassung:
                //     ID of the specified video present source is already used by another source in
                //     the set.
                public const int ERROR_GRAPHICS_SOURCE_ID_MUST_BE_UNIQUE = -1071242447;

                //
                // Zusammenfassung:
                //     ID of the specified video present target is already used by another target in
                //     the set.
                public const int ERROR_GRAPHICS_TARGET_ID_MUST_BE_UNIQUE = -1071242446;

                //
                // Zusammenfassung:
                //     Specified VidPN source cannot be used because there is no available VidPN target
                //     to connect it to.
                public const int ERROR_GRAPHICS_NO_AVAILABLE_VIDPN_TARGET = -1071242445;

                //
                // Zusammenfassung:
                //     Newly arrived monitor could not be associated with a display adapter.
                public const int ERROR_GRAPHICS_MONITOR_COULD_NOT_BE_ASSOCIATED_WITH_ADAPTER = -1071242444;

                //
                // Zusammenfassung:
                //     Display adapter in question does not have an associated VidPN manager.
                public const int ERROR_GRAPHICS_NO_VIDPNMGR = -1071242443;

                //
                // Zusammenfassung:
                //     VidPN manager of the display adapter in question does not have an active VidPN.
                public const int ERROR_GRAPHICS_NO_ACTIVE_VIDPN = -1071242442;

                //
                // Zusammenfassung:
                //     Specified VidPN topology is stale. Re-acquire the new topology.
                public const int ERROR_GRAPHICS_STALE_VIDPN_TOPOLOGY = -1071242441;

                //
                // Zusammenfassung:
                //     There is no monitor connected on the specified video present target.
                public const int ERROR_GRAPHICS_MONITOR_NOT_CONNECTED = -1071242440;

                //
                // Zusammenfassung:
                //     Specified source is not part of the specified VidPN topology.
                public const int ERROR_GRAPHICS_SOURCE_NOT_IN_TOPOLOGY = -1071242439;

                //
                // Zusammenfassung:
                //     Specified primary surface size is invalid.
                public const int ERROR_GRAPHICS_INVALID_PRIMARYSURFACE_SIZE = -1071242438;

                //
                // Zusammenfassung:
                //     Specified visible region size is invalid.
                public const int ERROR_GRAPHICS_INVALID_VISIBLEREGION_SIZE = -1071242437;

                //
                // Zusammenfassung:
                //     Specified stride is invalid.
                public const int ERROR_GRAPHICS_INVALID_STRIDE = -1071242436;

                //
                // Zusammenfassung:
                //     Specified pixel format is invalid.
                public const int ERROR_GRAPHICS_INVALID_PIXELFORMAT = -1071242435;

                //
                // Zusammenfassung:
                //     Specified color basis is invalid.
                public const int ERROR_GRAPHICS_INVALID_COLORBASIS = -1071242434;

                //
                // Zusammenfassung:
                //     Specified pixel value access mode is invalid.
                public const int ERROR_GRAPHICS_INVALID_PIXELVALUEACCESSMODE = -1071242433;

                //
                // Zusammenfassung:
                //     Specified target is not part of the specified VidPN topology.
                public const int ERROR_GRAPHICS_TARGET_NOT_IN_TOPOLOGY = -1071242432;

                //
                // Zusammenfassung:
                //     Failed to acquire display mode management interface.
                public const int ERROR_GRAPHICS_NO_DISPLAY_MODE_MANAGEMENT_SUPPORT = -1071242431;

                //
                // Zusammenfassung:
                //     Specified VidPN source is already owned by a display mode manager (DMM) client
                //     and cannot be used until that client releases it.
                public const int ERROR_GRAPHICS_VIDPN_SOURCE_IN_USE = -1071242430;

                //
                // Zusammenfassung:
                //     Specified VidPN is active and cannot be accessed.
                public const int ERROR_GRAPHICS_CANT_ACCESS_ACTIVE_VIDPN = -1071242429;

                //
                // Zusammenfassung:
                //     Specified VidPN present path importance ordinal is invalid.
                public const int ERROR_GRAPHICS_INVALID_PATH_IMPORTANCE_ORDINAL = -1071242428;

                //
                // Zusammenfassung:
                //     Specified VidPN present path content geometry transformation is invalid.
                public const int ERROR_GRAPHICS_INVALID_PATH_CONTENT_GEOMETRY_TRANSFORMATION = -1071242427;

                //
                // Zusammenfassung:
                //     Specified content geometry transformation is not supported on the respective
                //     VidPN present path.
                public const int ERROR_GRAPHICS_PATH_CONTENT_GEOMETRY_TRANSFORMATION_NOT_SUPPORTED = -1071242426;

                //
                // Zusammenfassung:
                //     Specified gamma ramp is invalid.
                public const int ERROR_GRAPHICS_INVALID_GAMMA_RAMP = -1071242425;

                //
                // Zusammenfassung:
                //     Specified gamma ramp is not supported on the respective VidPN present path.
                public const int ERROR_GRAPHICS_GAMMA_RAMP_NOT_SUPPORTED = -1071242424;

                //
                // Zusammenfassung:
                //     Multisampling is not supported on the respective VidPN present path.
                public const int ERROR_GRAPHICS_MULTISAMPLING_NOT_SUPPORTED = -1071242423;

                //
                // Zusammenfassung:
                //     Specified mode is not in the specified mode set.
                public const int ERROR_GRAPHICS_MODE_NOT_IN_MODESET = -1071242422;

                //
                // Zusammenfassung:
                //     Specified VidPN topology recommendation reason is invalid.
                public const int ERROR_GRAPHICS_INVALID_VIDPN_TOPOLOGY_RECOMMENDATION_REASON = -1071242419;

                //
                // Zusammenfassung:
                //     Specified VidPN present path content type is invalid.
                public const int ERROR_GRAPHICS_INVALID_PATH_CONTENT_TYPE = -1071242418;

                //
                // Zusammenfassung:
                //     Specified VidPN present path copy protection type is invalid.
                public const int ERROR_GRAPHICS_INVALID_COPYPROTECTION_TYPE = -1071242417;

                //
                // Zusammenfassung:
                //     No more than one unassigned mode set can exist at any given time for a given
                //     VidPN source or target.
                public const int ERROR_GRAPHICS_UNASSIGNED_MODESET_ALREADY_EXISTS = -1071242416;

                //
                // Zusammenfassung:
                //     The specified scan line ordering type is invalid.
                public const int ERROR_GRAPHICS_INVALID_SCANLINE_ORDERING = -1071242414;

                //
                // Zusammenfassung:
                //     Topology changes are not allowed for the specified VidPN.
                public const int ERROR_GRAPHICS_TOPOLOGY_CHANGES_NOT_ALLOWED = -1071242413;

                //
                // Zusammenfassung:
                //     All available importance ordinals are already used in the specified topology.
                public const int ERROR_GRAPHICS_NO_AVAILABLE_IMPORTANCE_ORDINALS = -1071242412;

                //
                // Zusammenfassung:
                //     Specified primary surface has a different private format attribute than the current
                //     primary surface.
                public const int ERROR_GRAPHICS_INCOMPATIBLE_PRIVATE_FORMAT = -1071242411;

                //
                // Zusammenfassung:
                //     Specified mode pruning algorithm is invalid.
                public const int ERROR_GRAPHICS_INVALID_MODE_PRUNING_ALGORITHM = -1071242410;

                //
                // Zusammenfassung:
                //     Specified display adapter child device already has an external device connected
                //     to it.
                public const int ERROR_GRAPHICS_SPECIFIED_CHILD_ALREADY_CONNECTED = -1071242240;

                //
                // Zusammenfassung:
                //     The display adapter child device does not support reporting a descriptor.
                public const int ERROR_GRAPHICS_CHILD_DESCRIPTOR_NOT_SUPPORTED = -1071242239;

                //
                // Zusammenfassung:
                //     The display adapter is not linked to any other adapters.
                public const int ERROR_GRAPHICS_NOT_A_LINKED_ADAPTER = -1071242192;

                //
                // Zusammenfassung:
                //     Lead adapter in a linked configuration was not enumerated yet.
                public const int ERROR_GRAPHICS_LEADLINK_NOT_ENUMERATED = -1071242191;

                //
                // Zusammenfassung:
                //     Some chain adapters in a linked configuration were not enumerated yet.
                public const int ERROR_GRAPHICS_CHAINLINKS_NOT_ENUMERATED = -1071242190;

                //
                // Zusammenfassung:
                //     The chain of linked adapters is not ready to start because of an unknown failure.
                public const int ERROR_GRAPHICS_ADAPTER_CHAIN_NOT_READY = -1071242189;

                //
                // Zusammenfassung:
                //     An attempt was made to start a lead link display adapter when the chain links
                //     were not started yet.
                public const int ERROR_GRAPHICS_CHAINLINKS_NOT_STARTED = -1071242188;

                //
                // Zusammenfassung:
                //     An attempt was made to turn on a lead link display adapter when the chain links
                //     were turned off.
                public const int ERROR_GRAPHICS_CHAINLINKS_NOT_POWERED_ON = -1071242187;

                //
                // Zusammenfassung:
                //     The adapter link was found to be in an inconsistent state. Not all adapters are
                //     in an expected PNP or power state.
                public const int ERROR_GRAPHICS_INCONSISTENT_DEVICE_LINK_STATE = -1071242186;

                //
                // Zusammenfassung:
                //     The driver trying to start is not the same as the driver for the posted display
                //     adapter.
                public const int ERROR_GRAPHICS_NOT_POST_DEVICE_DRIVER = -1071242184;

                //
                // Zusammenfassung:
                //     The driver does not support Output Protection Manager (OPM).
                public const int ERROR_GRAPHICS_OPM_NOT_SUPPORTED = -1071241984;

                //
                // Zusammenfassung:
                //     The driver does not support Certified Output Protection Protocol (COPP).
                public const int ERROR_GRAPHICS_COPP_NOT_SUPPORTED = -1071241983;

                //
                // Zusammenfassung:
                //     The driver does not support a user-accessible bus (UAB).
                public const int ERROR_GRAPHICS_UAB_NOT_SUPPORTED = -1071241982;

                //
                // Zusammenfassung:
                //     The specified encrypted parameters are invalid.
                public const int ERROR_GRAPHICS_OPM_INVALID_ENCRYPTED_PARAMETERS = -1071241981;

                //
                // Zusammenfassung:
                //     An array passed to a function cannot hold all of the data that the function wants
                //     to put in it.
                public const int ERROR_GRAPHICS_OPM_PARAMETER_ARRAY_TOO_SMALL = -1071241980;

                //
                // Zusammenfassung:
                //     The GDI display device passed to this function does not have any active video
                //     outputs.
                public const int ERROR_GRAPHICS_OPM_NO_VIDEO_OUTPUTS_EXIST = -1071241979;

                //
                // Zusammenfassung:
                //     The protected video path (PVP) cannot find an actual GDI display device that
                //     corresponds to the passed-in GDI display device name.
                public const int ERROR_GRAPHICS_PVP_NO_DISPLAY_DEVICE_CORRESPONDS_TO_NAME = -1071241978;

                //
                // Zusammenfassung:
                //     This function failed because the GDI display device passed to it was not attached
                //     to the Windows desktop.
                public const int ERROR_GRAPHICS_PVP_DISPLAY_DEVICE_NOT_ATTACHED_TO_DESKTOP = -1071241977;

                //
                // Zusammenfassung:
                //     The PVP does not support mirroring display devices because they do not have video
                //     outputs.
                public const int ERROR_GRAPHICS_PVP_MIRRORING_DEVICES_NOT_SUPPORTED = -1071241976;

                //
                // Zusammenfassung:
                //     The function failed because an invalid pointer parameter was passed to it. A
                //     pointer parameter is invalid if it is null, it points to an invalid address,
                //     it points to a kernel mode address, or it is not correctly aligned.
                public const int ERROR_GRAPHICS_OPM_INVALID_POINTER = -1071241974;

                //
                // Zusammenfassung:
                //     An internal error caused this operation to fail.
                public const int ERROR_GRAPHICS_OPM_INTERNAL_ERROR = -1071241973;

                //
                // Zusammenfassung:
                //     The function failed because the caller passed in an invalid OPM user mode handle.
                public const int ERROR_GRAPHICS_OPM_INVALID_HANDLE = -1071241972;

                //
                // Zusammenfassung:
                //     This function failed because the GDI device passed to it did not have any monitors
                //     associated with it.
                public const int ERROR_GRAPHICS_PVP_NO_MONITORS_CORRESPOND_TO_DISPLAY_DEVICE = -1071241971;

                //
                // Zusammenfassung:
                //     A certificate could not be returned because the certificate buffer passed to
                //     the function was too small.
                public const int ERROR_GRAPHICS_PVP_INVALID_CERTIFICATE_LENGTH = -1071241970;

                //
                // Zusammenfassung:
                //     A video output could not be created because the frame buffer is in spanning mode.
                public const int ERROR_GRAPHICS_OPM_SPANNING_MODE_ENABLED = -1071241969;

                //
                // Zusammenfassung:
                //     A video output could not be created because the frame buffer is in theater mode.
                public const int ERROR_GRAPHICS_OPM_THEATER_MODE_ENABLED = -1071241968;

                //
                // Zusammenfassung:
                //     The function call failed because the display adapter's hardware functionality
                //     scan failed to validate the graphics hardware.
                public const int ERROR_GRAPHICS_PVP_HFS_FAILED = -1071241967;

                //
                // Zusammenfassung:
                //     The High-Bandwidth Digital Content Protection (HDCP) System Renewability Message
                //     (SRM) passed to this function did not comply with section 5 of the HDCP 1.1 specification.
                public const int ERROR_GRAPHICS_OPM_INVALID_SRM = -1071241966;

                //
                // Zusammenfassung:
                //     The video output cannot enable the HDCP system because it does not support it.
                public const int ERROR_GRAPHICS_OPM_OUTPUT_DOES_NOT_SUPPORT_HDCP = -1071241965;

                //
                // Zusammenfassung:
                //     The video output cannot enable analog copy protection because it does not support
                //     it.
                public const int ERROR_GRAPHICS_OPM_OUTPUT_DOES_NOT_SUPPORT_ACP = -1071241964;

                //
                // Zusammenfassung:
                //     The video output cannot enable the Content Generation Management System Analog
                //     (CGMS-A) protection technology because it does not support it.
                public const int ERROR_GRAPHICS_OPM_OUTPUT_DOES_NOT_SUPPORT_CGMSA = -1071241963;

                //
                // Zusammenfassung:
                //     IOPMVideoOutput's GetInformation() method cannot return the version of the SRM
                //     being used because the application never successfully passed an SRM to the video
                //     output.
                public const int ERROR_GRAPHICS_OPM_HDCP_SRM_NEVER_SET = -1071241962;

                //
                // Zusammenfassung:
                //     IOPMVideoOutput's Configure() method cannot enable the specified output protection
                //     technology because the output's screen resolution is too high.
                public const int ERROR_GRAPHICS_OPM_RESOLUTION_TOO_HIGH = -1071241961;

                //
                // Zusammenfassung:
                //     IOPMVideoOutput's Configure() method cannot enable HDCP because the display adapter's
                //     HDCP hardware is already being used by other physical outputs.
                public const int ERROR_GRAPHICS_OPM_ALL_HDCP_HARDWARE_ALREADY_IN_USE = -1071241960;

                //
                // Zusammenfassung:
                //     The operating system asynchronously destroyed this OPM video output because the
                //     operating system's state changed. This error typically occurs because the monitor
                //     physical device object (PDO) associated with this video output was removed, the
                //     monitor PDO associated with this video output was stopped, the video output's
                //     session became a nonconsole session or the video output's desktop became an inactive
                //     desktop.
                public const int ERROR_GRAPHICS_OPM_VIDEO_OUTPUT_NO_LONGER_EXISTS = -1071241959;

                //
                // Zusammenfassung:
                //     IOPMVideoOutput's methods cannot be called when a session is changing its type.
                //     There are currently three types of sessions: console, disconnected and remote
                //     (remote desktop protocol [RDP] or Independent Computing Architecture [ICA]).
                public const int ERROR_GRAPHICS_OPM_SESSION_TYPE_CHANGE_IN_PROGRESS = -1071241958;

                //
                // Zusammenfassung:
                //     The monitor connected to the specified video output does not have an I2C bus.
                public const int ERROR_GRAPHICS_I2C_NOT_SUPPORTED = -1071241856;

                //
                // Zusammenfassung:
                //     No device on the I2C bus has the specified address.
                public const int ERROR_GRAPHICS_I2C_DEVICE_DOES_NOT_EXIST = -1071241855;

                //
                // Zusammenfassung:
                //     An error occurred while transmitting data to the device on the I2C bus.
                public const int ERROR_GRAPHICS_I2C_ERROR_TRANSMITTING_DATA = -1071241854;

                //
                // Zusammenfassung:
                //     An error occurred while receiving data from the device on the I2C bus.
                public const int ERROR_GRAPHICS_I2C_ERROR_RECEIVING_DATA = -1071241853;

                //
                // Zusammenfassung:
                //     The monitor does not support the specified Virtual Control Panel (VCP) code.
                public const int ERROR_GRAPHICS_DDCCI_VCP_NOT_SUPPORTED = -1071241852;

                //
                // Zusammenfassung:
                //     The data received from the monitor is invalid.
                public const int ERROR_GRAPHICS_DDCCI_INVALID_DATA = -1071241851;

                //
                // Zusammenfassung:
                //     A function call failed because a monitor returned an invalid Timing Status byte
                //     when the operating system used the Display Data Channel Command Interface (DDC/CI)
                //     Get Timing Report and Timing Message command to get a timing report from a monitor.
                public const int ERROR_GRAPHICS_DDCCI_MONITOR_RETURNED_INVALID_TIMING_STATUS_BYTE = -1071241850;

                //
                // Zusammenfassung:
                //     The monitor returned a DDC/CI capabilities string that did not comply with the
                //     ACCESS.bus 3.0, DDC/CI 1.1 or MCCS 2 Revision 1 specification.
                public const int ERROR_GRAPHICS_MCA_INVALID_CAPABILITIES_STRING = -1071241849;

                //
                // Zusammenfassung:
                //     An internal Monitor Configuration API error occurred.
                public const int ERROR_GRAPHICS_MCA_INTERNAL_ERROR = -1071241848;

                //
                // Zusammenfassung:
                //     An operation failed because a DDC/CI message had an invalid value in its command
                //     field.
                public const int ERROR_GRAPHICS_DDCCI_INVALID_MESSAGE_COMMAND = -1071241847;

                //
                // Zusammenfassung:
                //     This error occurred because a DDC/CI message length field contained an invalid
                //     value.
                public const int ERROR_GRAPHICS_DDCCI_INVALID_MESSAGE_LENGTH = -1071241846;

                //
                // Zusammenfassung:
                //     This error occurred because the value in a DDC/CI message checksum field did
                //     not match the message's computed checksum value. This error implies that the
                //     data was corrupted while it was being transmitted from a monitor to a computer.
                public const int ERROR_GRAPHICS_DDCCI_INVALID_MESSAGE_CHECKSUM = -1071241845;

                //
                // Zusammenfassung:
                //     The HMONITOR no longer exists, is not attached to the desktop, or corresponds
                //     to a mirroring device.
                public const int ERROR_GRAPHICS_PMEA_INVALID_MONITOR = -1071241770;

                //
                // Zusammenfassung:
                //     The Direct3D (D3D) device's GDI display device no longer exists, is not attached
                //     to the desktop, or is a mirroring display device.
                public const int ERROR_GRAPHICS_PMEA_INVALID_D3D_DEVICE = -1071241769;

                //
                // Zusammenfassung:
                //     A continuous VCP code's current value is greater than its maximum value. This
                //     error code indicates that a monitor returned an invalid value.
                public const int ERROR_GRAPHICS_DDCCI_CURRENT_CURRENT_VALUE_GREATER_THAN_MAXIMUM_VALUE = -1071241768;

                //
                // Zusammenfassung:
                //     The monitor's VCP Version (0xDF) VCP code returned an invalid version value.
                public const int ERROR_GRAPHICS_MCA_INVALID_VCP_VERSION = -1071241767;

                //
                // Zusammenfassung:
                //     The monitor does not comply with the Monitor Control Command Set (MCCS) specification
                //     it claims to support.
                public const int ERROR_GRAPHICS_MCA_MONITOR_VIOLATES_MCCS_SPECIFICATION = -1071241766;

                //
                // Zusammenfassung:
                //     The MCCS version in a monitor's mccs_ver capability does not match the MCCS version
                //     the monitor reports when the VCP Version (0xDF) VCP code is used.
                public const int ERROR_GRAPHICS_MCA_MCCS_VERSION_MISMATCH = -1071241765;

                //
                // Zusammenfassung:
                //     The Monitor Configuration API only works with monitors that support the MCCS
                //     1.0 specification, the MCCS 2.0 specification, or the MCCS 2.0 Revision 1 specification.
                public const int ERROR_GRAPHICS_MCA_UNSUPPORTED_MCCS_VERSION = -1071241764;

                //
                // Zusammenfassung:
                //     The monitor returned an invalid monitor technology type. CRT, plasma, and LCD
                //     (TFT) are examples of monitor technology types. This error implies that the monitor
                //     violated the MCCS 2.0 or MCCS 2.0 Revision 1 specification.
                public const int ERROR_GRAPHICS_MCA_INVALID_TECHNOLOGY_TYPE_RETURNED = -1071241762;

                //
                // Zusammenfassung:
                //     The SetMonitorColorTemperature() caller passed a color temperature to it that
                //     the current monitor did not support. CRT, plasma, and LCD (TFT) are examples
                //     of monitor technology types. This error implies that the monitor violated the
                //     MCCS 2.0 or MCCS 2.0 Revision 1 specification.
                public const int ERROR_GRAPHICS_MCA_UNSUPPORTED_COLOR_TEMPERATURE = -1071241761;

                //
                // Zusammenfassung:
                //     This function can be used only if a program is running in the local console session.
                //     It cannot be used if the program is running on a remote desktop session or on
                //     a terminal server session.
                public const int ERROR_GRAPHICS_ONLY_CONSOLE_SESSION_SUPPORTED = -1071241760;

                //
                // Zusammenfassung:
                //     User responded "Yes" to the dialog.
                public const int COPYENGINE_S_YES = 2555905;

                //
                // Zusammenfassung:
                //     Undocumented.
                public const int COPYENGINE_S_NOT_HANDLED = 2555907;

                //
                // Zusammenfassung:
                //     User responded to retry the current action.
                public const int COPYENGINE_S_USER_RETRY = 2555908;

                //
                // Zusammenfassung:
                //     User responded "No" to the dialog.
                public const int COPYENGINE_S_USER_IGNORED = 2555909;

                //
                // Zusammenfassung:
                //     User responded to merge folders.
                public const int COPYENGINE_S_MERGE = 2555910;

                //
                // Zusammenfassung:
                //     Child items should not be processed.
                public const int COPYENGINE_S_DONT_PROCESS_CHILDREN = 2555912;

                //
                // Zusammenfassung:
                //     Undocumented.
                public const int COPYENGINE_S_ALREADY_DONE = 2555914;

                //
                // Zusammenfassung:
                //     Error has been queued and will display later.
                public const int COPYENGINE_S_PENDING = 2555915;

                //
                // Zusammenfassung:
                //     Undocumented.
                public const int COPYENGINE_S_KEEP_BOTH = 2555916;

                //
                // Zusammenfassung:
                //     Close the program using the current file
                public const int COPYENGINE_S_CLOSE_PROGRAM = 2555917;

                //
                // Zusammenfassung:
                //     User wants to canceled entire job
                public const int COPYENGINE_E_USER_CANCELLED = -2144927744;

                //
                // Zusammenfassung:
                //     Engine wants to canceled entire job, don't set the CANCELLED bit
                public const int COPYENGINE_E_CANCELLED = -2144927743;

                //
                // Zusammenfassung:
                //     Need to elevate the process to complete the operation
                public const int COPYENGINE_E_REQUIRES_ELEVATION = -2144927742;

                //
                // Zusammenfassung:
                //     Source and destination file are the same
                public const int COPYENGINE_E_SAME_FILE = -2144927741;

                //
                // Zusammenfassung:
                //     Trying to rename a file into a different location, use move instead
                public const int COPYENGINE_E_DIFF_DIR = -2144927740;

                //
                // Zusammenfassung:
                //     One source specified, multiple destinations
                public const int COPYENGINE_E_MANY_SRC_1_DEST = -2144927739;

                //
                // Zusammenfassung:
                //     The destination is a sub-tree of the source
                public const int COPYENGINE_E_DEST_SUBTREE = -2144927735;

                //
                // Zusammenfassung:
                //     The destination is the same folder as the source
                public const int COPYENGINE_E_DEST_SAME_TREE = -2144927734;

                //
                // Zusammenfassung:
                //     Existing destination file with same name as folder
                public const int COPYENGINE_E_FLD_IS_FILE_DEST = -2144927733;

                //
                // Zusammenfassung:
                //     Existing destination folder with same name as file
                public const int COPYENGINE_E_FILE_IS_FLD_DEST = -2144927732;

                //
                // Zusammenfassung:
                //     File too large for destination file system
                public const int COPYENGINE_E_FILE_TOO_LARGE = -2144927731;

                //
                // Zusammenfassung:
                //     Destination device is full and happens to be removable
                public const int COPYENGINE_E_REMOVABLE_FULL = -2144927730;

                //
                // Zusammenfassung:
                //     Destination is a Read-Only CDRom, possibly unformatted
                public const int COPYENGINE_E_DEST_IS_RO_CD = -2144927729;

                //
                // Zusammenfassung:
                //     Destination is a Read/Write CDRom, possibly unformatted
                public const int COPYENGINE_E_DEST_IS_RW_CD = -2144927728;

                //
                // Zusammenfassung:
                //     Destination is a Recordable (Audio, CDRom, possibly unformatted
                public const int COPYENGINE_E_DEST_IS_R_CD = -2144927727;

                //
                // Zusammenfassung:
                //     Destination is a Read-Only DVD, possibly unformatted
                public const int COPYENGINE_E_DEST_IS_RO_DVD = -2144927726;

                //
                // Zusammenfassung:
                //     Destination is a Read/Wrote DVD, possibly unformatted
                public const int COPYENGINE_E_DEST_IS_RW_DVD = -2144927725;

                //
                // Zusammenfassung:
                //     Destination is a Recordable (Audio, DVD, possibly unformatted
                public const int COPYENGINE_E_DEST_IS_R_DVD = -2144927724;

                //
                // Zusammenfassung:
                //     Source is a Read-Only CDRom, possibly unformatted
                public const int COPYENGINE_E_SRC_IS_RO_CD = -2144927723;

                //
                // Zusammenfassung:
                //     Source is a Read/Write CDRom, possibly unformatted
                public const int COPYENGINE_E_SRC_IS_RW_CD = -2144927722;

                //
                // Zusammenfassung:
                //     Source is a Recordable (Audio, CDRom, possibly unformatted
                public const int COPYENGINE_E_SRC_IS_R_CD = -2144927721;

                //
                // Zusammenfassung:
                //     Source is a Read-Only DVD, possibly unformatted
                public const int COPYENGINE_E_SRC_IS_RO_DVD = -2144927720;

                //
                // Zusammenfassung:
                //     Source is a Read/Wrote DVD, possibly unformatted
                public const int COPYENGINE_E_SRC_IS_RW_DVD = -2144927719;

                //
                // Zusammenfassung:
                //     Source is a Recordable (Audio, DVD, possibly unformatted
                public const int COPYENGINE_E_SRC_IS_R_DVD = -2144927718;

                //
                // Zusammenfassung:
                //     Invalid source path
                public const int COPYENGINE_E_INVALID_FILES_SRC = -2144927717;

                //
                // Zusammenfassung:
                //     Invalid destination path
                public const int COPYENGINE_E_INVALID_FILES_DEST = -2144927716;

                //
                // Zusammenfassung:
                //     Source Files within folders where the overall path is longer than MAX_PATH
                public const int COPYENGINE_E_PATH_TOO_DEEP_SRC = -2144927715;

                //
                // Zusammenfassung:
                //     Destination files would be within folders where the overall path is longer than
                //     MAX_PATH
                public const int COPYENGINE_E_PATH_TOO_DEEP_DEST = -2144927714;

                //
                // Zusammenfassung:
                //     Source is a root directory, cannot be moved or renamed
                public const int COPYENGINE_E_ROOT_DIR_SRC = -2144927713;

                //
                // Zusammenfassung:
                //     Destination is a root directory, cannot be renamed
                public const int COPYENGINE_E_ROOT_DIR_DEST = -2144927712;

                //
                // Zusammenfassung:
                //     Security problem on source
                public const int COPYENGINE_E_ACCESS_DENIED_SRC = -2144927711;

                //
                // Zusammenfassung:
                //     Security problem on destination
                public const int COPYENGINE_E_ACCESS_DENIED_DEST = -2144927710;

                //
                // Zusammenfassung:
                //     Source file does not exist, or is unavailable
                public const int COPYENGINE_E_PATH_NOT_FOUND_SRC = -2144927709;

                //
                // Zusammenfassung:
                //     Destination file does not exist, or is unavailable
                public const int COPYENGINE_E_PATH_NOT_FOUND_DEST = -2144927708;

                //
                // Zusammenfassung:
                //     Source file is on a disconnected network location
                public const int COPYENGINE_E_NET_DISCONNECT_SRC = -2144927707;

                //
                // Zusammenfassung:
                //     Destination file is on a disconnected network location
                public const int COPYENGINE_E_NET_DISCONNECT_DEST = -2144927706;

                //
                // Zusammenfassung:
                //     Sharing Violation on source
                public const int COPYENGINE_E_SHARING_VIOLATION_SRC = -2144927705;

                //
                // Zusammenfassung:
                //     Sharing Violation on destination
                public const int COPYENGINE_E_SHARING_VIOLATION_DEST = -2144927704;

                //
                // Zusammenfassung:
                //     Destination exists, cannot replace
                public const int COPYENGINE_E_ALREADY_EXISTS_NORMAL = -2144927703;

                //
                // Zusammenfassung:
                //     Destination with read-only attribute exists, cannot replace
                public const int COPYENGINE_E_ALREADY_EXISTS_READONLY = -2144927702;

                //
                // Zusammenfassung:
                //     Destination with system attribute exists, cannot replace
                public const int COPYENGINE_E_ALREADY_EXISTS_SYSTEM = -2144927701;

                //
                // Zusammenfassung:
                //     Destination folder exists, cannot replace
                public const int COPYENGINE_E_ALREADY_EXISTS_FOLDER = -2144927700;

                //
                // Zusammenfassung:
                //     Secondary Stream information would be lost
                public const int COPYENGINE_E_STREAM_LOSS = -2144927699;

                //
                // Zusammenfassung:
                //     Extended Attributes would be lost
                public const int COPYENGINE_E_EA_LOSS = -2144927698;

                //
                // Zusammenfassung:
                //     Property would be lost
                public const int COPYENGINE_E_PROPERTY_LOSS = -2144927697;

                //
                // Zusammenfassung:
                //     Properties would be lost
                public const int COPYENGINE_E_PROPERTIES_LOSS = -2144927696;

                //
                // Zusammenfassung:
                //     Encryption would be lost
                public const int COPYENGINE_E_ENCRYPTION_LOSS = -2144927695;

                //
                // Zusammenfassung:
                //     Entire operation likely won't fit
                public const int COPYENGINE_E_DISK_FULL = -2144927694;

                //
                // Zusammenfassung:
                //     Entire operation likely won't fit, clean-up wizard available
                public const int COPYENGINE_E_DISK_FULL_CLEAN = -2144927693;

                //
                // Zusammenfassung:
                //     Can't reach source folder")
                public const int COPYENGINE_E_CANT_REACH_SOURCE = -2144927691;

                //
                // Zusammenfassung:
                //     ???
                public const int COPYENGINE_E_RECYCLE_UNKNOWN_ERROR = -2144927691;

                //
                // Zusammenfassung:
                //     Recycling not available (usually turned off,
                public const int COPYENGINE_E_RECYCLE_FORCE_NUKE = -2144927690;

                //
                // Zusammenfassung:
                //     Item is too large for the recycle-bin
                public const int COPYENGINE_E_RECYCLE_SIZE_TOO_BIG = -2144927689;

                //
                // Zusammenfassung:
                //     Folder is too deep to fit in the recycle-bin
                public const int COPYENGINE_E_RECYCLE_PATH_TOO_LONG = -2144927688;

                //
                // Zusammenfassung:
                //     Recycle bin could not be found or is unavailable
                public const int COPYENGINE_E_RECYCLE_BIN_NOT_FOUND = -2144927686;

                //
                // Zusammenfassung:
                //     Name of the new file being created is too long
                public const int COPYENGINE_E_NEWFILE_NAME_TOO_LONG = -2144927685;

                //
                // Zusammenfassung:
                //     Name of the new folder being created is too long
                public const int COPYENGINE_E_NEWFOLDER_NAME_TOO_LONG = -2144927684;

                //
                // Zusammenfassung:
                //     The directory being processed is not empty
                public const int COPYENGINE_E_DIR_NOT_EMPTY = -2144927683;

                //
                // Zusammenfassung:
                //     The IPv6 protocol is not installed.
                public const int PEER_E_IPV6_NOT_INSTALLED = -2140995583;

                //
                // Zusammenfassung:
                //     The component has not been initialized.
                public const int PEER_E_NOT_INITIALIZED = -2140995582;

                //
                // Zusammenfassung:
                //     The required service cannot be started.
                public const int PEER_E_CANNOT_START_SERVICE = -2140995581;

                //
                // Zusammenfassung:
                //     The P2P protocol is not licensed to run on this OS.
                public const int PEER_E_NOT_LICENSED = -2140995580;

                //
                // Zusammenfassung:
                //     The graph handle is invalid.
                public const int PEER_E_INVALID_GRAPH = -2140995568;

                //
                // Zusammenfassung:
                //     The graph database name has changed.
                public const int PEER_E_DBNAME_CHANGED = -2140995567;

                //
                // Zusammenfassung:
                //     A graph with the same ID already exists.
                public const int PEER_E_DUPLICATE_GRAPH = -2140995566;

                //
                // Zusammenfassung:
                //     The graph is not ready.
                public const int PEER_E_GRAPH_NOT_READY = -2140995565;

                //
                // Zusammenfassung:
                //     The graph is shutting down.
                public const int PEER_E_GRAPH_SHUTTING_DOWN = -2140995564;

                //
                // Zusammenfassung:
                //     The graph is still in use.
                public const int PEER_E_GRAPH_IN_USE = -2140995563;

                //
                // Zusammenfassung:
                //     The graph database is corrupt.
                public const int PEER_E_INVALID_DATABASE = -2140995562;

                //
                // Zusammenfassung:
                //     Too many attributes have been used.
                public const int PEER_E_TOO_MANY_ATTRIBUTES = -2140995561;

                //
                // Zusammenfassung:
                //     The connection can not be found.
                public const int PEER_E_CONNECTION_NOT_FOUND = -2140995325;

                //
                // Zusammenfassung:
                //     The peer attempted to connect to itself.
                public const int PEER_E_CONNECT_SELF = -2140995322;

                //
                // Zusammenfassung:
                //     The peer is already listening for connections.
                public const int PEER_E_ALREADY_LISTENING = -2140995321;

                //
                // Zusammenfassung:
                //     The node was not found.
                public const int PEER_E_NODE_NOT_FOUND = -2140995320;

                //
                // Zusammenfassung:
                //     The Connection attempt failed.
                public const int PEER_E_CONNECTION_FAILED = -2140995319;

                //
                // Zusammenfassung:
                //     The peer connection could not be authenticated.
                public const int PEER_E_CONNECTION_NOT_AUTHENTICATED = -2140995318;

                //
                // Zusammenfassung:
                //     The connection was refused.
                public const int PEER_E_CONNECTION_REFUSED = -2140995317;

                //
                // Zusammenfassung:
                //     The peer name classifier is too long.
                public const int PEER_E_CLASSIFIER_TOO_LONG = -2140995071;

                //
                // Zusammenfassung:
                //     The maximum number of identities have been created.
                public const int PEER_E_TOO_MANY_IDENTITIES = -2140995070;

                //
                // Zusammenfassung:
                //     Unable to access a key.
                public const int PEER_E_NO_KEY_ACCESS = -2140995069;

                //
                // Zusammenfassung:
                //     The group already exists.
                public const int PEER_E_GROUPS_EXIST = -2140995068;

                //
                // Zusammenfassung:
                //     The requested record could not be found.
                public const int PEER_E_RECORD_NOT_FOUND = -2140994815;

                //
                // Zusammenfassung:
                //     Access to the database was denied.
                public const int PEER_E_DATABASE_ACCESSDENIED = -2140994814;

                //
                // Zusammenfassung:
                //     The Database could not be initialized.
                public const int PEER_E_DBINITIALIZATION_FAILED = -2140994813;

                //
                // Zusammenfassung:
                //     The record is too big.
                public const int PEER_E_MAX_RECORD_SIZE_EXCEEDED = -2140994812;

                //
                // Zusammenfassung:
                //     The database already exists.
                public const int PEER_E_DATABASE_ALREADY_PRESENT = -2140994811;

                //
                // Zusammenfassung:
                //     The database could not be found.
                public const int PEER_E_DATABASE_NOT_PRESENT = -2140994810;

                //
                // Zusammenfassung:
                //     The identity could not be found.
                public const int PEER_E_IDENTITY_NOT_FOUND = -2140994559;

                //
                // Zusammenfassung:
                //     The event handle could not be found.
                public const int PEER_E_EVENT_HANDLE_NOT_FOUND = -2140994303;

                //
                // Zusammenfassung:
                //     Invalid search.
                public const int PEER_E_INVALID_SEARCH = -2140994047;

                //
                // Zusammenfassung:
                //     The search attributes are invalid.
                public const int PEER_E_INVALID_ATTRIBUTES = -2140994046;

                //
                // Zusammenfassung:
                //     The invitation is not trusted.
                public const int PEER_E_INVITATION_NOT_TRUSTED = -2140993791;

                //
                // Zusammenfassung:
                //     The certchain is too long.
                public const int PEER_E_CHAIN_TOO_LONG = -2140993789;

                //
                // Zusammenfassung:
                //     The time period is invalid.
                public const int PEER_E_INVALID_TIME_PERIOD = -2140993787;

                //
                // Zusammenfassung:
                //     A circular cert chain was detected.
                public const int PEER_E_CIRCULAR_CHAIN_DETECTED = -2140993786;

                //
                // Zusammenfassung:
                //     The certstore is corrupted.
                public const int PEER_E_CERT_STORE_CORRUPTED = -2140993535;

                //
                // Zusammenfassung:
                //     The specified PNRP cloud does not exist.
                public const int PEER_E_NO_CLOUD = -2140991487;

                //
                // Zusammenfassung:
                //     The cloud name is ambiguous.
                public const int PEER_E_CLOUD_NAME_AMBIGUOUS = -2140991483;

                //
                // Zusammenfassung:
                //     The record is invalid.
                public const int PEER_E_INVALID_RECORD = -2140987376;

                //
                // Zusammenfassung:
                //     Not authorized.
                public const int PEER_E_NOT_AUTHORIZED = -2140987360;

                //
                // Zusammenfassung:
                //     The password does not meet policy requirements.
                public const int PEER_E_PASSWORD_DOES_NOT_MEET_POLICY = -2140987359;

                //
                // Zusammenfassung:
                //     The record validation has been deferred.
                public const int PEER_E_DEFERRED_VALIDATION = -2140987344;

                //
                // Zusammenfassung:
                //     The group properties are invalid.
                public const int PEER_E_INVALID_GROUP_PROPERTIES = -2140987328;

                //
                // Zusammenfassung:
                //     The peername is invalid.
                public const int PEER_E_INVALID_PEER_NAME = -2140987312;

                //
                // Zusammenfassung:
                //     The classifier is invalid.
                public const int PEER_E_INVALID_CLASSIFIER = -2140987296;

                //
                // Zusammenfassung:
                //     The friendly name is invalid.
                public const int PEER_E_INVALID_FRIENDLY_NAME = -2140987280;

                //
                // Zusammenfassung:
                //     Invalid role property.
                public const int PEER_E_INVALID_ROLE_PROPERTY = -2140987279;

                //
                // Zusammenfassung:
                //     Invalid classifier property.
                public const int PEER_E_INVALID_CLASSIFIER_PROPERTY = -2140987278;

                //
                // Zusammenfassung:
                //     Invalid record expiration.
                public const int PEER_E_INVALID_RECORD_EXPIRATION = -2140987264;

                //
                // Zusammenfassung:
                //     Invalid credential info.
                public const int PEER_E_INVALID_CREDENTIAL_INFO = -2140987263;

                //
                // Zusammenfassung:
                //     Invalid credential.
                public const int PEER_E_INVALID_CREDENTIAL = -2140987262;

                //
                // Zusammenfassung:
                //     Invalid record size.
                public const int PEER_E_INVALID_RECORD_SIZE = -2140987261;

                //
                // Zusammenfassung:
                //     Unsupported version.
                public const int PEER_E_UNSUPPORTED_VERSION = -2140987248;

                //
                // Zusammenfassung:
                //     The group is not ready.
                public const int PEER_E_GROUP_NOT_READY = -2140987247;

                //
                // Zusammenfassung:
                //     The group is still in use.
                public const int PEER_E_GROUP_IN_USE = -2140987246;

                //
                // Zusammenfassung:
                //     The group is invalid.
                public const int PEER_E_INVALID_GROUP = -2140987245;

                //
                // Zusammenfassung:
                //     No members were found.
                public const int PEER_E_NO_MEMBERS_FOUND = -2140987244;

                //
                // Zusammenfassung:
                //     There are no member connections.
                public const int PEER_E_NO_MEMBER_CONNECTIONS = -2140987243;

                //
                // Zusammenfassung:
                //     Unable to listen.
                public const int PEER_E_UNABLE_TO_LISTEN = -2140987242;

                //
                // Zusammenfassung:
                //     The identity does not exist.
                public const int PEER_E_IDENTITY_DELETED = -2140987232;

                //
                // Zusammenfassung:
                //     The service is not available.
                public const int PEER_E_SERVICE_NOT_AVAILABLE = -2140987231;

                //
                // Zusammenfassung:
                //     THe contact could not be found.
                public const int PEER_E_CONTACT_NOT_FOUND = -2140971007;

                //
                // Zusammenfassung:
                //     The graph data was created.
                public const int PEER_S_GRAPH_DATA_CREATED = 6488065;

                //
                // Zusammenfassung:
                //     There is not more event data.
                public const int PEER_S_NO_EVENT_DATA = 6488066;

                //
                // Zusammenfassung:
                //     The graph is already connect.
                public const int PEER_S_ALREADY_CONNECTED = 6496256;

                //
                // Zusammenfassung:
                //     The subscription already exists.
                public const int PEER_S_SUBSCRIPTION_EXISTS = 6512640;

                //
                // Zusammenfassung:
                //     No connectivity.
                public const int PEER_S_NO_CONNECTIVITY = 6488069;

                //
                // Zusammenfassung:
                //     Already a member.
                public const int PEER_S_ALREADY_A_MEMBER = 6488070;

                //
                // Zusammenfassung:
                //     The peername could not be converted to a DNS pnrp name.
                public const int PEER_E_CANNOT_CONVERT_PEER_NAME = -2140979199;

                //
                // Zusammenfassung:
                //     Invalid peer host name.
                public const int PEER_E_INVALID_PEER_HOST_NAME = -2140979198;

                //
                // Zusammenfassung:
                //     No more data could be found.
                public const int PEER_E_NO_MORE = -2140979197;

                //
                // Zusammenfassung:
                //     The existing peer name is already registered.
                public const int PEER_E_PNRP_DUPLICATE_PEER_NAME = -2140979195;

                //
                // Zusammenfassung:
                //     The app invite request was cancelled by the user.
                public const int PEER_E_INVITE_CANCELLED = -2140966912;

                //
                // Zusammenfassung:
                //     No response of the invite was received.
                public const int PEER_E_INVITE_RESPONSE_NOT_AVAILABLE = -2140966911;

                //
                // Zusammenfassung:
                //     User is not signed into serverless presence.
                public const int PEER_E_NOT_SIGNED_IN = -2140966909;

                //
                // Zusammenfassung:
                //     The user declined the privacy policy prompt.
                public const int PEER_E_PRIVACY_DECLINED = -2140966908;

                //
                // Zusammenfassung:
                //     A timeout occurred.
                public const int PEER_E_TIMEOUT = -2140966907;

                //
                // Zusammenfassung:
                //     The address is invalid.
                public const int PEER_E_INVALID_ADDRESS = -2140966905;

                //
                // Zusammenfassung:
                //     A required firewall exception is disabled.
                public const int PEER_E_FW_EXCEPTION_DISABLED = -2140966904;

                //
                // Zusammenfassung:
                //     The service is blocked by a firewall policy.
                public const int PEER_E_FW_BLOCKED_BY_POLICY = -2140966903;

                //
                // Zusammenfassung:
                //     Firewall exceptions are disabled.
                public const int PEER_E_FW_BLOCKED_BY_SHIELDS_UP = -2140966902;

                //
                // Zusammenfassung:
                //     The user declined to enable the firewall exceptions.
                public const int PEER_E_FW_DECLINED = -2140966901;

                //
                // Zusammenfassung:
                //     The IAudioClient object is already initialized.
                public static readonly HRESULT AUDCLNT_E_ALREADY_INITIALIZED = AUDCLNT_ERR(2u);

                //
                // Zusammenfassung:
                //     The AUDCLNT_STREAMFLAGS_EVENTCALLBACK flag is set but parameters hnsBufferDuration
                //     and hnsPeriodicity are not equal.
                public static readonly HRESULT AUDCLNT_E_BUFDURATION_PERIOD_NOT_EQUAL = AUDCLNT_ERR(19u);

                //
                // Zusammenfassung:
                //     GetBuffer failed to retrieve a data buffer and *ppData points to NULL. For more
                //     information, see Remarks.
                public static readonly HRESULT AUDCLNT_E_BUFFER_ERROR = AUDCLNT_ERR(24u);

                //
                // Zusammenfassung:
                //     Buffer cannot be accessed because a stream reset is in progress.
                public static readonly HRESULT AUDCLNT_E_BUFFER_OPERATION_PENDING = AUDCLNT_ERR(11u);

                //
                // Zusammenfassung:
                //     Indicates that the buffer duration value requested by an exclusive-mode client
                //     is out of range. The requested duration value for pull mode must not be greater
                //     than 500 milliseconds; for push mode the duration value must not be greater than
                //     2 seconds.
                public static readonly HRESULT AUDCLNT_E_BUFFER_SIZE_ERROR = AUDCLNT_ERR(22u);

                //
                // Zusammenfassung:
                //     The requested buffer size is not aligned. This code can be returned for a render
                //     or a capture device if the caller specified AUDCLNT_SHAREMODE_EXCLUSIVE and the
                //     AUDCLNT_STREAMFLAGS_EVENTCALLBACK flags. The caller must call Initialize again
                //     with the aligned buffer size. For more information, see Remarks.
                public static readonly HRESULT AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED = AUDCLNT_ERR(25u);

                //
                // Zusammenfassung:
                //     The NumFramesRequested value exceeds the available buffer space (buffer size
                //     minus padding size).
                public static readonly HRESULT AUDCLNT_E_BUFFER_TOO_LARGE = AUDCLNT_ERR(6u);

                //
                // Zusammenfassung:
                //     Indicates that the process-pass duration exceeded the maximum CPU usage. The
                //     audio engine keeps track of CPU usage by maintaining the number of times the
                //     process-pass duration exceeds the maximum CPU usage. The maximum CPU usage is
                //     calculated as a percent of the engine's periodicity. The percentage value is
                //     the system's CPU throttle value (within the range of 10% and 90%). If this value
                //     is not found, then the default value of 40% is used to calculate the maximum
                //     CPU usage.
                public static readonly HRESULT AUDCLNT_E_CPUUSAGE_EXCEEDED = AUDCLNT_ERR(23u);

                //
                // Zusammenfassung:
                //     The endpoint device is already in use. Either the device is being used in exclusive
                //     mode, or the device is being used in shared mode and the caller asked to use
                //     the device in exclusive mode.
                public static readonly HRESULT AUDCLNT_E_DEVICE_IN_USE = AUDCLNT_ERR(10u);

                //
                // Zusammenfassung:
                //     The audio endpoint device has been unplugged, or the audio hardware or associated
                //     hardware resources have been reconfigured, disabled, removed, or otherwise made
                //     unavailable for use.
                public static readonly HRESULT AUDCLNT_E_DEVICE_INVALIDATED = AUDCLNT_ERR(4u);

                //
                // Zusammenfassung:
                //     The method failed to create the audio endpoint for the render or the capture
                //     device. This can occur if the audio endpoint device has been unplugged, or the
                //     audio hardware or associated hardware resources have been reconfigured, disabled,
                //     removed, or otherwise made unavailable for use.
                public static readonly HRESULT AUDCLNT_E_ENDPOINT_CREATE_FAILED = AUDCLNT_ERR(15u);

                //
                // Zusammenfassung:
                //     The endpoint does not support offloading.
                public static readonly HRESULT AUDCLNT_E_ENDPOINT_OFFLOAD_NOT_CAPABLE = AUDCLNT_ERR(34u);

                //
                // Zusammenfassung:
                //     The client specified AUDCLNT_STREAMOPTIONS_MATCH_FORMAT when calling IAudioClient2::SetClientProperties,
                //     but the format of the audio engine has been locked by another client. In this
                //     case, you can call IAudioClient2::SetClientProperties without specifying the
                //     match format option and then use audio engine's current format.
                public static readonly HRESULT AUDCLNT_E_ENGINE_FORMAT_LOCKED = AUDCLNT_ERR(41u);

                //
                // Zusammenfassung:
                //     The client specified AUDCLNT_STREAMOPTIONS_MATCH_FORMAT when calling IAudioClient2::SetClientProperties,
                //     but the periodicity of the audio engine has been locked by another client. In
                //     this case, you can call IAudioClient2::SetClientProperties without specifying
                //     the match format option and then use audio engine's current periodicity.
                public static readonly HRESULT AUDCLNT_E_ENGINE_PERIODICITY_LOCKED = AUDCLNT_ERR(40u);

                //
                // Zusammenfassung:
                //     The audio stream was not initialized for event-driven buffering.
                public static readonly HRESULT AUDCLNT_E_EVENTHANDLE_NOT_EXPECTED = AUDCLNT_ERR(17u);

                //
                // Zusammenfassung:
                //     The audio stream is configured to use event-driven buffering, but the caller
                //     has not called IAudioClient::SetEventHandle to set the event handle on the stream.
                public static readonly HRESULT AUDCLNT_E_EVENTHANDLE_NOT_SET = AUDCLNT_ERR(20u);

                //
                // Zusammenfassung:
                //     The caller is requesting exclusive-mode use of the endpoint device, but the user
                //     has disabled exclusive-mode use of the device.
                public static readonly HRESULT AUDCLNT_E_EXCLUSIVE_MODE_NOT_ALLOWED = AUDCLNT_ERR(14u);

                //
                // Zusammenfassung:
                //     Exclusive mode only.
                public static readonly HRESULT AUDCLNT_E_EXCLUSIVE_MODE_ONLY = AUDCLNT_ERR(18u);

                public static readonly HRESULT AUDCLNT_E_HEADTRACKING_ENABLED = AUDCLNT_ERR(48u);

                public static readonly HRESULT AUDCLNT_E_HEADTRACKING_UNSUPPORTED = AUDCLNT_ERR(64u);

                public static readonly HRESULT AUDCLNT_E_INCORRECT_BUFFER_SIZE = AUDCLNT_ERR(21u);

                //
                // Zusammenfassung:
                //     Indicates that the requested device period specified with the PeriodInFrames
                //     is not an integral multiple of the fundamental periodicity of the audio engine,
                //     is shorter than the engine's minimum period, or is longer than the engine's maximum
                //     period. Get the supported periodicity values of the engine by calling IAudioClient3::GetSharedModeEnginePeriod.
                public static readonly HRESULT AUDCLNT_E_INVALID_DEVICE_PERIOD = AUDCLNT_ERR(32u);

                //
                // Zusammenfassung:
                //     The NumFramesWritten value exceeds the NumFramesRequested value specified in
                //     the previous IAudioRenderClient::GetBuffer call.
                public static readonly HRESULT AUDCLNT_E_INVALID_SIZE = AUDCLNT_ERR(9u);

                public static readonly HRESULT AUDCLNT_E_INVALID_STREAM_FLAG = AUDCLNT_ERR(33u);

                public static readonly HRESULT AUDCLNT_E_NONOFFLOAD_MODE_ONLY = AUDCLNT_ERR(37u);

                //
                // Zusammenfassung:
                //     The audio stream has not been successfully initialized.
                public static readonly HRESULT AUDCLNT_E_NOT_INITIALIZED = AUDCLNT_ERR(1u);

                //
                // Zusammenfassung:
                //     The audio stream was not stopped at the time of the Start call.
                public static readonly HRESULT AUDCLNT_E_NOT_STOPPED = AUDCLNT_ERR(5u);

                public static readonly HRESULT AUDCLNT_E_OFFLOAD_MODE_ONLY = AUDCLNT_ERR(36u);

                public static readonly HRESULT AUDCLNT_E_OUT_OF_OFFLOAD_RESOURCES = AUDCLNT_ERR(35u);

                //
                // Zusammenfassung:
                //     A previous IAudioRenderClient::GetBuffer call is still in effect.
                public static readonly HRESULT AUDCLNT_E_OUT_OF_ORDER = AUDCLNT_ERR(7u);

                public static readonly HRESULT AUDCLNT_E_RAW_MODE_UNSUPPORTED = AUDCLNT_ERR(39u);

                //
                // Zusammenfassung:
                //     A resource associated with the spatial audio stream is no longer valid.
                public static readonly HRESULT AUDCLNT_E_RESOURCES_INVALIDATED = AUDCLNT_ERR(38u);

                //
                // Zusammenfassung:
                //     The Windows audio service is not running.
                public static readonly HRESULT AUDCLNT_E_SERVICE_NOT_RUNNING = AUDCLNT_ERR(16u);

                public static readonly HRESULT AUDCLNT_E_THREAD_NOT_REGISTERED = AUDCLNT_ERR(12u);

                //
                // Zusammenfassung:
                //     The audio engine (shared mode) or audio endpoint device (exclusive mode) does
                //     not support the specified format.
                public static readonly HRESULT AUDCLNT_E_UNSUPPORTED_FORMAT = AUDCLNT_ERR(8u);

                //
                // Zusammenfassung:
                //     The AUDCLNT_STREAMFLAGS_LOOPBACK flag is set but the endpoint device is a capture
                //     device, not a rendering device.
                public static readonly HRESULT AUDCLNT_E_WRONG_ENDPOINT_TYPE = AUDCLNT_ERR(3u);

                //
                // Zusammenfassung:
                //     The call succeeded and *pNumFramesToRead is 0, indicating that no capture data
                //     is available to be read.
                public static readonly HRESULT AUDCLNT_S_BUFFER_EMPTY = AUDCLNT_SUCCESS(1u);

                //
                // Zusammenfassung:
                //     The IAudioClient::Start method has not been called for this stream.
                public static readonly HRESULT AUDCLNT_S_POSITION_STALLED = AUDCLNT_SUCCESS(3u);

                public static readonly HRESULT AUDCLNT_S_THREAD_ALREADY_REGISTERED = AUDCLNT_SUCCESS(2u);

                public static readonly HRESULT DRT_E_TIMEOUT = Make(severe: true, 98u, 4097u);

                public static readonly HRESULT DRT_E_INVALID_KEY_SIZE = Make(severe: true, 98u, 4098u);

                public static readonly HRESULT DRT_E_INVALID_CERT_CHAIN = Make(severe: true, 98u, 4100u);

                public static readonly HRESULT DRT_E_INVALID_MESSAGE = Make(severe: true, 98u, 4101u);

                public static readonly HRESULT DRT_E_NO_MORE = Make(severe: true, 98u, 4102u);

                public static readonly HRESULT DRT_E_INVALID_MAX_ADDRESSES = Make(severe: true, 98u, 4103u);

                public static readonly HRESULT DRT_E_SEARCH_IN_PROGRESS = Make(severe: true, 98u, 4104u);

                public static readonly HRESULT DRT_E_INVALID_KEY = Make(severe: true, 98u, 4105u);

                public static readonly HRESULT DRT_S_RETRY = Make(severe: false, 98u, 4112u);

                public static readonly HRESULT DRT_E_INVALID_MAX_ENDPOINTS = Make(severe: true, 98u, 4113u);

                public static readonly HRESULT DRT_E_INVALID_SEARCH_RANGE = Make(severe: true, 98u, 4114u);

                public static readonly HRESULT DRT_E_INVALID_PORT = Make(severe: true, 98u, 8192u);

                public static readonly HRESULT DRT_E_INVALID_TRANSPORT_PROVIDER = Make(severe: true, 98u, 8193u);

                public static readonly HRESULT DRT_E_INVALID_SECURITY_PROVIDER = Make(severe: true, 98u, 8194u);

                public static readonly HRESULT DRT_E_STILL_IN_USE = Make(severe: true, 98u, 8195u);

                public static readonly HRESULT DRT_E_INVALID_BOOTSTRAP_PROVIDER = Make(severe: true, 98u, 8196u);

                public static readonly HRESULT DRT_E_INVALID_ADDRESS = Make(severe: true, 98u, 8197u);

                public static readonly HRESULT DRT_E_INVALID_SCOPE = Make(severe: true, 98u, 8198u);

                public static readonly HRESULT DRT_E_TRANSPORT_SHUTTING_DOWN = Make(severe: true, 98u, 8199u);

                public static readonly HRESULT DRT_E_NO_ADDRESSES_AVAILABLE = Make(severe: true, 98u, 8200u);

                public static readonly HRESULT DRT_E_DUPLICATE_KEY = Make(severe: true, 98u, 8201u);

                public static readonly HRESULT DRT_E_TRANSPORTPROVIDER_IN_USE = Make(severe: true, 98u, 8202u);

                public static readonly HRESULT DRT_E_TRANSPORTPROVIDER_NOT_ATTACHED = Make(severe: true, 98u, 8203u);

                public static readonly HRESULT DRT_E_SECURITYPROVIDER_IN_USE = Make(severe: true, 98u, 8204u);

                public static readonly HRESULT DRT_E_SECURITYPROVIDER_NOT_ATTACHED = Make(severe: true, 98u, 8205u);

                public static readonly HRESULT DRT_E_BOOTSTRAPPROVIDER_IN_USE = Make(severe: true, 98u, 8206u);

                public static readonly HRESULT DRT_E_BOOTSTRAPPROVIDER_NOT_ATTACHED = Make(severe: true, 98u, 8207u);

                public static readonly HRESULT DRT_E_TRANSPORT_ALREADY_BOUND = Make(severe: true, 98u, 8449u);

                public static readonly HRESULT DRT_E_TRANSPORT_NOT_BOUND = Make(severe: true, 98u, 8450u);

                public static readonly HRESULT DRT_E_TRANSPORT_UNEXPECTED = Make(severe: true, 98u, 8451u);

                public static readonly HRESULT DRT_E_TRANSPORT_INVALID_ARGUMENT = Make(severe: true, 98u, 8452u);

                public static readonly HRESULT DRT_E_TRANSPORT_NO_DEST_ADDRESSES = Make(severe: true, 98u, 8453u);

                public static readonly HRESULT DRT_E_TRANSPORT_EXECUTING_CALLBACK = Make(severe: true, 98u, 8454u);

                public static readonly HRESULT DRT_E_TRANSPORT_ALREADY_EXISTS_FOR_SCOPE = Make(severe: true, 98u, 8455u);

                public static readonly HRESULT DRT_E_INVALID_SETTINGS = Make(severe: true, 98u, 8456u);

                public static readonly HRESULT DRT_E_INVALID_SEARCH_INFO = Make(severe: true, 98u, 8457u);

                public static readonly HRESULT DRT_E_FAULTED = Make(severe: true, 98u, 8458u);

                public static readonly HRESULT DRT_E_TRANSPORT_STILL_BOUND = Make(severe: true, 98u, 8459u);

                public static readonly HRESULT DRT_E_INSUFFICIENT_BUFFER = Make(severe: true, 98u, 8460u);

                public static readonly HRESULT DRT_E_INVALID_INSTANCE_PREFIX = Make(severe: true, 98u, 8461u);

                public static readonly HRESULT DRT_E_INVALID_SECURITY_MODE = Make(severe: true, 98u, 8462u);

                public static readonly HRESULT DRT_E_CAPABILITY_MISMATCH = Make(severe: true, 98u, 8463u);

                //
                // Zusammenfassung:
                //     The request was cancelled.
                public const int E_IMAPI_REQUEST_CANCELLED = -1062600702;

                //
                // Zusammenfassung:
                //     The request requires a current disc recorder to be selected.
                public const int E_IMAPI_RECORDER_REQUIRED = -1062600701;

                //
                // Zusammenfassung:
                //     The requested write speed was not supported by the drive and the speed was adjusted.
                public const int S_IMAPI_SPEEDADJUSTED = 11141124;

                //
                // Zusammenfassung:
                //     The requested rotation type was not supported by the drive and the rotation type
                //     was adjusted.
                public const int S_IMAPI_ROTATIONADJUSTED = 11141125;

                //
                // Zusammenfassung:
                //     The requested write speed and rotation type were not supported by the drive and
                //     they were both adjusted.
                public const int S_IMAPI_BOTHADJUSTED = 11141126;

                //
                // Zusammenfassung:
                //     The disc did not pass burn verification and may contain corrupt data or be unusable.
                public const int E_IMAPI_BURN_VERIFICATION_FAILED = -1062600697;

                //
                // Zusammenfassung:
                //     The device accepted the command, but returned sense data, indicating an error.
                public const int S_IMAPI_COMMAND_HAS_SENSE_DATA = 11141632;

                //
                // Zusammenfassung:
                //     The device reported that the requested mode page (and type) is not present.
                public const int E_IMAPI_RECORDER_NO_SUCH_MODE_PAGE = -1062600191;

                //
                // Zusammenfassung:
                //     There is no media in the device.
                public const int E_IMAPI_RECORDER_MEDIA_NO_MEDIA = -1062600190;

                //
                // Zusammenfassung:
                //     The media is not compatible or of unknown physical format.
                public const int E_IMAPI_RECORDER_MEDIA_INCOMPATIBLE = -1062600189;

                //
                // Zusammenfassung:
                //     The media is inserted upside down.
                public const int E_IMAPI_RECORDER_MEDIA_UPSIDE_DOWN = -1062600188;

                //
                // Zusammenfassung:
                //     The drive reported that it is in the process of becoming ready. Please try the
                //     request again later.
                public const int E_IMAPI_RECORDER_MEDIA_BECOMING_READY = -1062600187;

                //
                // Zusammenfassung:
                //     The media is currently being formatted. Please wait for the format to complete
                //     before attempting to use the media.
                public const int E_IMAPI_RECORDER_MEDIA_FORMAT_IN_PROGRESS = -1062600186;

                //
                // Zusammenfassung:
                //     The drive reported that it is performing a long-running operation, such as finishing
                //     a write. The drive may be unusable for a long period of time.
                public const int E_IMAPI_RECORDER_MEDIA_BUSY = -1062600185;

                //
                // Zusammenfassung:
                //     The drive reported that the combination of parameters provided in the mode page
                //     for a MODE SELECT command were not supported.
                public const int E_IMAPI_RECORDER_INVALID_MODE_PARAMETERS = -1062600184;

                //
                // Zusammenfassung:
                //     The drive reported that the media is write protected.
                public const int E_IMAPI_RECORDER_MEDIA_WRITE_PROTECTED = -1062600183;

                //
                // Zusammenfassung:
                //     The feature page requested is not supported by the device.
                public const int E_IMAPI_RECORDER_NO_SUCH_FEATURE = -1062600182;

                //
                // Zusammenfassung:
                //     The feature page requested is supported, but is not marked as current.
                public const int E_IMAPI_RECORDER_FEATURE_IS_NOT_CURRENT = -1062600181;

                //
                // Zusammenfassung:
                //     The drive does not support the GET CONFIGURATION command.
                public const int E_IMAPI_RECORDER_GET_CONFIGURATION_NOT_SUPPORTED = -1062600180;

                //
                // Zusammenfassung:
                //     The device failed to accept the command within the timeout period. This may be
                //     caused by the device having entered an inconsistent state, or the timeout value
                //     for the command may need to be increased.
                public const int E_IMAPI_RECORDER_COMMAND_TIMEOUT = -1062600179;

                //
                // Zusammenfassung:
                //     The DVD structure is not present. This may be caused by incompatible drive/medium
                //     used.
                public const int E_IMAPI_RECORDER_DVD_STRUCTURE_NOT_PRESENT = -1062600178;

                //
                // Zusammenfassung:
                //     The media's speed is incompatible with the device. This may be caused by using
                //     higher or lower speed media than the range of speeds supported by the device.
                public const int E_IMAPI_RECORDER_MEDIA_SPEED_MISMATCH = -1062600177;

                //
                // Zusammenfassung:
                //     The device associated with this recorder during the last operation has been exclusively
                //     locked, causing this operation to failed.
                public const int E_IMAPI_RECORDER_LOCKED = -1062600176;

                //
                // Zusammenfassung:
                //     The client name is not valid.
                public const int E_IMAPI_RECORDER_CLIENT_NAME_IS_NOT_VALID = -1062600175;

                //
                // Zusammenfassung:
                //     The media is not formatted. Please format the media before attempting to use
                //     it.
                public const int E_IMAPI_RECORDER_MEDIA_NOT_FORMATTED = -1062600174;

                //
                // Zusammenfassung:
                //     The device reported unexpected or invalid data for a command.
                public const int E_IMAPI_RECORDER_INVALID_RESPONSE_FROM_DEVICE = -1062599937;

                //
                // Zusammenfassung:
                //     The write failed because the drive did not receive data quickly enough to continue
                //     writing. Moving the source data to the local computer, reducing the write speed,
                //     or enabling a "buffer underrun free" setting may resolve this issue.
                public const int E_IMAPI_LOSS_OF_STREAMING = -1062599936;

                //
                // Zusammenfassung:
                //     The write failed because the drive returned error information that could not
                //     be recovered from.
                public const int E_IMAPI_UNEXPECTED_RESPONSE_FROM_DEVICE = -1062599935;

                //
                // Zusammenfassung:
                //     There is no write operation currently in progress.
                public const int S_IMAPI_WRITE_NOT_IN_PROGRESS = 11141890;

                //
                // Zusammenfassung:
                //     There is currently a write operation in progress.
                public const int E_IMAPI_DF2DATA_WRITE_IN_PROGRESS = -1062599680;

                //
                // Zusammenfassung:
                //     There is no write operation currently in progress.
                public const int E_IMAPI_DF2DATA_WRITE_NOT_IN_PROGRESS = -1062599679;

                //
                // Zusammenfassung:
                //     The requested operation is only valid with supported media.
                public const int E_IMAPI_DF2DATA_INVALID_MEDIA_STATE = -1062599678;

                //
                // Zusammenfassung:
                //     The provided stream to write is not supported.
                public const int E_IMAPI_DF2DATA_STREAM_NOT_SUPPORTED = -1062599677;

                //
                // Zusammenfassung:
                //     The provided stream to write is too large for the currently inserted media.
                public const int E_IMAPI_DF2DATA_STREAM_TOO_LARGE_FOR_CURRENT_MEDIA = -1062599676;

                //
                // Zusammenfassung:
                //     Overwriting non-blank media is not allowed without the ForceOverwrite property
                //     set to VARIANT_TRUE.
                public const int E_IMAPI_DF2DATA_MEDIA_NOT_BLANK = -1062599675;

                //
                // Zusammenfassung:
                //     The current media type is unsupported.
                public const int E_IMAPI_DF2DATA_MEDIA_IS_NOT_SUPPORTED = -1062599674;

                //
                // Zusammenfassung:
                //     This device does not support the operations required by this disc format.
                public const int E_IMAPI_DF2DATA_RECORDER_NOT_SUPPORTED = -1062599673;

                //
                // Zusammenfassung:
                //     The client name is not valid.
                public const int E_IMAPI_DF2DATA_CLIENT_NAME_IS_NOT_VALID = -1062599672;

                //
                // Zusammenfassung:
                //     There is currently a write operation in progress.
                public const int E_IMAPI_DF2TAO_WRITE_IN_PROGRESS = -1062599424;

                //
                // Zusammenfassung:
                //     There is no write operation currently in progress.
                public const int E_IMAPI_DF2TAO_WRITE_NOT_IN_PROGRESS = -1062599423;

                //
                // Zusammenfassung:
                //     The requested operation is only valid when media has been "prepared".
                public const int E_IMAPI_DF2TAO_MEDIA_IS_NOT_PREPARED = -1062599422;

                //
                // Zusammenfassung:
                //     The requested operation is not valid when media has been "prepared" but not released.
                public const int E_IMAPI_DF2TAO_MEDIA_IS_PREPARED = -1062599421;

                //
                // Zusammenfassung:
                //     The property cannot be changed once the media has been written to.
                public const int E_IMAPI_DF2TAO_PROPERTY_FOR_BLANK_MEDIA_ONLY = -1062599420;

                //
                // Zusammenfassung:
                //     The table of contents cannot be retrieved from an empty disc.
                public const int E_IMAPI_DF2TAO_TABLE_OF_CONTENTS_EMPTY_DISC = -1062599419;

                //
                // Zusammenfassung:
                //     Only blank CD-R/RW media is supported.
                public const int E_IMAPI_DF2TAO_MEDIA_IS_NOT_BLANK = -1062599418;

                //
                // Zusammenfassung:
                //     Only blank CD-R/RW media is supported.
                public const int E_IMAPI_DF2TAO_MEDIA_IS_NOT_SUPPORTED = -1062599417;

                //
                // Zusammenfassung:
                //     CD-R and CD-RW media support a maximum of 99 audio tracks.
                public const int E_IMAPI_DF2TAO_TRACK_LIMIT_REACHED = -1062599416;

                //
                // Zusammenfassung:
                //     There is not enough space left on the media to add the provided audio track.
                public const int E_IMAPI_DF2TAO_NOT_ENOUGH_SPACE = -1062599415;

                //
                // Zusammenfassung:
                //     You cannot prepare the media until you choose a recorder to use.
                public const int E_IMAPI_DF2TAO_NO_RECORDER_SPECIFIED = -1062599414;

                //
                // Zusammenfassung:
                //     The ISRC provided is not valid.
                public const int E_IMAPI_DF2TAO_INVALID_ISRC = -1062599413;

                //
                // Zusammenfassung:
                //     The Media Catalog Number provided is not valid.
                public const int E_IMAPI_DF2TAO_INVALID_MCN = -1062599412;

                //
                // Zusammenfassung:
                //     The provided audio stream is not valid.
                public const int E_IMAPI_DF2TAO_STREAM_NOT_SUPPORTED = -1062599411;

                //
                // Zusammenfassung:
                //     This device does not support the operations required by this disc format.
                public const int E_IMAPI_DF2TAO_RECORDER_NOT_SUPPORTED = -1062599410;

                //
                // Zusammenfassung:
                //     The client name is not valid.
                public const int E_IMAPI_DF2TAO_CLIENT_NAME_IS_NOT_VALID = -1062599409;

                //
                // Zusammenfassung:
                //     There is currently a write operation in progress.
                public const int E_IMAPI_DF2RAW_WRITE_IN_PROGRESS = -1062599168;

                //
                // Zusammenfassung:
                //     There is no write operation currently in progress.
                public const int E_IMAPI_DF2RAW_WRITE_NOT_IN_PROGRESS = -1062599167;

                //
                // Zusammenfassung:
                //     The requested operation is only valid when media has been "prepared".
                public const int E_IMAPI_DF2RAW_MEDIA_IS_NOT_PREPARED = -1062599166;

                //
                // Zusammenfassung:
                //     The requested operation is not valid when media has been "prepared" but not released.
                public const int E_IMAPI_DF2RAW_MEDIA_IS_PREPARED = -1062599165;

                //
                // Zusammenfassung:
                //     The client name is not valid.
                public const int E_IMAPI_DF2RAW_CLIENT_NAME_IS_NOT_VALID = -1062599164;

                //
                // Zusammenfassung:
                //     Only blank CD-R/RW media is supported.
                public const int E_IMAPI_DF2RAW_MEDIA_IS_NOT_BLANK = -1062599162;

                //
                // Zusammenfassung:
                //     Only blank CD-R/RW media is supported.
                public const int E_IMAPI_DF2RAW_MEDIA_IS_NOT_SUPPORTED = -1062599161;

                //
                // Zusammenfassung:
                //     There is not enough space on the media to add the provided session.
                public const int E_IMAPI_DF2RAW_NOT_ENOUGH_SPACE = -1062599159;

                //
                // Zusammenfassung:
                //     You cannot prepare the media until you choose a recorder to use.
                public const int E_IMAPI_DF2RAW_NO_RECORDER_SPECIFIED = -1062599158;

                //
                // Zusammenfassung:
                //     The provided audio stream is not valid.
                public const int E_IMAPI_DF2RAW_STREAM_NOT_SUPPORTED = -1062599155;

                //
                // Zusammenfassung:
                //     The requested data block type is not supported by the current device.
                public const int E_IMAPI_DF2RAW_DATA_BLOCK_TYPE_NOT_SUPPORTED = -1062599154;

                //
                // Zusammenfassung:
                //     The stream does not contain a sufficient number of sectors in the leadin for
                //     the current media.
                public const int E_IMAPI_DF2RAW_STREAM_LEADIN_TOO_SHORT = -1062599153;

                //
                // Zusammenfassung:
                //     This device does not support the operations required by this disc format.
                public const int E_IMAPI_DF2RAW_RECORDER_NOT_SUPPORTED = -1062599152;

                //
                // Zusammenfassung:
                //     The format is currently using the disc recorder for an erase operation. Please
                //     wait for the erase to complete before attempting to set or clear the current
                //     disc recorder.
                public const int E_IMAPI_ERASE_RECORDER_IN_USE = -2136340224;

                //
                // Zusammenfassung:
                //     The erase format only supports one recorder. You must clear the current recorder
                //     before setting a new one.
                public const int E_IMAPI_ERASE_ONLY_ONE_RECORDER_SUPPORTED = -2136340223;

                //
                // Zusammenfassung:
                //     The drive did not report sufficient data for a READ DISC INFORMATION command.
                //     The drive may not be supported, or the media may not be correct.
                public const int E_IMAPI_ERASE_DISC_INFORMATION_TOO_SMALL = -2136340222;

                //
                // Zusammenfassung:
                //     The drive did not report sufficient data for a MODE SENSE (page 0x2A) command.
                //     The drive may not be supported, or the media may not be correct.
                public const int E_IMAPI_ERASE_MODE_PAGE_2A_TOO_SMALL = -2136340221;

                //
                // Zusammenfassung:
                //     The drive reported that the media is not erasable.
                public const int E_IMAPI_ERASE_MEDIA_IS_NOT_ERASABLE = -2136340220;

                //
                // Zusammenfassung:
                //     The drive failed the erase command.
                public const int E_IMAPI_ERASE_DRIVE_FAILED_ERASE_COMMAND = -2136340219;

                //
                // Zusammenfassung:
                //     The drive did not complete the erase in one hour. The drive may require a power
                //     cycle, media removal, or other manual intervention to resume proper operation.
                public const int E_IMAPI_ERASE_TOOK_LONGER_THAN_ONE_HOUR = -2136340218;

                //
                // Zusammenfassung:
                //     The drive returned an unexpected error during the erase. The the media may be
                //     unusable, the erase may be complete, or the drive may still be in the process
                //     of erasing the disc.
                public const int E_IMAPI_ERASE_UNEXPECTED_DRIVE_RESPONSE_DURING_ERASE = -2136340217;

                //
                // Zusammenfassung:
                //     The drive returned an error for a START UNIT (spinup) command. Manual intervention
                //     may be required.
                public const int E_IMAPI_ERASE_DRIVE_FAILED_SPINUP_COMMAND = -2136340216;

                //
                // Zusammenfassung:
                //     The current media type is unsupported.
                public const int E_IMAPI_ERASE_MEDIA_IS_NOT_SUPPORTED = -1062598391;

                //
                // Zusammenfassung:
                //     This device does not support the operations required by this disc format.
                public const int E_IMAPI_ERASE_RECORDER_NOT_SUPPORTED = -1062598390;

                //
                // Zusammenfassung:
                //     The client name is not valid.
                public const int E_IMAPI_ERASE_CLIENT_NAME_IS_NOT_VALID = -1062598389;

                //
                // Zusammenfassung:
                //     The image has become read-only from a call to CreateResultImage(). The object
                //     can no longer be modified.
                public const int E_IMAPI_RAW_IMAGE_IS_READ_ONLY = -2136339968;

                //
                // Zusammenfassung:
                //     No more tracks may be added, as CD media is restricted to track numbers between
                //     1 and 99.
                public const int E_IMAPI_RAW_IMAGE_TOO_MANY_TRACKS = -2136339967;

                //
                // Zusammenfassung:
                //     The requested sector type is not supported.
                public const int E_IMAPI_RAW_IMAGE_SECTOR_TYPE_NOT_SUPPORTED = -2136339966;

                //
                // Zusammenfassung:
                //     Tracks must be added to the image before using this function.
                public const int E_IMAPI_RAW_IMAGE_NO_TRACKS = -2136339965;

                //
                // Zusammenfassung:
                //     Tracks may not be added to the image prior to the use of this function.
                public const int E_IMAPI_RAW_IMAGE_TRACKS_ALREADY_ADDED = -2136339964;

                //
                // Zusammenfassung:
                //     Adding the track would result in exceeding the limit for the start of the leadout.
                public const int E_IMAPI_RAW_IMAGE_INSUFFICIENT_SPACE = -2136339963;

                //
                // Zusammenfassung:
                //     Adding the track index would result in exceeding the 99 index limit.
                public const int E_IMAPI_RAW_IMAGE_TOO_MANY_TRACK_INDEXES = -2136339962;

                //
                // Zusammenfassung:
                //     The specified LBA offset is not in the list of track indexes.
                public const int E_IMAPI_RAW_IMAGE_TRACK_INDEX_NOT_FOUND = -2136339961;

                //
                // Zusammenfassung:
                //     The specified LBA offset is already in the list of track indexes.
                public const int S_IMAPI_RAW_IMAGE_TRACK_INDEX_ALREADY_EXISTS = 11143688;

                //
                // Zusammenfassung:
                //     Index 1 (LBA offset zero) may not be cleared.
                public const int E_IMAPI_RAW_IMAGE_TRACK_INDEX_OFFSET_ZERO_CANNOT_BE_CLEARED = -2136339959;

                //
                // Zusammenfassung:
                //     Each index must have a minimum size of ten sectors.
                public const int E_IMAPI_RAW_IMAGE_TRACK_INDEX_TOO_CLOSE_TO_OTHER_INDEX = -2136339958;

                //
                // Zusammenfassung:
                //     Gets the code portion of the Vanara.PInvoke.HRESULT.
                //
                // Wert:
                //     The code value (bits 0-15).
                public int Code => GetCode(_value);

                //
                // Zusammenfassung:
                //     Gets the facility portion of the Vanara.PInvoke.HRESULT.
                //
                // Wert:
                //     The facility value (bits 16-26).
                public FacilityCode Facility => GetFacility(_value);

                //
                // Zusammenfassung:
                //     Gets a value indicating whether this Vanara.PInvoke.HRESULT is a failure (Severity
                //     bit 31 equals 1).
                //
                // Wert:
                //     true if failed; otherwise, false.
                public bool Failed => _value < 0;

                //
                // Zusammenfassung:
                //     Gets the severity level of the Vanara.PInvoke.HRESULT.
                //
                // Wert:
                //     The severity level.
                public SeverityLevel Severity => GetSeverity(_value);

                //
                // Zusammenfassung:
                //     Gets a value indicating whether this Vanara.PInvoke.HRESULT is a success (Severity
                //     bit 31 equals 0).
                //
                // Wert:
                //     true if succeeded; otherwise, false.
                public bool Succeeded => _value >= 0;

                //
                // Zusammenfassung:
                //     Initializes a new instance of the Vanara.PInvoke.HRESULT structure.
                //
                // Parameter:
                //   rawValue:
                //     The raw HRESULT value.
                public HRESULT(int rawValue)
                {
                    _value = rawValue;
                }

                //
                // Zusammenfassung:
                //     Initializes a new instance of the Vanara.PInvoke.HRESULT structure.
                //
                // Parameter:
                //   rawValue:
                //     The raw HRESULT value.
                public HRESULT(uint rawValue)
                {
                    _value = (int)rawValue;
                }

                //
                // Zusammenfassung:
                //     Performs an explicit conversion from System.Boolean to Vanara.PInvoke.HRESULT.
                //
                // Parameter:
                //   value:
                //     if set to true returns S_OK; otherwise S_FALSE.
                //
                // Rückgabewerte:
                //     The result of the conversion.
                public static explicit operator HRESULT(bool value)
                {
                    return (!value) ? 1 : 0;
                }

                //
                // Zusammenfassung:
                //     Performs an explicit conversion from Vanara.PInvoke.HRESULT to System.Int32.
                //
                // Parameter:
                //   value:
                //     The value.
                //
                // Rückgabewerte:
                //     The result of the conversion.
                public static explicit operator int(HRESULT value)
                {
                    return value._value;
                }

                //
                // Zusammenfassung:
                //     Performs an explicit conversion from Vanara.PInvoke.HRESULT to System.UInt32.
                //
                // Parameter:
                //   value:
                //     The value.
                //
                // Rückgabewerte:
                //     The result of the conversion.
                public static explicit operator uint(HRESULT value)
                {
                    return (uint)value._value;
                }

                //
                // Zusammenfassung:
                //     Tries to extract a HRESULT from an exception.
                //
                // Parameter:
                //   exception:
                //     The exception.
                //
                // Rückgabewerte:
                //     The error. If undecipherable, E_FAIL is returned.
                public static HRESULT FromException(Exception exception)
                {
                    Win32Exception ex = exception as Win32Exception;
                    if (ex != null)
                    {
                        return new Win32Error((uint)ex.NativeErrorCode).ToHRESULT();
                    }

                    Win32Exception ex2 = exception.InnerException as Win32Exception;
                    if (ex2 != null)
                    {
                        return new Win32Error((uint)ex2.NativeErrorCode).ToHRESULT();
                    }

                    if (exception.HResult != 0)
                    {
                        return new HRESULT(exception.HResult);
                    }

                    if (exception.InnerException != null && exception.InnerException.HResult != 0)
                    {
                        return new HRESULT(exception.InnerException.HResult);
                    }

                    return -2147467259;
                }

                //
                // Zusammenfassung:
                //     Gets the code value from a 32-bit value.
                //
                // Parameter:
                //   hresult:
                //     The 32-bit raw HRESULT value.
                //
                // Rückgabewerte:
                //     The code value (bits 0-15).
                public static int GetCode(int hresult)
                {
                    return hresult & 0xFFFF;
                }

                //
                // Zusammenfassung:
                //     Gets the facility value from a 32-bit value.
                //
                // Parameter:
                //   hresult:
                //     The 32-bit raw HRESULT value.
                //
                // Rückgabewerte:
                //     The facility value (bits 16-26).
                public static FacilityCode GetFacility(int hresult)
                {
                    return (FacilityCode)(((long)hresult & 0x7FF0000L) >> 16);
                }

                //
                // Zusammenfassung:
                //     Gets the severity value from a 32-bit value.
                //
                // Parameter:
                //   hresult:
                //     The 32-bit raw HRESULT value.
                //
                // Rückgabewerte:
                //     The severity value (bit 31).
                public static SeverityLevel GetSeverity(int hresult)
                {
                    return (SeverityLevel)((hresult & 2147483648u) >> 31);
                }

                //
                // Zusammenfassung:
                //     Performs an implicit conversion from System.Int32 to Vanara.PInvoke.HRESULT.
                //
                // Parameter:
                //   value:
                //     The value.
                //
                // Rückgabewerte:
                //     The result of the conversion.
                public static implicit operator HRESULT(int value)
                {
                    return new HRESULT(value);
                }

                //
                // Zusammenfassung:
                //     Performs an implicit conversion from System.UInt32 to Vanara.PInvoke.HRESULT.
                //
                // Parameter:
                //   value:
                //     The value.
                //
                // Rückgabewerte:
                //     The resulting Vanara.PInvoke.HRESULT instance from the conversion.
                public static implicit operator HRESULT(uint value)
                {
                    return new HRESULT(value);
                }

                //
                // Zusammenfassung:
                //     Maps an NT Status value to an HRESULT value.
                //
                // Parameter:
                //   err:
                //     The NT Status value.
                //
                // Rückgabewerte:
                //     The HRESULT value.
                public static HRESULT HRESULT_FROM_NT(NTStatus err)
                {
                    return err.ToHRESULT();
                }

                //
                // Zusammenfassung:
                //     Maps a system error code to an HRESULT value.
                //
                // Parameter:
                //   err:
                //     The system error code.
                //
                // Rückgabewerte:
                //     The HRESULT value.
                public static HRESULT HRESULT_FROM_WIN32(Win32Error err)
                {
                    return err.ToHRESULT();
                }

                //
                // Zusammenfassung:
                //     Creates a new Vanara.PInvoke.HRESULT from provided values.
                //
                // Parameter:
                //   severe:
                //     if set to false, sets the severity bit to 1.
                //
                //   facility:
                //     The facility.
                //
                //   code:
                //     The code.
                //
                // Rückgabewerte:
                //     The resulting Vanara.PInvoke.HRESULT.
                public static HRESULT Make(bool severe, FacilityCode facility, uint code)
                {
                    return Make(severe, (uint)facility, code);
                }

                //
                // Zusammenfassung:
                //     Creates a new Vanara.PInvoke.HRESULT from provided values.
                //
                // Parameter:
                //   severe:
                //     if set to false, sets the severity bit to 1.
                //
                //   facility:
                //     The facility.
                //
                //   code:
                //     The code.
                //
                // Rückgabewerte:
                //     The resulting Vanara.PInvoke.HRESULT.
                public static HRESULT Make(bool severe, uint facility, uint code)
                {
                    return new HRESULT((severe ? int.MinValue : 0) | (int)(facility << 16) | (int)code);
                }

                //
                // Zusammenfassung:
                //     Implements the operator !=.
                //
                // Parameter:
                //   hrLeft:
                //     The first Vanara.PInvoke.HRESULT.
                //
                //   hrRight:
                //     The second Vanara.PInvoke.HRESULT.
                //
                // Rückgabewerte:
                //     The result of the operator.
                public static bool operator !=(HRESULT hrLeft, HRESULT hrRight)
                {
                    return !(hrLeft == hrRight);
                }

                //
                // Zusammenfassung:
                //     Implements the operator !=.
                //
                // Parameter:
                //   hrLeft:
                //     The first Vanara.PInvoke.HRESULT.
                //
                //   hrRight:
                //     The second System.Int32.
                //
                // Rückgabewerte:
                //     The result of the operator.
                public static bool operator !=(HRESULT hrLeft, int hrRight)
                {
                    return !(hrLeft == hrRight);
                }

                //
                // Zusammenfassung:
                //     Implements the operator !=.
                //
                // Parameter:
                //   hrLeft:
                //     The first Vanara.PInvoke.HRESULT.
                //
                //   hrRight:
                //     The second System.UInt32.
                //
                // Rückgabewerte:
                //     The result of the operator.
                public static bool operator !=(HRESULT hrLeft, uint hrRight)
                {
                    return !(hrLeft == hrRight);
                }

                //
                // Zusammenfassung:
                //     Implements the operator ==.
                //
                // Parameter:
                //   hrLeft:
                //     The first Vanara.PInvoke.HRESULT.
                //
                //   hrRight:
                //     The second Vanara.PInvoke.HRESULT.
                //
                // Rückgabewerte:
                //     The result of the operator.
                public static bool operator ==(HRESULT hrLeft, HRESULT hrRight)
                {
                    return hrLeft.Equals(hrRight);
                }

                //
                // Zusammenfassung:
                //     Implements the operator ==.
                //
                // Parameter:
                //   hrLeft:
                //     The first Vanara.PInvoke.HRESULT.
                //
                //   hrRight:
                //     The second System.Int32.
                //
                // Rückgabewerte:
                //     The result of the operator.
                public static bool operator ==(HRESULT hrLeft, int hrRight)
                {
                    return hrLeft.Equals(hrRight);
                }

                //
                // Zusammenfassung:
                //     Implements the operator ==.
                //
                // Parameter:
                //   hrLeft:
                //     The first Vanara.PInvoke.HRESULT.
                //
                //   hrRight:
                //     The second System.UInt32.
                //
                // Rückgabewerte:
                //     The result of the operator.
                public static bool operator ==(HRESULT hrLeft, uint hrRight)
                {
                    return hrLeft.Equals(hrRight);
                }

                //
                // Zusammenfassung:
                //     If the supplied raw HRESULT value represents a failure, throw the associated
                //     System.Exception with the optionally supplied message.
                //
                // Parameter:
                //   hresult:
                //     The 32-bit raw HRESULT value.
                //
                //   message:
                //     The optional message to assign to the System.Exception.
                [DebuggerStepThrough]
                public static void ThrowIfFailed(int hresult, string message = null)
                {
                    new HRESULT(hresult).ThrowIfFailed(message);
                }

                //
                // Zusammenfassung:
                //     Compares the current object with another object of the same type.
                //
                // Parameter:
                //   other:
                //     An object to compare with this object.
                //
                // Rückgabewerte:
                //     A value that indicates the relative order of the objects being compared. The
                //     return value has the following meanings: Value Meaning Less than zero This object
                //     is less than the other parameter.Zero This object is equal to other. Greater
                //     than zero This object is greater than other.
                public int CompareTo(HRESULT other)
                {
                    return _value.CompareTo(other._value);
                }

                //
                // Zusammenfassung:
                //     Compares the current instance with another object of the same type and returns
                //     an integer that indicates whether the current instance precedes, follows, or
                //     occurs in the same position in the sort order as the other object.
                //
                // Parameter:
                //   obj:
                //     An object to compare with this instance.
                //
                // Rückgabewerte:
                //     A value that indicates the relative order of the objects being compared. The
                //     return value has these meanings: Value Meaning Less than zero This instance precedes
                //     obj in the sort order. Zero This instance occurs in the same position in the
                //     sort order as obj. Greater than zero This instance follows obj in the sort order.
                public int CompareTo(object obj)
                {
                    int? num = ValueFromObj(obj);
                    if (!num.HasValue)
                    {
                        throw new ArgumentException("Object cannot be converted to a UInt32 value for comparison.", "obj");
                    }

                    return _value.CompareTo(num.Value);
                }

                //
                // Zusammenfassung:
                //     Indicates whether the current object is equal to an System.Int32.
                //
                // Parameter:
                //   other:
                //     An object to compare with this object.
                //
                // Rückgabewerte:
                //     true if the current object is equal to the other parameter; otherwise, false.
                public bool Equals(int other)
                {
                    return other == _value;
                }

                //
                // Zusammenfassung:
                //     Indicates whether the current object is equal to an System.UInt32.
                //
                // Parameter:
                //   other:
                //     An object to compare with this object.
                //
                // Rückgabewerte:
                //     true if the current object is equal to the other parameter; otherwise, false.
                public bool Equals(uint other)
                {
                    return other == (uint)_value;
                }

                //
                // Zusammenfassung:
                //     Determines whether the specified System.Object, is equal to this instance.
                //
                // Parameter:
                //   obj:
                //     The System.Object to compare with this instance.
                //
                // Rückgabewerte:
                //     true if the specified System.Object is equal to this instance; otherwise, false.
                public override bool Equals(object obj)
                {
                    if (obj != null)
                    {
                        if (obj is HRESULT)
                        {
                            HRESULT other = (HRESULT)obj;
                            return Equals(other);
                        }

                        if (obj is int)
                        {
                            int other2 = (int)obj;
                            return Equals(other2);
                        }

                        if (obj is uint)
                        {
                            uint other3 = (uint)obj;
                            return Equals(other3);
                        }

                        return object.Equals(_value, ValueFromObj(obj));
                    }

                    return false;
                }

                //
                // Zusammenfassung:
                //     Indicates whether the current object is equal to another object of the same type.
                //
                // Parameter:
                //   other:
                //     An object to compare with this object.
                //
                // Rückgabewerte:
                //     true if the current object is equal to the other parameter; otherwise, false.
                public bool Equals(HRESULT other)
                {
                    return other._value == _value;
                }

                //
                // Zusammenfassung:
                //     Gets the .NET System.Exception associated with the HRESULT value and optionally
                //     adds the supplied message.
                //
                // Parameter:
                //   message:
                //     The optional message to assign to the System.Exception.
                //
                // Rückgabewerte:
                //     The associated System.Exception or null if this HRESULT is not a failure.
                [SecurityCritical]
                [SecuritySafeCritical]
                public Exception GetException(string message = null)
                {
                    if (!Failed)
                    {
                        return null;
                    }

                    Exception ex = Marshal.GetExceptionForHR(_value, new IntPtr(-1));
                    if (ex.GetType() == typeof(COMException))
                    {
                        if (Facility == FacilityCode.FACILITY_WIN32)
                        {
                            if (!string.IsNullOrEmpty(message))
                            {
                                return new Win32Exception(Code, message);
                            }

                            return new Win32Exception(Code);
                        }

                        return new COMException(message ?? ex.Message, _value);
                    }

                    if (!string.IsNullOrEmpty(message))
                    {
                        Type[] types = new Type[1]
                        {
                    typeof(string)
                        };
                        ConstructorInfo constructor = ex.GetType().GetConstructor(types);
                        if (null != constructor)
                        {
                            object[] parameters = new object[1]
                            {
                        message
                            };
                            ex = (constructor.Invoke(parameters) as Exception);
                        }
                    }

                    return ex;
                }

                //
                // Zusammenfassung:
                //     Returns a hash code for this instance.
                //
                // Rückgabewerte:
                //     A hash code for this instance, suitable for use in hashing algorithms and data
                //     structures like a hash table.
                public override int GetHashCode()
                {
                    return _value;
                }

                //
                // Zusammenfassung:
                //     If this Vanara.PInvoke.HRESULT represents a failure, throw the associated System.Exception
                //     with the optionally supplied message.
                //
                // Parameter:
                //   message:
                //     The optional message to assign to the System.Exception.
                [SecurityCritical]
                [SecuritySafeCritical]
                [DebuggerStepThrough]
                public void ThrowIfFailed(string message = null)
                {
                    Exception exception = GetException(message);
                    if (exception != null)
                    {
                        throw exception;
                    }
                }

                //
                // Zusammenfassung:
                //     Returns a System.String that represents this instance.
                //
                // Rückgabewerte:
                //     A System.String that represents this instance.
                public override string ToString()
                {
                    string fieldName = null;
                    if (!StaticFieldValueHash.TryGetFieldName<HRESULT, int>(_value, out fieldName) && Facility == FacilityCode.FACILITY_WIN32)
                    {
                        foreach (FieldInfo item in from fi in typeof(Win32Error).GetFields(BindingFlags.Static | BindingFlags.Public)
                                                   where fi.FieldType == typeof(uint)
                                                   select fi)
                        {
                            if ((HRESULT)(Win32Error)(uint)item.GetValue(null) == this)
                            {
                                fieldName = "HRESULT_FROM_WIN32(" + item.Name + ")";
                                break;
                            }
                        }
                    }

                    string text = FormatMessage((uint)_value);
                    return (fieldName ?? string.Format(System.Globalization.CultureInfo.InvariantCulture, "0x{0:X8}", new object[1]
                    {
                _value
                    })) + ((text == null) ? "" : (": " + text));
                }

                TypeCode IConvertible.GetTypeCode()
                {
                    return _value.GetTypeCode();
                }

                bool IConvertible.ToBoolean(IFormatProvider provider)
                {
                    return Succeeded;
                }

                byte IConvertible.ToByte(IFormatProvider provider)
                {
                    return ((IConvertible)_value).ToByte(provider);
                }

                char IConvertible.ToChar(IFormatProvider provider)
                {
                    throw new NotSupportedException();
                }

                DateTime IConvertible.ToDateTime(IFormatProvider provider)
                {
                    throw new NotSupportedException();
                }

                decimal IConvertible.ToDecimal(IFormatProvider provider)
                {
                    return ((IConvertible)_value).ToDecimal(provider);
                }

                double IConvertible.ToDouble(IFormatProvider provider)
                {
                    return ((IConvertible)_value).ToDouble(provider);
                }

                //
                // Zusammenfassung:
                //     Converts this error to an Vanara.PInvoke.HRESULT.
                //
                // Rückgabewerte:
                //     An equivalent Vanara.PInvoke.HRESULT.
                HRESULT IErrorProvider.ToHRESULT()
                {
                    return this;
                }

                short IConvertible.ToInt16(IFormatProvider provider)
                {
                    return ((IConvertible)_value).ToInt16(provider);
                }

                int IConvertible.ToInt32(IFormatProvider provider)
                {
                    return _value;
                }

                long IConvertible.ToInt64(IFormatProvider provider)
                {
                    return ((IConvertible)_value).ToInt64(provider);
                }

                sbyte IConvertible.ToSByte(IFormatProvider provider)
                {
                    return ((IConvertible)_value).ToSByte(provider);
                }

                float IConvertible.ToSingle(IFormatProvider provider)
                {
                    return ((IConvertible)_value).ToSingle(provider);
                }

                string IConvertible.ToString(IFormatProvider provider)
                {
                    return ToString();
                }

                object IConvertible.ToType(Type conversionType, IFormatProvider provider)
                {
                    return ((IConvertible)_value).ToType(conversionType, provider);
                }

                ushort IConvertible.ToUInt16(IFormatProvider provider)
                {
                    return ((IConvertible)(uint)_value).ToUInt16(provider);
                }

                uint IConvertible.ToUInt32(IFormatProvider provider)
                {
                    return (uint)_value;
                }

                ulong IConvertible.ToUInt64(IFormatProvider provider)
                {
                    return ((IConvertible)(uint)_value).ToUInt64(provider);
                }

                //
                // Zusammenfassung:
                //     Formats the message.
                //
                // Parameter:
                //   id:
                //     The error.
                //
                // Rückgabewerte:
                //     The string.
                internal static string FormatMessage(uint id)
                {
                    uint dwFlags = 4608u;
                    System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(1024);
                    do
                    {
                        if (FormatMessage(dwFlags, default(HINSTANCE), id, 0u, stringBuilder, (uint)stringBuilder.Capacity, (IntPtr)0) != 0)
                        {
                            return stringBuilder.ToString();
                        }

                        Win32Error lastError = Win32Error.GetLastError();
                        if (lastError == 317u || lastError == 15100u)
                        {
                            break;
                        }

                        if (lastError != 122u)
                        {
                            lastError.ThrowIfFailed();
                        }

                        stringBuilder.Capacity *= 2;
                    }
                    while (stringBuilder.Capacity < 16384);
                    return string.Empty;
                }

                [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
                private static extern int FormatMessage(uint dwFlags, HINSTANCE lpSource, uint dwMessageId, uint dwLanguageId, System.Text.StringBuilder lpBuffer, uint nSize, IntPtr Arguments);

                private static int? ValueFromObj(object obj)
                {
                    if (obj != null)
                    {
                        if (!(obj is int))
                        {
                            if (obj is uint)
                            {
                                return (int)(uint)obj;
                            }

                            System.ComponentModel.TypeConverter converter = System.ComponentModel.TypeDescriptor.GetConverter(obj);
                            if (!converter.CanConvertTo(typeof(int)))
                            {
                                return null;
                            }

                            return (int?)converter.ConvertTo(obj, typeof(int));
                        }

                        return (int)obj;
                    }

                    return null;
                }

                private static HRESULT AUDCLNT_ERR(uint n)
                {
                    return Make(severe: false, FacilityCode.FACILITY_AUDCLNT, n);
                }

                private static HRESULT AUDCLNT_SUCCESS(uint n)
                {
                    return Make(severe: true, FacilityCode.FACILITY_AUDCLNT, n);
                }
            }
        }
    }
}
