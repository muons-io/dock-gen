namespace DockGen.Generator.Constants;

public static class DockGenConstants
{
    public const string DefaultBuildRegistry = "mcr.microsoft.com";
    public const string DefaultBuildPort = "443";
    public const string DefaultBuildRepository = "dotnet/sdk";
    public const string DefaultBuildFamily = "";

    public const string DefaultBaseRegistry = "mcr.microsoft.com";
    public const string DefaultBasePort = "443";
    public const string DefaultBaseRepository = "dotnet/aspnet";
    public const string DefaultBaseFamily = "";

    public const string DefaultContainerPortType = "tcp";

    public const string DirectoryBuildPropsFileName = "Directory.Build.props";
    public const string DirectoryBuildTargetsFileName = "Directory.Build.targets";
    public const string DirectoryPackagesPropsFileName = "Directory.Packages.props";
    public const string NugetConfigFileName = "NuGet.Config";

    public const string SimpleAnalyzerName = "SimpleAnalyzer";
    public const string DesignBuildTimeAnalyzerName = "DesignBuildTimeAnalyzer";
    public const string FastAnalyzerName = "FastAnalyzer";
}
