using Buildalyzer;
using Microsoft.Extensions.FileProviders;

namespace DockGen.Generator.ProjectEvaluators;

public sealed class BuildalyzerProjectEvaluator : IProjectEvaluator
{
    private readonly IFileProvider _fileProvider;

    public BuildalyzerProjectEvaluator(IFileProvider fileProvider)
    {
        _fileProvider = fileProvider;
    }

    public async Task<EvaluatedProject> EvaluateAsync(
        string workingDirectory,
        string relativeProjectPath, CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        var fileInfo = _fileProvider.GetFileInfo(relativeProjectPath);

        AnalyzerManager manager = new AnalyzerManager();

        var analyzer = manager.GetProject(fileInfo.PhysicalPath!);
        var analyzerResult = analyzer.Build();

        var project = analyzerResult.Results.First();
        var properties = project.Properties
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value);
        var result = new EvaluatedProject(
            Properties: properties,
            References: project.ProjectReferences.ToList());

        return result;
    }
}
