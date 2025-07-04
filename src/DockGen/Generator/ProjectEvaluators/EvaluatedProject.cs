namespace DockGen.Generator.ProjectEvaluators;

public sealed record EvaluatedProject(
    Dictionary<string, string> Properties,
    List<string> References);
