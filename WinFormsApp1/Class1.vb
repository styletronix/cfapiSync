Imports System
Imports System.Runtime.InteropServices
Imports Vanara.InteropServices
Imports Vanara.PInvoke.Kernel32
Imports USN = System.Int64


Module cldapi
    <DllImport("CldApi.dll", SetLastError:=False, ExactSpelling:=True)>
    Public Function CfConnectSyncRoot(
        <MarshalAs(UnmanagedType.LPWStr)> ByVal SyncRootPath As String,
        <[In], MarshalAs(UnmanagedType.LPArray)> ByVal CallbackTable As CF_CALLBACK_REGISTRATION(),
        <[In], [Optional]> ByVal CallbackContext As IntPtr, ByVal ConnectFlags As CF_CONNECT_FLAGS, <Out> ByRef ConnectionKey As CF_CONNECTION_KEY) As HRESULT
    End Function

    <StructLayout(LayoutKind.Sequential)>
    Public Structure CF_CALLBACK_REGISTRATION
        ''' <summary>The type of callback to be registered.</summary>
        Public Type As CF_CALLBACK_TYPE

        ''' <summary>A pointer to the callback function.</summary>
        <MarshalAs(UnmanagedType.FunctionPtr)>
        Public Callback As CF_CALLBACK

        ''' <summary>An instance of <c>CF_CALLBACK_REGISTRATION</c> that indicates the end of the registration list.</summary>
        Public Shared ReadOnly CF_CALLBACK_REGISTRATION_END As CF_CALLBACK_REGISTRATION = New CF_CALLBACK_REGISTRATION With {
            .Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NONE
        }
    End Structure

    Public Enum CF_CALLBACK_TYPE As UInteger
        ''' <summary>Callback to satisfy an I/O request, or a placeholder hydration request.</summary>
        CF_CALLBACK_TYPE_FETCH_DATA

        ''' <summary>Callback to validate placeholder data.</summary>
        CF_CALLBACK_TYPE_VALIDATE_DATA

        ''' <summary>Callback to cancel an ongoing placeholder hydration.</summary>
        CF_CALLBACK_TYPE_CANCEL_FETCH_DATA

        ''' <summary>Callback to request information about the contents of placeholder files.</summary>
        CF_CALLBACK_TYPE_FETCH_PLACEHOLDERS

        ''' <summary>Callback to cancel a request for the contents of placeholder files.</summary>
        CF_CALLBACK_TYPE_CANCEL_FETCH_PLACEHOLDERS

        ''' <summary>
        ''' Callback to inform the sync provider that a placeholder under one of its sync roots has been successfully opened for
        ''' read/write/delete access.
        ''' </summary>
        CF_CALLBACK_TYPE_NOTIFY_FILE_OPEN_COMPLETION

        ''' <summary>
        ''' Callback to inform the sync provider that a placeholder under one of its sync roots that has been previously opened for
        ''' read/write/delete access is now closed.
        ''' </summary>
        CF_CALLBACK_TYPE_NOTIFY_FILE_CLOSE_COMPLETION

        ''' <summary>Callback to inform the sync provider that a placeholder under one of its sync roots is about to be dehydrated.</summary>
        CF_CALLBACK_TYPE_NOTIFY_DEHYDRATE

        ''' <summary>Callback to inform the sync provider that a placeholder under one of its sync roots has been successfully dehydrated.</summary>
        CF_CALLBACK_TYPE_NOTIFY_DEHYDRATE_COMPLETION

        ''' <summary>Callback to inform the sync provider that a placeholder under one of its sync roots is about to be deleted.</summary>
        CF_CALLBACK_TYPE_NOTIFY_DELETE

        ''' <summary>Callback to inform the sync provider that a placeholder under one of its sync roots has been successfully deleted.</summary>
        CF_CALLBACK_TYPE_NOTIFY_DELETE_COMPLETION

        ''' <summary>
        ''' Callback to inform the sync provider that a placeholder under one of its sync roots is about to be renamed or moved.
        ''' </summary>
        CF_CALLBACK_TYPE_NOTIFY_RENAME

        ''' <summary>
        ''' Callback to inform the sync provider that a placeholder under one of its sync roots has been successfully renamed or moved.
        ''' </summary>
        CF_CALLBACK_TYPE_NOTIFY_RENAME_COMPLETION

        ''' <summary>No callback type.</summary>
        CF_CALLBACK_TYPE_NONE = &HFFFFFFFFUI
    End Enum

    <UnmanagedFunctionPointer(CallingConvention.Winapi)>
    Public Delegate Sub CF_CALLBACK(ByRef CallbackInfo As CF_CALLBACK_INFO, ByRef CallbackParameters As CF_CALLBACK_PARAMETERS)

    Public Structure CF_CALLBACK_INFO
        Public StructSize As UInteger
        Public CorrelationVector As IntPtr
        Public PriorityHint As Byte
        Public TransferKey As CF_TRANSFER_KEY
        Public NormalizedPath As String
        Public FileIdentityLength As UInteger
        Public FileIdentity As IntPtr
        Public FileSize As Long
        Public ProcessInfo As IntPtr
        Public FileId As Long
        Public SyncRootIdentity As IntPtr
        Public SyncRootFileId As Long
        Public VolumeSerialNumber As UInteger
        Public VolumeDosName As String
        Public VolumeGuidName As String
        Public CallbackContext As IntPtr
        Public ConnectionKey As CF_CONNECTION_KEY
        Public SyncRootIdentityLength As UInteger
        Public RequestKey As CF_REQUEST_KEY
    End Structure
End Module
