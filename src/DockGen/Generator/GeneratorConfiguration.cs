namespace DockGen.Generator;

public sealed class GeneratorConfiguration
{
    public required string DockerfileContextDirectory { get; init; }
    public bool MultiArch { get; set; } = true;
}