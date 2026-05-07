Imports GHost.Core.Logging
Imports GHost.GSharp.Core.Configuration.Logging
Imports GHost.GSharp.Developer
Imports GHost.GSharp.Developer.Builder

Partial Friend Class Program

    Public Shared Sub Main()
        RunTestbed()
    End Sub

    Private Sub ConfigureAssembly(builder As AssemblyBuilder)

        builder _
            .AddModule(Of GSharp.PythonRegressionAnalysis.LinearRegression)(Sub(moduleBuilder) moduleBuilder _
                .ConfigurePoint(NameOf(GSharp.PythonRegressionAnalysis.LinearRegression.ProcessFeedrate), "Process Feedrate") _
                .ConfigurePoint(NameOf(GSharp.PythonRegressionAnalysis.LinearRegression.Intercept), "Linear Regression Analysis|Intercept") _
                .ConfigurePoint(NameOf(GSharp.PythonRegressionAnalysis.LinearRegression.Slope), "Linear Regression Analysis|Slope") _
                .AddContext($"{NameOf(GSharp.PythonRegressionAnalysis.LinearRegression)} Test Context", "\\SALES-AF1\NuGreen\NuGreen\Tucson\Distilling Process\Equipment\P-871")
            ) _
            .WithMinimumAssemblyLogLevel(LogLevel.Info) _
            .RunAsAt("*")

    End Sub

End Class
