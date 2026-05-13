using GHost.Core.Logging;
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
            .AddModule<PythonAnalyses.LinearRegression>(moduleBuilder => moduleBuilder
                .ConfigurePoint(nameof(PythonAnalyses.LinearRegression.Well_Level), ".WellLevel")
                .ConfigurePoint(nameof(PythonAnalyses.LinearRegression.Intercept), ".Intercept")
                .ConfigurePoint(nameof(PythonAnalyses.LinearRegression.Slope), ".Slope")
                .AddContext($"{nameof(PythonAnalyses.LinearRegression)} Test Context", "WasteWater.SPS1")
            )
            .RunAsAt("*")
            .WithMinimumAssemblyLogLevel(LogLevel.Info)
            ;
    }
}
