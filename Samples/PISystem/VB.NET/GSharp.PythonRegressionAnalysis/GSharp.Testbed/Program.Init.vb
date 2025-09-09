Imports GHost.Core.Logging
Imports GHost.GSharp.Developer

Friend Class Program
    Private Sub RunDebuggingHost()
        Dim Log As ILogger = Nothing

        Try
            GlobalDataProvider.Initialize()

            Dim host = New AssemblyTestHost()

            ConfigureAssembly(host.GetBuilder())
            ConfigureSettings(host.Settings)

            Log = host.Log

            host.Start()
        Catch ex As Exception
            If Log Is Nothing Then
                Console.WriteLine(ex.ToString())
            Else
                Log.Error(ex)
            End If
        End Try

        If Debugger.IsAttached Then
            Console.Write("Press any key to continue . . . ")
            Console.ReadKey(intercept:=True)
        End If
    End Sub
End Class
