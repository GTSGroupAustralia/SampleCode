Imports GHost.Core.Logging
Imports GHost.GSharp.Core
Imports GHost.GSharp.Core.Enums
Imports GHost.GSharp.Developer

Partial Friend Class Program

    Public Shared Sub RunTestbed()
        Dim prog As New Program()
        prog.RunDebuggingHost()
    End Sub

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

    Partial Private Sub ConfigureSettings(settings As AssemblyTestHost.TestHostSettings)
    End Sub

End Class

Public Class GlobalDataProvider
    Inherits GlobalDataProviderBase

    Public Shared Instance As New GlobalDataProvider()

    Public Shared Sub Initialize()
        GlobalData.GlobalDataProvider = Instance
    End Sub

    Public Overrides ReadOnly Property ProductTitle As String
        Get
            Return "gSharp Developer Testbed"
        End Get
    End Property

    Public Overrides ReadOnly Property ProductVersionString As String
        Get
            Return Versions.ProductVersion
        End Get
    End Property

    Public Overrides ReadOnly Property LogSource As String
        Get
            Return "$DevTestbed"
        End Get
    End Property

    Public Overrides ReadOnly Property ProductName As String
        Get
            Return "GHost.GSharp.Developer.Testbed"
        End Get
    End Property

    Public Overrides ReadOnly Property ProgramType As ProgramType
        Get
            Return ProgramType.DeveloperTestbed
        End Get
    End Property

    Public Overrides ReadOnly Property CompanyDataFolder As String
        Get
            Return SolutionConstants.CompanyDataFolder
        End Get
    End Property
End Class
