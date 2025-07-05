using System.Collections.Concurrent;
using System.Diagnostics;
using DockGen.Generator.Evaluators;
using DockGen.Generator.Locators;
using Microsoft.Extensions.Logging;

namespace DockGen.Generator;

public sealed class Analyser : IAnalyser
{
    private readonly ILogger<Analyser> _logger;
    private readonly IProjectFileLocator _projectLocator;
    private readonly IProjectEvaluator _projectEvaluator;

    public Analyser(
        ILogger<Analyser> logger,
        IProjectFileLocator projectLocator,
        IProjectEvaluator projectEvaluator)
    {
        _logger = logger;
        _projectLocator = projectLocator;
        _projectEvaluator = projectEvaluator;
    }

    public async ValueTask<List<Project>> AnalyseAsync(AnalyserRequest request, CancellationToken cancellationToken)
    {
        var projectFiles = await _projectLocator.LocateProjectFilesAsync(request, cancellationToken);

        var dependencyTree = await BuildDependencyTreeAsync(request.WorkingDirectory, projectFiles, cancellationToken);

        var result = dependencyTree.DependencyTree
            .Where(x => projectFiles.Contains(x.Key))
            .Select(x => x.Value)
            .ToList();

        return result;
    }

    private sealed record DependencyTreeResult(ConcurrentDictionary<string, Project> DependencyTree);

    private async Task<DependencyTreeResult> BuildDependencyTreeAsync(string workingDirectory, List<string> projects, CancellationToken ct = default)
    {
        _logger.LogInformation("Building dependency tree for {ProjectCount} project(s)", projects.Count);

        ConcurrentDictionary<string, Project> dependencyTree = new();

        ct.ThrowIfCancellationRequested();

        var degreeOfParallelism = Environment.ProcessorCount;

        await Parallel.ForEachAsync(projects, new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism },
            async (currentProjectPath, cancellationToken) =>
            {
                if (string.IsNullOrEmpty(currentProjectPath))
                {
                    return;
                }

                if (dependencyTree.ContainsKey(currentProjectPath))
                {
                    return;
                }

                var relativeProjectPath = Path.GetRelativePath(workingDirectory, currentProjectPath);
                await ProcessProjectAsync(workingDirectory, relativeProjectPath, dependencyTree, cancellationToken);
            });

        var result = new DependencyTreeResult(dependencyTree);

        _logger.LogInformation("Built dependency tree with {ProjectCount} projects", result.DependencyTree.Count);

        return result;
    }

    private async Task<Project> ProcessProjectAsync(
        string workingDirectory,
        string relativeProjectPath,
        ConcurrentDictionary<string, Project> dependencyTree,
        CancellationToken cancellationToken = default)
    {
        var absoluteProjectPath = Path.GetFullPath(relativeProjectPath, workingDirectory);
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

        var evaluatedProject = await _projectEvaluator.EvaluateAsync(workingDirectory, relativeProjectPath, cancellationToken);
        var projectProperties = evaluatedProject.Properties;
        var projectReferences = evaluatedProject.References;

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
                var normalizedPath = Path.GetFullPath(combinedPath, workingDirectory);

                absoluteReferencePath = normalizedPath;
            }

            var relativeReferencePath = Path.GetRelativePath(workingDirectory, absoluteReferencePath);

            var dependency = await ProcessProjectAsync(workingDirectory, relativeReferencePath, dependencyTree, cancellationToken);

            shallowReferences.Add(dependency);
        }

        var deepReferences = ExpandReferences(shallowReferences);

        project = new Project
        {
            ProjectName = Path.GetFileName(relativeProjectPath),
            ProjectDirectory = Path.GetFullPath(Path.GetDirectoryName(relativeProjectPath)!, workingDirectory),
            Properties = projectProperties,
            Items = new Dictionary<string, List<ProjectItem>>(),
            Dependencies = deepReferences
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

        var expandedReferences = new List<Project>();
        while (stack.Count > 0)
        {
            var currentProject = stack.Pop();
            if (expandedReferences.Contains(currentProject))
            {
                continue;
            }

            expandedReferences.Add(currentProject);

            foreach (var dependency in currentProject.Dependencies)
            {
                if (!expandedReferences.Contains(dependency))
                {
                    stack.Push(dependency);
                }
            }
        }

        return expandedReferences;
    }
}
