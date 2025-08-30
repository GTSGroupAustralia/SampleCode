using GHost.GSharp.Core;

namespace GSharp.Testbed
{
    public class GlobalDataProvider : GlobalDataProviderBase
    {
        public static GlobalDataProvider Instance = new GlobalDataProvider();

        public static void Initialize() => GlobalData.GlobalDataProvider = Instance;


        public override string ProductTitle => "gSharp Developer Testbed";

        public override string ProductVersionString => Versions.ProductVersion;

        public override string LogSource => "$DevTestbed";

        public override string ProductName => "GHost.GSharp.Developer.Testbed";

        public override string CompanyDataFolder => SolutionConstants.CompanyDataFolder;
    }
}
