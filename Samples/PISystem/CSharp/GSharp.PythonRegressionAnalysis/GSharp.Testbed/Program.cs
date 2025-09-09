using GHost.Core.Logging;
using GHost.GSharp.Core.Configuration.Logging;
using GHost.GSharp.Developer.Builder;
using GSharp.PythonRegressionAnalysis;
using static GHost.GSharp.Developer.AssemblyTestHost;

namespace GSharp.Testbed
{
    internal partial class Program
    {
        private static void Main()
        {
            new Program().RunDebuggingHost();
        }

        private void ConfigureAssembly(AssemblyBuilder builder)
        {
            builder.AddModule<LinearRegression>(moduleBuilder => moduleBuilder
                .ConfigurePoint(nameof(LinearRegression.ProcessFeedrate), "|Process Feedrate")
                .ConfigurePoint(nameof(LinearRegression.Intercept), "|Linear Regression Analysis|Intercept")
                .ConfigurePoint(nameof(LinearRegression.Slope), "|Linear Regression Analysis|Intercept")
                .AddContext("ContextName1", @"\\SALES-AF1\NuGreen\NuGreen\Tucson\Distilling Process\Equipment\P-871")
                );
        }

        private void ConfigureSettings(TestHostSettings settings)
        {
            LogLevelsSettings logLevelSettings = settings.Logging.Levels;
            logLevelSettings.ApplyToAll(LogLevel.Info);
        }
    }
}
