using GHost.Core.Logging;
using GHost.GSharp.Core.Configuration.Logging;
using GHost.GSharp.Developer;
using GHost.GSharp.Developer.Builder;

namespace GSharp.Testbed;

internal partial class Program
{
    private static void Main()
    {
        RunTestbed();
    }

    private void ConfigureAssembly(AssemblyBuilder builder)
    {
        builder
            .AddModule<GSharp.PythonRegressionAnalysis.LinearRegression>(moduleBuilder => moduleBuilder
                .ConfigurePoint(nameof(GSharp.PythonRegressionAnalysis.LinearRegression.Well_Level), ".WellLevel")
                .ConfigurePoint(nameof(GSharp.PythonRegressionAnalysis.LinearRegression.Slope), ".Slope")
                .ConfigurePoint(nameof(GSharp.PythonRegressionAnalysis.LinearRegression.Intercept), ".Intercept")
                .AddContext($"{nameof(GSharp.PythonRegressionAnalysis.LinearRegression)} Test Context", "WasteWater.SPS1")
            )
            .RunAsAt("*")
            .WithMinimumAssemblyLogLevel(LogLevel.Info)
            ;
    }
}
