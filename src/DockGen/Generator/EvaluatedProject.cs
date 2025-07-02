namespace DockGen.Generator;

public sealed record EvaluatedProject(
    Dictionary<string, string> Properties,
    List<string> References);
