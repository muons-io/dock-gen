using Buildalyzer;
using DockGen.Generator.Locators;
using Microsoft.Extensions.FileProviders;

namespace DockGen.Generator.Evaluators;

public sealed class BuildalyzerProjectEvaluator : IProjectEvaluator
{
    private readonly IFileProvider _fileProvider;
    private readonly IRelevantFileLocator _relevantFileLocator;

    public BuildalyzerProjectEvaluator(IFileProvider fileProvider, IRelevantFileLocator relevantFileLocator)
    {
        _fileProvider = fileProvider;
        _relevantFileLocator = relevantFileLocator;
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
        var relevantFiles = await _relevantFileLocator.GetRelevantFilesAsync(project.ProjectFilePath, properties, cancellationToken);

        var result = new EvaluatedProject(
            Properties: properties,
            References: project.ProjectReferences.ToList(),
            RelevantFiles: relevantFiles);

        return result;
    }
}
