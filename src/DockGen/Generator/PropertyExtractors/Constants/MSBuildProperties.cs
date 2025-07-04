namespace DockGen.Generator.PropertyExtractors.Constants;

public static class MSBuildProperties
{
    public const string True = "true";
    public const string False = "false";

    public static class GeneralProperties
    {
        public const string TargetFramework = "TargetFramework";
        public const string TargetFileName = "TargetFileName";

        public const string ImportDirectoryBuildProps = "ImportDirectoryBuildProps";
        public const string DirectoryBuildPropsPath = "DirectoryBuildPropsPath";

        public const string ImportDirectoryPackagesProps = "ImportDirectoryPackagesProps";
        public const string DirectoryPackagesPropsPath = "DirectoryPackagesPropsPath";

        public const string OutputType = "OutputType";
    }

    public static class ContainerProperties
    {
        // Container properties based on: https://learn.microsoft.com/en-us/dotnet/core/docker/publish-as-container?pivots=dotnet-8-0

        public const string ContainerBaseImage = "ContainerBaseImage";
        public const string ContainerFamily = "ContainerFamily";
        public const string ContainerRuntimeIdentifier = "ContainerRuntimeIdentifier";
        public const string ContainerRegistry = "ContainerRegistry";
        public const string ContainerRepository = "ContainerRepository";
        public const string ContainerImageTag = "ContainerImageTag";
        public const string ContainerImageTags = "ContainerImageTags";
        public const string ContainerLabel = "ContainerLabel";

        public const string ContainerWorkingDirectory = "ContainerWorkingDirectory";
        public const string ContainerPort = "ContainerPort";

        public const string ContainerEnvironmentVariable = "ContainerEnvironmentVariable";

        public const string ContainerAppCommand = "ContainerAppCommand";
        public const string ContainerAppCommandArgs = "ContainerAppCommandArgs";
        public const string ContainerDefaultArgs = "ContainerDefaultArgs";
        public const string ContainerAppCommandInstruction = "ContainerAppCommandInstruction";
        public const string ContainerUser = "ContainerUser";
    }

    public static class ContainerMetadata
    {
        public const string ContainerPortType = "Type";
    }

    public static class DockerfileProperties
    {
        // Dockerfile Properties https://learn.microsoft.com/en-us/visualstudio/containers/container-msbuild-properties?view=vs-2022

        public const string ContainerDevelopmentMode = "ContainerDevelopmentMode";
        public const string ContainerVsDbgPath = "ContainerVsDbgPath";
        public const string DockerDebuggeeArguments = "DockerDebuggeeArguments";
        public const string DockerDebuggeeProgram = "DockerDebuggeeProgram";
        public const string DockerDebuggeeKillProgram = "DockerDebuggeeKillProgram";
        public const string DockerDebuggeeWorkingDirectory = "DockerDebuggeeWorkingDirectory";
        public const string DockerDefaultTargetOS = "DockerDefaultTargetOS";
        public const string DockerImageLabels = "DockerImageLabels";
        public const string DockerFastModeProjectMountDirectory = "DockerFastModeProjectMountDirectory";
        public const string DockerfileBuildArguments = "DockerfileBuildArguments";
        public const string DockerfileContext = "DockerfileContext";
        public const string DockerfileFastModeStage = "DockerfileFastModeStage";
        public const string DockerfileFile = "DockerfileFile";
        public const string DockerfileRunArguments = "DockerfileRunArguments";
        public const string DockerfileRunEnvironmentFiles = "DockerfileRunEnvironmentFiles";
        public const string DockerfileTag = "DockerfileTag";
    }
}
