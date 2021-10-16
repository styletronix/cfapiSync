
Imports Microsoft.Windows.ProjFS

Public Class FS
    Implements IDisposable

    Private _instance As Microsoft.Windows.ProjFS.VirtualizationInstance
    Private _callbackMappings As New List(Of Microsoft.Windows.ProjFS.NotificationMapping)
    Private disposedValue As Boolean
    Private _Callback As Callback

    Private Property VirtualizationRootPath As String
    Public Sub New()
        Me.VirtualizationRootPath = "c:\VirtualTest"

        _callbackMappings.Add(New NotificationMapping(
                              NotificationType.NewFileCreated +
                              NotificationType.FileHandleClosedFileDeleted +
                              NotificationType.FileOverwritten +
                              NotificationType.FileHandleClosedFileModified +
                              NotificationType.FileRenamed +
                              NotificationType.PreDelete +
                              NotificationType.PreRename, String.Empty))

        _instance = New Microsoft.Windows.ProjFS.VirtualizationInstance(Me.VirtualizationRootPath, 0, 0, False, _callbackMappings)
        _Callback = New Callback("D:\TEMP", _instance)
        _instance.StartVirtualizing(_Callback)
    End Sub

    Public Sub removeLocalCache()
        For Each item In _Callback.MaintainedFiles.ToList
            Dim state = item.Value.OnDiskFileState

            If state.HasFlag(OnDiskFileState.HydratedPlaceholder) Then
                Debug.WriteLine("DeleteFile " & item.Value.relativeName)

                Dim failureReason As UpdateFailureCause
                _instance.DeleteFile(Me.VirtualizationRootPath & "\" & item.Value.relativeName, UpdateType.AllowDirtyData + UpdateType.AllowDirtyMetadata + UpdateType.AllowTombstone + UpdateType.AllowReadOnly, failureReason)

                Select Case failureReason
                    Case UpdateFailureCause.NoFailure
                        _Callback.MaintainedFiles.TryRemove(item.Key, Nothing)

                    Case Else
                        Debug.WriteLine(failureReason.ToString)
                End Select
            End If

        Next

        'For Each file In IO.Directory.EnumerateFileSystemEntries(Me.VirtualizationRootPath)
        '    Dim state As OnDiskFileState
        '    If Utils.TryGetOnDiskFileState(file, state) Then
        '        Debug.WriteLine(String.Format("{0} - {1}", file, state.ToString))
        '    End If
        'Next
    End Sub
    Public Class MaintainedFile
        Public Sub New(relativeName As String)
            Me.relativeName = relativeName
            Me.OnDiskFileState = OnDiskFileState.Placeholder
        End Sub

        Public Property relativeName As String
        Public Property OnDiskFileState As Microsoft.Windows.ProjFS.OnDiskFileState
    End Class


    Public Class Callback
        Implements Microsoft.Windows.ProjFS.IRequiredCallbacks

        Private _path As String
        Private VirtualizationInstance As VirtualizationInstance
        Private DirectoryEnumerations As New Concurrent.ConcurrentDictionary(Of Guid, DirectoryEnumerationRequest)

        Public MaintainedFiles As New Concurrent.ConcurrentDictionary(Of String, MaintainedFile)

        Public Class DirectoryEnumerationRequest
            Public ReadOnly Property relativePath As String
            Public ReadOnly Property BasePath As String

            Public Sub New(BasePath As String, relativePath As String)
                Me._relativePath = relativePath
                Me._BasePath = BasePath

                Dim info = New IO.DirectoryInfo(BasePath & "\" & relativePath)

                list = info.EnumerateFileSystemInfos.OrderBy(Of String)(Function(a) a.Name, New ProjFSSorter()).ToList
                Enumerator = list.GetEnumerator
            End Sub

            Private list As List(Of IO.FileSystemInfo)

            Public Property Enumerator As IEnumerator(Of IO.FileSystemInfo)
        End Class

        Public Sub New(Path As String, instance As VirtualizationInstance)
            Me._path = Path
            Me.VirtualizationInstance = instance

            VirtualizationInstance.OnNotifyFileHandleClosedFileModifiedOrDeleted = AddressOf NotifyFileHandleClosedFileModifiedOrDeletedCallback
            VirtualizationInstance.OnNotifyFileOverwritten = AddressOf NotifyFileOverwrittenCallback
            VirtualizationInstance.OnNotifyFileRenamed = AddressOf NotifyFileRenamedCallback
            VirtualizationInstance.OnNotifyNewFileCreated = AddressOf NotifyNewFileCreatedCallback
            VirtualizationInstance.OnNotifyPreDelete = AddressOf NotifyPreDeleteCallback
            VirtualizationInstance.OnNotifyPreRename = AddressOf NotifyPreRenameCallback
            VirtualizationInstance.OnQueryFileName = AddressOf QueryFileNameCallback
        End Sub

#Region "Optional Callbacks"
        Private Sub NotifyFileHandleClosedFileModifiedOrDeletedCallback(relativePath As String, isDirectory As Boolean, isFileModified As Boolean, isFileDeleted As Boolean, triggeringProcessId As UInteger, triggeringProcessImageFileName As String)
            Debug.WriteLine("NotifyFileHandleClosedFileModifiedOrDeletedCallback")
            If isFileModified Then
                UpdateMaintainStatus(relativePath, OnDiskFileState.DirtyPlaceholder)
            End If
            If isFileDeleted Then
                UpdateMaintainStatus(relativePath, OnDiskFileState.Tombstone)
            End If
        End Sub
        Private Sub NotifyFileOverwrittenCallback(relativePath As String, isDirectory As Boolean, triggeringProcessId As UInteger, triggeringProcessImageFileName As String, ByRef notificationMask As NotificationType)
            Debug.WriteLine("NotifyFileOverwrittenCallback")
            UpdateMaintainStatus(relativePath, OnDiskFileState.DirtyPlaceholder)
        End Sub
        Private Sub NotifyFileRenamedCallback(relativePath As String, destinationPath As String, isDirectory As Boolean, triggeringProcessId As UInteger, triggeringProcessImageFileName As String, ByRef notificationMask As NotificationType)
            Debug.WriteLine("NotifyFileRenamedCallback")
        End Sub
        Private Sub NotifyNewFileCreatedCallback(relativePath As String, isDirectory As Boolean, triggeringProcessId As UInteger, triggeringProcessImageFileName As String, ByRef notificationMask As NotificationType)
            Debug.WriteLine("NotifyNewFileCreatedCallback")
            UpdateMaintainStatus(relativePath, OnDiskFileState.Full)
        End Sub
        Private Function NotifyPreDeleteCallback(relativePath As String, isDirectory As Boolean, triggeringProcessId As UInteger, triggeringProcessImageFileName As String) As Boolean
            Debug.WriteLine("NotifyPreDeleteCallback")
            Dim f As MaintainedFile = Nothing
            MaintainedFiles.TryGetValue(relativePath, f)

            If f Is Nothing OrElse f.OnDiskFileState.HasFlag(OnDiskFileState.Full) Then
                Return True
            End If

            Return False
        End Function
        Private Function NotifyPreRenameCallback(relativePath As String, destinationPath As String, triggeringProcessId As UInteger, triggeringProcessImageFileName As String) As Boolean
            Debug.WriteLine("NotifyPreRenameCallback")
            Dim f As MaintainedFile = Nothing
            MaintainedFiles.TryGetValue(relativePath, f)

            If f Is Nothing OrElse f.OnDiskFileState = OnDiskFileState.Full Then
                Return True
            End If

            Return False
        End Function
        Private Function QueryFileNameCallback(relativePath As String) As HResult
            Debug.WriteLine("QueryFileNameCallback")
            If IO.File.Exists(Me._path & "\" & relativePath) Then
                Return HResult.Ok
            End If
            If IO.Directory.Exists(Me._path & "\" & relativePath) Then
                Return HResult.Ok
            End If

            Return HResult.FileNotFound
        End Function
#End Region

#Region "Required Callbacks"
        Public Function StartDirectoryEnumerationCallback(commandId As Integer, enumerationId As Guid, relativePath As String, triggeringProcessId As UInteger, triggeringProcessImageFileName As String) As HResult Implements IRequiredCallbacks.StartDirectoryEnumerationCallback
            Debug.WriteLine(String.Format("StartDirectoryEnumerationCallback - {0}", enumerationId))

            Dim info = New IO.DirectoryInfo(Me._path & "\" & relativePath)
            If Not info.Exists Then
                Return HResult.FileNotFound
            End If

            DirectoryEnumerations.TryAdd(enumerationId, New DirectoryEnumerationRequest(Me._path, relativePath))

            Return HResult.Ok
        End Function

        Public Function GetDirectoryEnumerationCallback(commandId As Integer, enumerationId As Guid, filterFileName As String, restartScan As Boolean, result As IDirectoryEnumerationResults) As HResult Implements IRequiredCallbacks.GetDirectoryEnumerationCallback
            Debug.WriteLine(String.Format("GetDirectoryEnumerationCallback - {0}- restart {1}", enumerationId.ToString, restartScan.ToString))

            Dim dir As DirectoryEnumerationRequest = Nothing
            Dim entryAdded = False

            If Not DirectoryEnumerations.TryGetValue(enumerationId, dir) Then
                Return HResult.InternalError
            End If

            If restartScan Then
                dir.Enumerator.Reset()
            End If


            Dim isCurrentValid = dir.Enumerator.MoveNext

            While isCurrentValid
                Dim fi = dir.Enumerator.Current
                Dim res As Boolean

                If Utils.IsFileNameMatch(fi.Name, filterFileName) Then
                    If fi.Attributes.HasFlag(IO.FileAttributes.Directory) Then
                        res = result.Add(fi.Name, 0, True, fi.Attributes, fi.CreationTime, fi.LastAccessTime, fi.LastWriteTime, fi.LastWriteTime)
                    Else
                        Dim fil As IO.FileInfo = fi
                        res = result.Add(fil.Name, fil.Length, False, fil.Attributes, fil.CreationTime, fil.LastAccessTime, fil.LastWriteTime, fil.LastWriteTime)
                    End If

                    If res Then
                        entryAdded = True
                        Dim relativePath As String = String.Empty
                        If Not String.IsNullOrWhiteSpace(dir.relativePath) Then
                            relativePath &= dir.relativePath & "\"
                        End If
                        relativePath &= fi.Name
                        UpdateMaintainStatus(relativePath)

                    Else
                            If entryAdded Then
                            Return HResult.Ok
                        Else
                            Return HResult.InsufficientBuffer
                        End If
                    End If
                End If

                isCurrentValid = dir.Enumerator.MoveNext
            End While

            Return HResult.Ok
        End Function

        Public Function EndDirectoryEnumerationCallback(enumerationId As Guid) As HResult Implements IRequiredCallbacks.EndDirectoryEnumerationCallback
            Debug.WriteLine(String.Format("EndDirectoryEnumerationCallback - {0}", enumerationId.ToString))

            Dim dir As DirectoryEnumerationRequest = Nothing
            DirectoryEnumerations.TryRemove(enumerationId, dir)

            Return HResult.Ok
        End Function

        Public Function GetPlaceholderInfoCallback(commandId As Integer, relativePath As String, triggeringProcessId As UInteger, triggeringProcessImageFileName As String) As HResult Implements IRequiredCallbacks.GetPlaceholderInfoCallback
            Debug.WriteLine(String.Format("GetPlaceholderInfoCallback - {0}", relativePath))

            Dim path = Me._path & "\" & relativePath
            If IO.File.Exists(path) Then
                Dim data = New IO.FileInfo(path)

                Me.VirtualizationInstance.WritePlaceholderInfo(relativePath, data.CreationTime, data.LastAccessTime, data.LastWriteTime, data.LastWriteTime, data.Attributes, data.Length, data.Attributes.HasFlag(IO.FileAttributes.Directory), Nothing, Nothing)
                UpdateMaintainStatus(relativePath)
                Return HResult.Ok

            ElseIf IO.Directory.Exists(path) Then
                Dim data = New IO.DirectoryInfo(path)
                Me.VirtualizationInstance.WritePlaceholderInfo(relativePath, data.CreationTime, data.LastAccessTime, data.LastWriteTime, data.LastWriteTime, data.Attributes, 0, data.Attributes.HasFlag(IO.FileAttributes.Directory), Nothing, Nothing)
                UpdateMaintainStatus(relativePath)

                Return HResult.Ok

            Else
                Return HResult.FileNotFound
            End If
        End Function

        Public Function GetFileDataCallback(commandId As Integer, relativePath As String, byteOffset As ULong, length As UInteger, dataStreamId As Guid, contentId() As Byte, providerId() As Byte, triggeringProcessId As UInteger, triggeringProcessImageFileName As String) As HResult Implements IRequiredCallbacks.GetFileDataCallback
            Debug.WriteLine(String.Format("GetFileDataCallback - {0}", relativePath))

            Dim path = Me._path & "\" & relativePath
            Dim offset = 0
            Dim lengthRead = 0
            Dim bufferSize = 4096
            Dim totalread = 0
            Dim byteBuffer(bufferSize - 1) As Byte
            Dim currentOffset = byteOffset

            Using buffer = Me.VirtualizationInstance.CreateWriteBuffer(bufferSize)
                Using str As New IO.FileStream(path, IO.FileMode.Open, IO.FileAccess.Read)
                    Dim l = str.Length
                    Do
                        str.Position = currentOffset
                        lengthRead = str.Read(byteBuffer, 0, IIf(bufferSize < l - currentOffset, bufferSize, l - currentOffset))
                        totalread += lengthRead

                        buffer.Stream.Position = 0
                        buffer.Stream.Write(byteBuffer, 0, lengthRead)
                        Me.VirtualizationInstance.WriteFileData(dataStreamId, buffer, currentOffset, lengthRead)
                        currentOffset += lengthRead
                    Loop Until totalread >= length Or lengthRead = 0
                End Using
            End Using

            UpdateMaintainStatus(relativePath, OnDiskFileState.HydratedPlaceholder)

            Return HResult.Ok
        End Function

#End Region

#Region "Utils"
        'Private Sub UpdateMaintainStatus(FullName As String, relativePath As String)
        '    Dim stat As OnDiskFileState

        '    If Not Microsoft.Windows.ProjFS.Utils.TryGetOnDiskFileState(FullName, stat) Then
        '        Debug.WriteLine(String.Format("Update Status Failed for - {0}", IO.Path.GetFileName(FullName)))
        '    End If

        '    UpdateMaintainStatus(FullName, stat, relativePath)
        'End Sub

        Private Sub UpdateMaintainStatus(relativePath As String, OnDiskFileState As OnDiskFileState)
            Debug.WriteLine("MaintainStatus {0} - {1}", relativePath, OnDiskFileState.ToString)
            MaintainedFiles.AddOrUpdate(relativePath,
                                        Function(key)
                                            Dim mf = New MaintainedFile(relativePath)
                                            mf.OnDiskFileState = OnDiskFileState
                                            Return mf
                                        End Function,
                                        Function(key, oldValue)
                                            oldValue.OnDiskFileState = OnDiskFileState
                                            Return oldValue
                                        End Function)
        End Sub
        Private Sub UpdateMaintainStatus(relativePath As String)
            Dim mf = New MaintainedFile(relativePath)
            mf.OnDiskFileState = OnDiskFileState.Placeholder

            If MaintainedFiles.TryAdd(relativePath, mf) Then
                Debug.WriteLine("MaintainStatus {0} - {1}", relativePath, OnDiskFileState.Placeholder)
            End If
        End Sub

#End Region
    End Class

    Public Class ProjFSSorter
        Implements IComparer(Of String)

        Public Function Compare(x As String, y As String) As Integer Implements IComparer(Of String).Compare
            Return Utils.FileNameCompare(x, y)
        End Function
    End Class

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then

                _instance.StopVirtualizing()

                For Each item In New IO.DirectoryInfo(Me.VirtualizationRootPath).EnumerateFileSystemInfos
                    If item.Attributes.HasFlag(IO.FileAttributes.Directory) Then
                        IO.Directory.Delete(item.FullName, True)
                    Else
                        IO.File.Delete(item.FullName)
                    End If
                Next

                IO.Directory.Delete(Me.VirtualizationRootPath, True)
            End If

            ' TODO: Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer überschreiben
            ' TODO: Große Felder auf NULL setzen
            disposedValue = True
        End If
    End Sub

    ' ' TODO: Finalizer nur überschreiben, wenn "Dispose(disposing As Boolean)" Code für die Freigabe nicht verwalteter Ressourcen enthält
    ' Protected Overrides Sub Finalize()
    '     ' Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(disposing As Boolean)" ein.
    '     Dispose(disposing:=False)
    '     MyBase.Finalize()
    ' End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ' Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(disposing As Boolean)" ein.
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub
End Class


