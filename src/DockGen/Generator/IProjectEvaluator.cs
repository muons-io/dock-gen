namespace DockGen.Generator;

public interface IProjectEvaluator
{
    Task<EvaluatedProject> EvaluateAsync(
        string workingDirectory,
        string relativeProjectPath,
        CancellationToken cancellationToken = default);
}