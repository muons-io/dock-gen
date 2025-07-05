using System.Collections.Concurrent;
using System.Diagnostics;
using DockGen.Generator.Constants;
using DockGen.Generator.Evaluators;
using DockGen.Generator.Locators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DockGen.Generator;

public sealed class Analyzer : IAnalyzer
{
    private readonly ILogger<Analyzer> _logger;
    private readonly IProjectFileLocator _projectLocator;
    private readonly IProjectEvaluator _simpleEvaluator;
    private readonly IProjectEvaluator _designBuildTimeEvaluator;

    public Analyzer(
        ILogger<Analyzer> logger,
        IProjectFileLocator projectLocator,
        [FromKeyedServices(DockGenConstants.SimpleAnalyzerName)] IProjectEvaluator simpleEvaluator,
        [FromKeyedServices(DockGenConstants.DesignBuildTimeAnalyzerName)] IProjectEvaluator designBuildTimeEvaluator)
    {
        _logger = logger;
        _projectLocator = projectLocator;
        _simpleEvaluator = simpleEvaluator;
        _designBuildTimeEvaluator = designBuildTimeEvaluator;
    }

    public async ValueTask<List<Project>> AnalyseAsync(AnalyzerRequest request, CancellationToken cancellationToken)
    {
        var projectFiles = await _projectLocator.LocateProjectFilesAsync(request, cancellationToken);

        _logger.LogInformation("Building dependency tree for {ProjectCount} project(s)", projectFiles.Count);

        var sw = Stopwatch.StartNew();

        ConcurrentDictionary<string, Project> dependencyTree = new();

        var degreeOfParallelism = Environment.ProcessorCount;
        await Parallel.ForEachAsync(projectFiles, new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism },
            async (currentProjectPath, ct) =>
            {
                if (string.IsNullOrEmpty(currentProjectPath))
                {
                    return;
                }

                if (dependencyTree.ContainsKey(currentProjectPath))
                {
                    return;
                }

                var relativeProjectPath = Path.GetRelativePath(request.WorkingDirectory, currentProjectPath);
                await ProcessProjectAsync(request, relativeProjectPath, dependencyTree, ct);
            });

        _logger.LogInformation("Built dependency tree with {ProjectCount} projects in {ElapsedMilliseconds}ms", dependencyTree.Count, sw.ElapsedMilliseconds);

        var result = dependencyTree
            .Where(x => projectFiles.Contains(x.Key))
            .Select(x => x.Value)
            .ToList();

        return result;
    }

    private async Task<Project> ProcessProjectAsync(
        AnalyzerRequest request,
        string relativeProjectPath,
        ConcurrentDictionary<string, Project> dependencyTree,
        CancellationToken cancellationToken = default)
    {
        var absoluteProjectPath = Path.GetFullPath(relativeProjectPath, request.WorkingDirectory);
        if (dependencyTree.TryGetValue(absoluteProjectPath, out var project))
        {
            return project;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Operation cancelled while processing project {ProjectPath}", relativeProjectPath);
            cancellationToken.ThrowIfCancellationRequested();
        }

        _logger.LogInformation("Analyzing project {ProjectPath}...", relativeProjectPath);

        var stopWatch = Stopwatch.StartNew();

        var evaluatedProject = request.Analyzer switch
        {
            DockGenConstants.SimpleAnalyzerName => await _simpleEvaluator.EvaluateAsync(request.WorkingDirectory, relativeProjectPath, cancellationToken),
            DockGenConstants.DesignBuildTimeAnalyzerName => await _designBuildTimeEvaluator.EvaluateAsync(request.WorkingDirectory, relativeProjectPath, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Analyzer), request.Analyzer, "Unknown analyzer type")
        };

        var projectProperties = evaluatedProject.Properties;
        var projectReferences = evaluatedProject.References;
        var relevantFiles = evaluatedProject.RelevantFiles;

        var shallowReferences = new List<Project>();
        foreach (var projectReference in projectReferences)
        {
            string absoluteReferencePath;
            if (Path.IsPathRooted(projectReference))
            {
                absoluteReferencePath = projectReference;
            }
            else
            {
                var currentProjectDirectory = Path.GetDirectoryName(relativeProjectPath);
                var combinedPath = Path.Combine(currentProjectDirectory ?? string.Empty, projectReference);
                var normalizedPath = Path.GetFullPath(combinedPath, request.WorkingDirectory);

                absoluteReferencePath = normalizedPath;
            }

            var relativeReferencePath = Path.GetRelativePath(request.WorkingDirectory, absoluteReferencePath);

            var dependency = await ProcessProjectAsync(request, relativeReferencePath, dependencyTree, cancellationToken);

            shallowReferences.Add(dependency);
        }

        var deepReferences = ExpandReferences(shallowReferences);

        project = new Project
        {
            ProjectName = Path.GetFileName(relativeProjectPath),
            ProjectDirectory = Path.GetFullPath(Path.GetDirectoryName(relativeProjectPath)!, request.WorkingDirectory),
            Properties = projectProperties,
            Dependencies = deepReferences.DistinctBy(x => x.FullPath).ToList(),
            RelevantFiles = relevantFiles
        };

        dependencyTree.TryAdd(absoluteProjectPath, project);

        stopWatch.Stop();
        _logger.LogInformation("Analyzed project {ProjectPath} in {ElapsedMilliseconds}ms", relativeProjectPath, stopWatch.ElapsedMilliseconds);

        return project;
    }

    /// <summary>
    /// This method expands the references of a project
    /// </summary>
    private List<Project> ExpandReferences(List<Project> references)
    {
        var stack = new Stack<Project>(references);

        var expandedReferences = new HashSet<Project>();
        while (stack.Count > 0)
        {
            var currentProject = stack.Pop();
            if (!expandedReferences.Add(currentProject))
            {
                continue;
            }

            foreach (var dependency in currentProject.Dependencies)
            {
                if (!expandedReferences.Contains(dependency))
                {
                    stack.Push(dependency);
                }
            }
        }

        return expandedReferences.ToList();
    }
}
