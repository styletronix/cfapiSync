Public Class Form1
    Private fs As New FS

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles Me.Load


    End Sub

    Private Sub Form1_Disposed(sender As Object, e As EventArgs) Handles Me.Disposed
        fs.Dispose()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        fs.removeLocalCache()
    End Sub
End Class
