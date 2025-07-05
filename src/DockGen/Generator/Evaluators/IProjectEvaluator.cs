namespace DockGen.Generator.Evaluators;

public interface IProjectEvaluator
{
    Task<EvaluatedProject> EvaluateAsync(
        string workingDirectory,
        string relativeProjectPath,
        CancellationToken cancellationToken = default);
}