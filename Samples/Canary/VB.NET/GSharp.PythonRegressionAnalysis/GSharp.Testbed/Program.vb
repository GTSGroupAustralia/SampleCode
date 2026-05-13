Imports GHost.Core.Logging
Imports GHost.GSharp.Developer.Builder

Partial Friend Class Program

    Public Shared Sub Main()
        RunTestbed()
    End Sub

    Private Sub ConfigureAssembly(builder As AssemblyBuilder)

        builder _
            .AddModule(Of GSharp.PythonRegressionAnalysis.LinearRegression)(Sub(moduleBuilder) moduleBuilder _
                .ConfigurePoint(NameOf(GSharp.PythonRegressionAnalysis.LinearRegression.Well_Level), ".WellLevel") _
                .ConfigurePoint(NameOf(GSharp.PythonRegressionAnalysis.LinearRegression.Intercept), ".Intercept") _
                .ConfigurePoint(NameOf(GSharp.PythonRegressionAnalysis.LinearRegression.Slope), ".Slope") _
                .AddContext($"{NameOf(GSharp.PythonRegressionAnalysis.LinearRegression)} Test Context", "WasteWater.SPS1")
            ) _
            .WithMinimumAssemblyLogLevel(LogLevel.Info) _
            .RunAsAt("*")

    End Sub

End Class
