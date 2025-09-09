Imports GHost.Core.Logging
Imports GHost.GSharp.Core.Configuration.Logging
Imports GHost.GSharp.Developer
Imports GHost.GSharp.Developer.Builder

Partial Friend Class Program
    Public Shared Sub Main()
        Dim prog As New Program()
        prog.RunDebuggingHost()
    End Sub


    Private Sub ConfigureAssembly(builder As AssemblyBuilder)
        builder.AddModule(Of PythonRegressionAnalysis.LinearRegression)(Sub(moduleBuilder) moduleBuilder.
            ConfigurePoint(NameOf(PythonRegressionAnalysis.LinearRegression.ProcessFeedrate), "|Process Feedrate").
            ConfigurePoint(NameOf(PythonRegressionAnalysis.LinearRegression.Intercept), "|Linear Regression Analysis|Intercept").
            ConfigurePoint(NameOf(PythonRegressionAnalysis.LinearRegression.Slope), "|Linear Regression Analysis|Slope").
            AddContext("ContextName1", "\\SALES-AF1\NuGreen\NuGreen\Tucson\Distilling Process\Equipment\P-871")
            )
    End Sub

    Private Sub ConfigureSettings(settings As AssemblyTestHost.TestHostSettings)
        Dim logLevelSettings As LogLevelsSettings = settings.Logging.Levels
        logLevelSettings.ApplyToAll(LogLevel.Info)
    End Sub
End Class
