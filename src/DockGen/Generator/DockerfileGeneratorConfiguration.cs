namespace DockGen.Generator;

public sealed class DockerfileGeneratorConfiguration
{
    public string? TargetFramework { get; set; }
    public string? SolutionPath { get; set; }
    public string? ProjectPath { get; set; }
    public bool MultiArch { get; set; } = true;

    public bool OnlyUpdateCopyIfExists { get; set; } = true;
}