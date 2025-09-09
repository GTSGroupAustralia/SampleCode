using GHost.Core.Logging;
using GHost.GSharp.Core.Configuration.Logging;
using GHost.GSharp.Developer.Builder;
using GSharp.CL.PythonRegression;
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
                .ConfigurePoint(nameof(LinearRegression.Well_Level), ".WellLevel")
                .ConfigurePoint(nameof(LinearRegression.Intercept), ".Intercept")
                .ConfigurePoint(nameof(LinearRegression.Slope), ".Slope")
                .AddContext("ContextName1", "WasteWater.SPS1")
                );
        }

        private void ConfigureSettings(TestHostSettings settings)
        {
            LogLevelsSettings logLevelSettings = settings.Logging.Levels;
            logLevelSettings.ApplyToAll(LogLevel.Info);
        }
    }
}
