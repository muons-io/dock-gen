namespace DockGen.Generator;

public class ProjectItem
{
    public required string ItemSpec { get; init; }
    public required IReadOnlyDictionary<string, string> Metadata { get; init; }
}