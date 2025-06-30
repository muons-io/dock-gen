using System.Collections.Concurrent;
using System.Diagnostics;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Definition;
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

        var fileInfo = _fileProvider.GetFileInfo(relativeProjectPath);

        await using var stream = fileInfo.CreateReadStream();
        using var xmlReader = XmlReader.Create(stream);

        var globalProperties = FindAllGlobalProperties(workingDirectory, string.Empty, relativeProjectPath);

        var p = Microsoft.Build.Evaluation.Project.FromXmlReader(xmlReader, new ProjectOptions
        {
            GlobalProperties = globalProperties
        });

        var projectReferences = p.Items
            .Where(x => x.ItemType.Equals("ProjectReference", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.EvaluatedInclude)
            .ToList();

        var projectProperties = p.Properties
            .ToDictionary(x => x.Name, x => x.EvaluatedValue, StringComparer.OrdinalIgnoreCase);

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
            ProjectDirectory = Path.GetDirectoryName(relativeProjectPath)!,
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
    /// Finds all global properties for a project based on Directory.Build.props and Directory.Build.targets files.
    /// </summary>
    /// <param name="workingDirectory">The working directory of the project.</param>
    /// <param name="relativeCurrentPath">The current path being analyzed.</param>
    /// <param name="relativeProjectPath">The relative path of the project file.</param>
    /// <returns>A dictionary containing global properties as key-value pairs.</returns>
    private Dictionary<string, string> FindAllGlobalProperties(string workingDirectory, string relativeCurrentPath, string relativeProjectPath)
    {
        string[] readPropertiesFrom =
        [
            "Directory.Build.props",
            "Directory.Build.targets"
        ];

        var globalProperties = new Dictionary<string, string>();

        var items = _fileProvider.GetDirectoryContents(relativeCurrentPath);

        // we should only analyze directories that are parent to the project file
        foreach (var item in items)
        {
            if (!item.Exists)
            {
                continue;
            }

            if (item.IsDirectory)
            {
                var directoryPath = item.PhysicalPath!;

                var buildProperties = FindAllGlobalProperties(workingDirectory, directoryPath, relativeProjectPath);
                foreach (var property in buildProperties)
                {
                    globalProperties[property.Key] = property.Value;
                }
            }

            if (readPropertiesFrom.Contains(item.Name))
            {
                var buildProperties = ReadPropertiesFromFile(item);
                foreach (var property in buildProperties)
                {
                    globalProperties[property.Key] = property.Value;
                }
            }
        }

        Dictionary<string, string> ReadPropertiesFromFile(IFileInfo fileInfo)
        {
            var properties = new Dictionary<string, string>();

            using var stream = fileInfo.CreateReadStream();
            using var xmlReader = XmlReader.Create(stream);
            var projectRootElement = ProjectRootElement.Create(xmlReader);
            foreach (var property in projectRootElement.Properties)
            {
                properties[property.Name] = property.Value;
            }

            return properties;
        }


        return globalProperties;
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
