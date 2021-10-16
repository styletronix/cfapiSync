Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports Vanara.PInvoke
Imports Vanara.PInvoke.CldApi

Public Class Form1
    Private VirtualizationRootPath As String
    Private _SyncProvider As SyncProvider


    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim source As String = "D:\TEMP"
        Me.VirtualizationRootPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\VirtualTest3"

        If Not IO.Directory.Exists(source) Then IO.Directory.CreateDirectory(source)

        Me._SyncProvider = New SyncProvider(Me.VirtualizationRootPath, New FolderProvider(source))
        Me._SyncProvider.Start()

        Me.Button1.Enabled = False
        Me.Button2.Enabled = True
        Me.Button3.Enabled = True
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Me._SyncProvider.Stop()

        Me.Button1.Enabled = True
        Me.Button2.Enabled = False
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Me._SyncProvider.DeleteLocalData()

        Me.Button1.Enabled = True
        Me.Button2.Enabled = False
        Me.Button3.Enabled = False
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        Me._SyncProvider.Unregister()
    End Sub
End Class

