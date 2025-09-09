Imports GHost.GSharp.Core

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

    Public Overrides ReadOnly Property CompanyDataFolder As String
        Get
            Return SolutionConstants.CompanyDataFolder
        End Get
    End Property
End Class
