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

        ConcurrentDictionary<string, Lazy<Task<Project>>> analysisByAbsolutePath = new(StringComparer.OrdinalIgnoreCase);
        ConcurrentDictionary<string, Project> dependencyTree = new(StringComparer.OrdinalIgnoreCase);

        var degreeOfParallelism = Environment.ProcessorCount;
        await Parallel.ForEachAsync(projectFiles, new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism },
            async (currentProjectPath, ct) =>
            {
                if (string.IsNullOrEmpty(currentProjectPath))
                {
                    return;
                }

                var relativeProjectPath = Path.GetRelativePath(request.WorkingDirectory, currentProjectPath);
                await ProcessProjectAsync(request, relativeProjectPath, analysisByAbsolutePath, dependencyTree, ct);
            });

        _logger.LogInformation("Built dependency tree with {ProjectCount} projects in {ElapsedMilliseconds}ms", dependencyTree.Count, sw.ElapsedMilliseconds);

        var result = dependencyTree
            .Where(x => projectFiles.Contains(x.Key, StringComparer.OrdinalIgnoreCase))
            .Select(x => x.Value)
            .ToList();

        return result;
    }

    private Task<Project> ProcessProjectAsync(
        AnalyzerRequest request,
        string relativeProjectPath,
        ConcurrentDictionary<string, Lazy<Task<Project>>> analysisByAbsolutePath,
        ConcurrentDictionary<string, Project> dependencyTree,
        CancellationToken cancellationToken = default)
    {
        var absoluteProjectPath = Path.GetFullPath(relativeProjectPath, request.WorkingDirectory);

        var lazyTask = analysisByAbsolutePath.GetOrAdd(
            absoluteProjectPath,
            _ => new Lazy<Task<Project>>(
                () => AnalyzeProjectAsync(request, relativeProjectPath, absoluteProjectPath, analysisByAbsolutePath, dependencyTree, cancellationToken),
                LazyThreadSafetyMode.ExecutionAndPublication));

        return AwaitAnalysisAsync(absoluteProjectPath, lazyTask, analysisByAbsolutePath);
    }

    private async Task<Project> AwaitAnalysisAsync(
        string absoluteProjectPath,
        Lazy<Task<Project>> lazyTask,
        ConcurrentDictionary<string, Lazy<Task<Project>>> analysisByAbsolutePath)
    {
        try
        {
            return await lazyTask.Value;
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze project {ProjectPath}", absoluteProjectPath);
            analysisByAbsolutePath.TryRemove(absoluteProjectPath, out _);
            throw;
        }
    }

    private async Task<Project> AnalyzeProjectAsync(
        AnalyzerRequest request,
        string relativeProjectPath,
        string absoluteProjectPath,
        ConcurrentDictionary<string, Lazy<Task<Project>>> analysisByAbsolutePath,
        ConcurrentDictionary<string, Project> dependencyTree,
        CancellationToken cancellationToken)
    {
        if (dependencyTree.TryGetValue(absoluteProjectPath, out var existingProject))
        {
            return existingProject;
        }

        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Analyzing project {ProjectPath}...", relativeProjectPath);

        var stopWatch = Stopwatch.StartNew();

        var evaluatedProject = request.Analyzer switch
        {
            DockGenConstants.SimpleAnalyzerName => await _simpleEvaluator.EvaluateAsync(request.WorkingDirectory, relativeProjectPath, cancellationToken),
            DockGenConstants.DesignBuildTimeAnalyzerName => await _designBuildTimeEvaluator.EvaluateAsync(request.WorkingDirectory, relativeProjectPath, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Analyzer), request.Analyzer, "Unknown analyzer type")
        };

        var shallowReferences = new List<Project>();
        foreach (var projectReference in evaluatedProject.References)
        {
            var absoluteReferencePath = Path.IsPathRooted(projectReference)
                ? projectReference
                : Path.GetFullPath(
                    Path.Combine(Path.GetDirectoryName(relativeProjectPath) ?? string.Empty, projectReference),
                    request.WorkingDirectory);

            var relativeReferencePath = Path.GetRelativePath(request.WorkingDirectory, absoluteReferencePath);

            var dependency = await ProcessProjectAsync(request, relativeReferencePath, analysisByAbsolutePath, dependencyTree, cancellationToken);
            shallowReferences.Add(dependency);
        }

        var deepReferences = ExpandReferences(shallowReferences);

        var project = new Project
        {
            ProjectName = Path.GetFileName(relativeProjectPath),
            ProjectDirectory = Path.GetFullPath(Path.GetDirectoryName(relativeProjectPath)!, request.WorkingDirectory),
            Properties = evaluatedProject.Properties,
            Dependencies = deepReferences.DistinctBy(x => x.FullPath).ToList(),
            RelevantFiles = evaluatedProject.RelevantFiles
        };

        dependencyTree[absoluteProjectPath] = project;

        stopWatch.Stop();
        _logger.LogInformation("Analyzed project {ProjectPath} in {ElapsedMilliseconds}ms", relativeProjectPath, stopWatch.ElapsedMilliseconds);

        return project;
    }

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
