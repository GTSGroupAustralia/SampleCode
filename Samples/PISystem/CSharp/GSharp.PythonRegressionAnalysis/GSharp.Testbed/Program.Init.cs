using GHost.Core.Logging;
using GHost.GSharp.Developer;
using System;
using System.Diagnostics;

namespace GSharp.Testbed
{
    internal partial class Program
    {
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
    }
}
