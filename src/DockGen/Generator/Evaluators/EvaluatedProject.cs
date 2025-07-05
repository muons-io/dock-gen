namespace DockGen.Generator.Evaluators;

public sealed record EvaluatedProject(
    Dictionary<string, string> Properties,
    List<string> References,
    List<string> RelevantFiles);
