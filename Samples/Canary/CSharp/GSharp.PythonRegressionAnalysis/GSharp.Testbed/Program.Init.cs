using GHost.Core.Logging;
using GHost.GSharp.Core;
using GHost.GSharp.Core.Enums;
using GHost.GSharp.Developer;
using System;
using System.Diagnostics;

namespace GSharp.Testbed;

internal partial class Program
{
    private static void RunTestbed()
    {
        new Program().RunDebuggingHost();
    }

    private void RunDebuggingHost()
    {
        ILogger log = null;

        try
        {
            GlobalDataProvider.Initialize();

            var host = new AssemblyTestHost();

            ConfigureAssembly(host.GetBuilder());
            ConfigureSettings(host.Settings);

            log = host.Log;

            host.Start();
        }
        catch (Exception ex)
        {
            if (log is null)
                Console.WriteLine(ex.ToString());
            else
                log.Error(ex);
        }

        if (Debugger.IsAttached)
        {
            Console.Write("Press any key to continue . . . ");
            _ = Console.ReadKey(intercept: true);
        }
    }

    partial void ConfigureSettings(AssemblyTestHost.TestHostSettings settings);
}

public class GlobalDataProvider : GlobalDataProviderBase
{
    public static GlobalDataProvider Instance = new GlobalDataProvider();

    public static void Initialize() => GlobalData.GlobalDataProvider = Instance;


    public override string ProductTitle => "gSharp Developer Testbed";

    public override string ProductVersionString => Versions.ProductVersion;

    public override string LogSource => "$DevTestbed";

    public override string ProductName => "GHost.GSharp.Developer.Testbed";

    public override ProgramType ProgramType => ProgramType.DeveloperTestbed;

    public override string CompanyDataFolder => SolutionConstants.CompanyDataFolder;
}
