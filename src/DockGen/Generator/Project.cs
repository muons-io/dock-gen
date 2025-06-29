namespace DockGen.Generator;

public sealed class Project
{
    public required string ProjectName { get; init; }
    public required string ProjectDirectory { get; init; }
    public required Dictionary<string, string> Properties { get; init; }
    public required Dictionary<string, List<ProjectItem>> Items { get; init; }

    public required List<Project> Dependencies { get; init; }

    public string FullPath => Path.Combine(ProjectDirectory, ProjectName);
}