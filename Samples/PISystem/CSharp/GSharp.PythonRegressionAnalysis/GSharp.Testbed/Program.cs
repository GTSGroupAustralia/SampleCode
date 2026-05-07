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
                .ConfigurePoint(nameof(GSharp.PythonRegressionAnalysis.LinearRegression.ProcessFeedrate), "Process Feedrate")
                .ConfigurePoint(nameof(GSharp.PythonRegressionAnalysis.LinearRegression.Intercept), "Linear Regression Analysis|Intercept")
                .ConfigurePoint(nameof(GSharp.PythonRegressionAnalysis.LinearRegression.Slope), "Linear Regression Analysis|Slope")
                .AddContext($"{nameof(GSharp.PythonRegressionAnalysis.LinearRegression)} Test Context", @"\\DEV-AF1\NuGreen\NuGreen\Tucson\Distilling Process\Equipment\P-871")
            )
            .RunAsAt("*")
            .WithMinimumAssemblyLogLevel(LogLevel.Info)
            ;
    }
}
