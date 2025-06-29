using System.Collections.Concurrent;
using System.Diagnostics;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace DockGen.Generator;

public sealed class PlainAnalyser : IDockGenAnalyser
{
    private readonly ILogger<PlainAnalyser> _logger;
    private readonly IFileProvider _fileProvider;
    private readonly IProjectFileLocator _fileLocator;

    public PlainAnalyser(ILogger<PlainAnalyser> logger, IFileProvider fileProvider, IProjectFileLocator fileLocator)
    {
        _logger = logger;
        _fileProvider = fileProvider;
        _fileLocator = fileLocator;
    }

    public async ValueTask<List<Project>> AnalyseAsync(AnalyserRequest request, CancellationToken cancellationToken)
    {
        var projectFiles = await _fileLocator.LocateProjectFilesAsync(request, cancellationToken);

        var dependencyTree = await BuildDependencyTreeAsync(projectFiles, cancellationToken);

        var result = dependencyTree.DependencyTree
            .Where(x => projectFiles.Contains(x.Key))
            .Select(x => x.Value)
            .ToList();

        return result;
    }

    private sealed record DependencyTreeResult(ConcurrentDictionary<string, Project> DependencyTree);

    private async Task<DependencyTreeResult> BuildDependencyTreeAsync(List<string> projects, CancellationToken ct = default)
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

            await ProcessProjectAsync(currentProjectPath, dependencyTree, cancellationToken);
        });

        var result = new DependencyTreeResult(dependencyTree);

        _logger.LogInformation("Built dependency tree with {ProjectCount} projects", result.DependencyTree.Count);

        return result;
    }

    private async Task<Project> ProcessProjectAsync(
        string currentProjectPath,
        ConcurrentDictionary<string, Project> dependencyTree,
        CancellationToken cancellationToken = default)
    {
        if (dependencyTree.TryGetValue(currentProjectPath, out var project))
        {
            return project;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Operation cancelled while processing project {ProjectPath}", currentProjectPath);
            cancellationToken.ThrowIfCancellationRequested();
        }

        _logger.LogInformation("Analyzing project {ProjectPath}...", currentProjectPath);

        var stopWatch = Stopwatch.StartNew();

        var fileInfo = _fileProvider.GetFileInfo(currentProjectPath);

        await using var stream = fileInfo.CreateReadStream();
        using var xmlReader = XmlReader.Create(stream);
        var projectRootElement = ProjectRootElement.Create(xmlReader);

        var projectReferences = projectRootElement.Items
            .Where(x => x.ItemType.Equals("ProjectReference", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Include)
            .ToList();

        var dependencies = new List<Project>();
        foreach (var projectReference in projectReferences)
        {
            string absoluteReferencePath;
            if (Path.IsPathRooted(projectReference))
            {
                absoluteReferencePath = projectReference;
            }
            else
            {
                var currentProjectDirectory = Path.GetDirectoryName(currentProjectPath);
                var combinedPath = Path.Combine(currentProjectDirectory ?? string.Empty, projectReference);
                var normalizedPath = Path.GetFullPath(combinedPath);

                absoluteReferencePath = normalizedPath;
            }

            var dependency = await ProcessProjectAsync(absoluteReferencePath, dependencyTree, cancellationToken);

            dependencies.Add(dependency);
        }

        project = new Project
        {
            ProjectName = Path.GetFileName(currentProjectPath),
            ProjectDirectory = Path.GetDirectoryName(currentProjectPath)!,
            Properties = new Dictionary<string, string>(),
            Items = new Dictionary<string, List<ProjectItem>>(),
            Dependencies = dependencies
        };

        dependencyTree.TryAdd(currentProjectPath, project);

        stopWatch.Stop();
        _logger.LogInformation("Analyzed project {ProjectPath} in {ElapsedMilliseconds}ms", currentProjectPath, stopWatch.ElapsedMilliseconds);

        return project;
    }
}
