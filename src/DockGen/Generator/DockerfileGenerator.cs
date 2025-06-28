using System.Collections.Concurrent;
using System.Diagnostics;
using Buildalyzer;
using DockGen.Constants;
using DockGen.Generator.Extractors;
using DockGen.Generator.Models;
using Microsoft.Build.Construction;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace DockGen.Generator;

public sealed class Project
{
    public required string ProjectName { get; init; }
    public required string ProjectDirectory { get; init; }
    public required Dictionary<string, string> Properties { get; init; }
    public required Dictionary<string, List<ProjectItem>> Items { get; init; }

    public required List<Project> Dependencies { get; init; }

    public string FullPath => Path.Combine(ProjectDirectory, ProjectName);
}

public class ProjectItem
{
    public required string ItemSpec { get; init; }
    public required IReadOnlyDictionary<string, string> Metadata { get; init; }
}

/// <summary>
/// Represents a request to analyze project or solution files, including information about the working directory,
/// solution path, and project path.
/// </summary>
/// <param name="WorkingDirectory">
/// The working directory where the analysis is initiated. This directory typically contains the solution or project files.
/// </param>
/// <param name="SolutionPath">
/// The optional path to the solution file (.sln)/(.slnx). If provided, it will be used to analyze projects within the solution.
/// </param>
/// <param name="ProjectPath">
/// The optional path to a specific project file (.csproj). If provided, it allows focusing the analysis on a single project.
/// </param>
public sealed record AnalyserRequest(string WorkingDirectory, string? SolutionPath = null, string? ProjectPath = null);

public interface IDockGenAnalyser
{
    ValueTask<List<Project>> AnalyseAsync(AnalyserRequest request, CancellationToken cancellationToken);
}

public sealed class BuildalyzerAnalyzer : IDockGenAnalyser
{
    private readonly ConcurrentDictionary<string, IAnalyzerResult> _analyzerCache = new();
    private readonly ConcurrentDictionary<string, List<string>> _dependencyTree = new();

    private readonly ILogger<DockerfileGenerator> _logger;

    public BuildalyzerAnalyzer(ILogger<DockerfileGenerator> logger)
    {
        _logger = logger;
    }

    public async ValueTask<List<Project>> AnalyseAsync(AnalyserRequest request, CancellationToken cancellationToken)
    {
        await Task.Yield();

        _logger.LogInformation("Solution Path: {SolutionPath}, Project Path {ProjectPath}", request.SolutionPath, request.ProjectPath);

        var manager = new AnalyzerManager(request.SolutionPath!);

        BuildDependencyTree(manager, request.ProjectPath!, string.Empty, cancellationToken);

        var projects = manager.SolutionFile.ProjectsInOrder
            .Where(x => x.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
            .ToList();

        _logger.LogInformation("Found {ProjectCount} project(s) in solution", projects.Count);

        var result = new List<Project>();

        foreach (var project in projects)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_dependencyTree.TryGetValue(project.AbsolutePath, out var dependencies))
            {
                _logger.LogWarning("No dependencies found for project {ProjectPath}", project.AbsolutePath);
                continue;
            }

            if (!_analyzerCache.TryGetValue(project.AbsolutePath, out var analyzerResult))
            {
                _logger.LogWarning("No analyzer result found for project {ProjectPath}", project.AbsolutePath);
                continue;
            }

            var projectItems = new Dictionary<string, List<ProjectItem>>();
            foreach (var item in analyzerResult.Items)
            {
                projectItems[item.Key] = item.Value.Select(x => new ProjectItem
                {
                    ItemSpec = x.ItemSpec,
                    Metadata = x.Metadata
                }).ToList();
            }

            var projectDependencies = dependencies.Select(dep => _analyzerCache[dep]).ToList();

            // result.Add(new Project
            // {
            //     ProjectName = project.ProjectName,
            //     ProjectDirectory = Path.GetDirectoryName(project.AbsolutePath)!,
            //     Properties = analyzerResult.Properties,
            //     Items = projectItems,
            //     Dependencies = projectDependencies.Select(d => new Project
            //     {
            //         ProjectName = d.ProjectFileName,
            //         ProjectDirectory = Path.GetDirectoryName(d.ProjectFilePath)!,
            //         Properties = d.Properties.ToDictionary(p => p.Name, p => p.EvaluatedValue),
            //         Items = d.Items.ToDictionary(i => i.Key, i => i.Value.Select(x => new ProjectItem
            //         {
            //             ItemSpec = x.ItemSpec,
            //             Metadata = x.Metadata.ToDictionary(m => m.Name, m => m.EvaluatedValue)
            //         }).ToList()),
            //         Dependencies = []
            //     }).ToList()
            // });
        }

        _logger.LogInformation("Analyzed {ProjectCount} project(s)", result.Count);

        return result;
    }

    private void BuildDependencyTree(IAnalyzerManager manager, string? projectPath, string? targetFramework, CancellationToken ct = default)
    {
        var projects = manager.SolutionFile.ProjectsInOrder
            .Where(x => x.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
            .ToList();

        _logger.LogInformation("Building dependency tree for {ProjectCount} project(s)", projects.Count);

        ConcurrentStack<string> stack = new();
        var projectsToAnalyze = projects.Where(x => x.AbsolutePath == projectPath || projectPath is null);
        foreach(var project in projectsToAnalyze)
        {
            stack.Push(project.AbsolutePath);
        }

        while (stack.Count != 0)
        {
            ct.ThrowIfCancellationRequested();

            var degreeOfParallelism = Environment.ProcessorCount;

            var projectsToProcess = new string[degreeOfParallelism];
            stack.TryPopRange(projectsToProcess, 0, int.Min(degreeOfParallelism, stack.Count));

            Parallel.ForEach(projectsToProcess, currentProjectPath =>
            {
                if (string.IsNullOrEmpty(currentProjectPath))
                {
                    return;
                }

                if (_dependencyTree.ContainsKey(currentProjectPath))
                {
                    return;
                }

                var projectAnalyzer = manager.GetProject(currentProjectPath);

                ProcessProject(projectAnalyzer, targetFramework, stack);
            });
        }
    }

    private void ProcessProject(IProjectAnalyzer projectAnalyzer, string? targetFramework, ConcurrentStack<string> stack)
    {
        _logger.LogInformation("Analyzing project {ProjectPath}...", projectAnalyzer.ProjectFile.Path);
        var stopWatch = Stopwatch.StartNew();
        var analyzerResults = projectAnalyzer.Build();
        stopWatch.Stop();
        _logger.LogInformation("Analyzed project {ProjectPath} in {ElapsedMilliseconds}ms", projectAnalyzer.ProjectFile.Path, stopWatch.ElapsedMilliseconds);

        if (string.IsNullOrEmpty(targetFramework) && analyzerResults.TargetFrameworks.Any())
        {
            targetFramework = analyzerResults.TargetFrameworks.First();
        }

        if (!analyzerResults.TryGetTargetFramework(targetFramework ?? "", out var analyzerResult))
        {
            _logger.LogWarning("Failed to analyze project for target framework {TargetFramework}", targetFramework);
            return;
        }

        _analyzerCache.TryAdd(analyzerResult.ProjectFilePath, analyzerResult);
        _dependencyTree.TryAdd(analyzerResult.ProjectFilePath, analyzerResult.ProjectReferences.ToList());

        foreach (var dependency in analyzerResult.ProjectReferences)
        {
            if (_analyzerCache.ContainsKey(dependency))
            {
                continue;
            }

            stack.Push(dependency);
        }
    }
}

public sealed class PlainAnalyser : IDockGenAnalyser
{
    private readonly ILogger<PlainAnalyser> _logger;
    private readonly IFileProvider _fileProvider;

    public PlainAnalyser(ILogger<PlainAnalyser> logger, IFileProvider fileProvider)
    {
        _logger = logger;
        _fileProvider = fileProvider;
    }

    public async ValueTask<List<Project>> AnalyseAsync(AnalyserRequest request, CancellationToken cancellationToken)
    {
        var workingDirectory = _fileProvider.GetFileInfo(request.WorkingDirectory);
        if (!workingDirectory.Exists || !workingDirectory.IsDirectory)
        {
            _logger.LogError("Working directory {WorkingDirectory} does not exist or is not a directory", request.WorkingDirectory);
            return [];
        }

        var projectFiles = (request.WorkingDirectory, request.SolutionPath, request.ProjectPath) switch
        {
            (_, _, not null) => GetProjectToAnalyse(request.ProjectPath),
            (_, not null, _) => await GetProjectsToAnalyseAsync(request.SolutionPath, cancellationToken),
            _ => GetProjectFilesToAnalyse(workingDirectory.PhysicalPath!)
        };

        var analysedProjects = new List<Project>();

        foreach(var projectFile in projectFiles)
        {
            if (!_fileProvider.GetFileInfo(projectFile).Exists)
            {
                _logger.LogWarning("Project file {ProjectFile} does not exist", projectFile);
                continue;
            }

            analysedProjects.Add(new Project
            {
                ProjectName = Path.GetFileName(projectFile),
                ProjectDirectory = Path.GetDirectoryName(projectFile)!,
                Properties = new Dictionary<string, string>(),
                Items = new Dictionary<string, List<ProjectItem>>(),
                Dependencies = []
            });
        }
        return analysedProjects;
    }

    private List<string> GetProjectToAnalyse(string projectPath)
    {
        var projectFiles = new List<string>();

        var projectFile = _fileProvider.GetFileInfo(projectPath);
        if (projectFile.Exists && !projectFile.IsDirectory)
        {
            projectFiles.Add(projectFile.PhysicalPath!);
        }

        return projectFiles;
    }

    private async Task<List<string>> GetProjectsToAnalyseAsync(string solutionPath, CancellationToken ct = default)
    {
        var supportedExtensions = new[] { ".sln", ".slnx" };

        var projectFiles = new List<string>();

        var solutionFile = _fileProvider.GetFileInfo(solutionPath);
        if (!solutionFile.Exists || solutionFile.IsDirectory)
        {
            _logger.LogError("Solution file {SolutionPath} does not exist", solutionPath);
            return projectFiles;
        }

        var extension = Path.GetExtension(solutionFile.PhysicalPath!);
        if (!supportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogError("Solution file {SolutionPath} is not a supported solution file type", solutionPath);
            return projectFiles;
        }

        var serializer = SolutionSerializers.GetSerializerByMoniker(extension);
        if (serializer is null)
        {
            _logger.LogError("No serializer found for solution file {SolutionPath}", solutionPath);
            return projectFiles;
        }

        var solution = await serializer.OpenAsync(solutionFile.PhysicalPath!, ct);
        foreach (var project in solution.SolutionProjects)
        {
            projectFiles.Add(project.FilePath);
        }

        return projectFiles;
    }

    private List<string> GetProjectFilesToAnalyse(string workingDirectory)
    {
        var projectFiles = new List<string>();

        var items = _fileProvider.GetDirectoryContents(workingDirectory);
        foreach(var item in items)
        {
            if (item.IsDirectory)
            {
                // Recursively get project files from subdirectories
                var subProjectFiles = GetProjectFilesToAnalyse(item.PhysicalPath!);
                projectFiles.AddRange(subProjectFiles);
                continue;
            }

            var extension = Path.GetExtension(item.PhysicalPath!);
            if (extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                projectFiles.Add(item.PhysicalPath!);
            }
        }

        return projectFiles;
    }
}

public sealed class GeneratorConfiguration
{
    public required string DockerfileContextDirectory { get; init; }
    public bool MultiArch { get; set; } = true;
}

public sealed class DockerfileGenerator
{
    private readonly ILogger<DockerfileGenerator> _logger;
    private readonly IExtractor _extractor;

    public DockerfileGenerator(ILogger<DockerfileGenerator> logger, IExtractor extractor)
    {
        _logger = logger;
        _extractor = extractor;
    }

    public async Task<ExitCodes> GenerateDockerfileAsync(GeneratorConfiguration configuration, Project project, CancellationToken ct = default)
    {
        // var outputTypeResult = await _extractor.ExtractAsync(new OutputTypeExtractRequest(project), ct);
        // if (!outputTypeResult.Extracted || !outputTypeResult.Value.Equals("Exe", StringComparison.OrdinalIgnoreCase))
        // {
        //     _logger.LogError("Skipping library project {ProjectPath}", currentProjectPath);
        //     return ExitCodes.Failure;
        // }

        var buildImageResult = await _extractor.ExtractAsync(new ContainerBuildImageExtractRequest(project), ct);
        if (!buildImageResult.Extracted)
        {
            _logger.LogError("Failed to get build image");
            return ExitCodes.Failure;
        }

        var baseImageResult = await _extractor.ExtractAsync(new ContainerBaseImageExtractRequest(project), ct);
        if (!baseImageResult.Extracted)
        {
            _logger.LogError("Failed to get base image");
            return ExitCodes.Failure;
        }

        var targetFileNameResult = await _extractor.ExtractAsync(new TargetFileNameExtractRequest(project), ct);
        if (!targetFileNameResult.Extracted)
        {
            _logger.LogError("Failed to get target file name");
            return ExitCodes.Failure;
        }

        var dockerfileContextDirectory = configuration.DockerfileContextDirectory;

        var copyFromTo = PrepareCopyDictionary(dockerfileContextDirectory, project);
        var initialCopyFromTo = PrepareInitialCopyDictionary(dockerfileContextDirectory, project);

        var relativeProjectPath = Path.GetRelativePath(dockerfileContextDirectory, project.FullPath).Replace("..\\","");

        var containerPorts = await _extractor.ExtractAsync(new ContainerPortExtractRequest(project), ct);

        var builder = new DockerfileBuilder
        {
            BaseImage = baseImageResult.Value,
            BuildImage = buildImageResult.Value,
            ProjectDirectory = relativeProjectPath,
            ProjectFile = project.ProjectName,
            WorkDir = "/app",
            Copy = copyFromTo,
            AdditionalCopy = initialCopyFromTo,
            TargetFileName = targetFileNameResult.Value,
            Expose = containerPorts.Extracted ? containerPorts.Value : new List<ContainerPort>(),
            MultiArch = configuration.MultiArch
        };

        var dockerfile = builder.Build();

        var dockerfileName = "Dockerfile";
        var destinationDirectory = project.ProjectDirectory;

        try
        {
            await SaveDockerfileAsync(dockerfile, dockerfileName, destinationDirectory, ct);
            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Dockerfile");
            return ExitCodes.Failure;
        }
    }

    private static Dictionary<string, string> PrepareInitialCopyDictionary(string dockerfileContext, Project analyzerResult)
    {
        // get Directory.Build.props, Directory.Build.targets, and NuGet.Config, Directory.Packages.props for project
        // based on project properties
        // and copy it to the root of the project
        var copyFromTo = new Dictionary<string, string>();

        if (analyzerResult.Properties.TryGetValue(MSBuildProperties.GeneralProperties.ImportDirectoryBuildProps, out var importDirectoryBuildProps)
            && importDirectoryBuildProps == MSBuildProperties.True && analyzerResult.Properties.TryGetValue(MSBuildProperties.GeneralProperties.DirectoryBuildPropsPath, out var directoryBuildPropsPath))
        {
            var relativeBuildPropsPath = Path.GetRelativePath(dockerfileContext, directoryBuildPropsPath).Replace("..\\","");
            var relativeBuildPropsDirectory = Path.GetDirectoryName(relativeBuildPropsPath)?.Replace("..\\","");
            if (string.IsNullOrEmpty(relativeBuildPropsDirectory))
            {
                relativeBuildPropsDirectory = ".";
            }

            copyFromTo.Add(relativeBuildPropsPath, relativeBuildPropsDirectory);
        }

        if (analyzerResult.Properties.TryGetValue(MSBuildProperties.GeneralProperties.ImportDirectoryPackagesProps, out var importDirectoryPackagesProps)
            && importDirectoryPackagesProps == MSBuildProperties.True && analyzerResult.Properties.TryGetValue(MSBuildProperties.GeneralProperties.DirectoryPackagesPropsPath, out var directoryPackagesPropsPath))
        {
            var relativePackagesPropsPath = Path.GetRelativePath(dockerfileContext, directoryPackagesPropsPath).Replace("..\\","");
            var relativePackagesPropsDirectory = Path.GetDirectoryName(relativePackagesPropsPath)?.Replace("..\\","");
            if (string.IsNullOrEmpty(relativePackagesPropsDirectory))
            {
                relativePackagesPropsDirectory = ".";
            }

            copyFromTo.Add(relativePackagesPropsPath, relativePackagesPropsDirectory);
        }

        // check if context directory contains nuget.config and if so add it to copyFromTo;
        var dockerfileContextDirectory = Path.GetDirectoryName(dockerfileContext);
        if (!string.IsNullOrEmpty(dockerfileContextDirectory))
        {
            // we need to also make sure we use the correct casing, as in linux the file system is case sensitive
            var nugetConfigPath = Directory.GetFiles(dockerfileContextDirectory, "nuget.config", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (!string.IsNullOrEmpty(nugetConfigPath))
            {
                var relativeNugetConfigPath = Path.GetRelativePath(dockerfileContext, nugetConfigPath).Replace("..\\","");
                copyFromTo.Add(relativeNugetConfigPath, ".");
            }
        }

        return copyFromTo;
    }

    private Dictionary<string, string> PrepareCopyDictionary(string dockerfileContextDirectory, Project project)
    {
        var copyFromTo = new Dictionary<string, string>();

        HashSet<Project> visited = new();
        Stack<Project> projectStack = new();

        projectStack.Push(project);

        while (projectStack.Count > 0)
        {
            var projectToVisit = projectStack.Pop();
            if (visited.Contains(projectToVisit))
            {
                continue;
            }

            var projectDependencies = project.Dependencies.FirstOrDefault(x => x == projectToVisit)?.Dependencies ?? [];
            foreach (var dependency in projectDependencies)
            {
                projectStack.Push(dependency);
            }

            visited.Add(projectToVisit);
        }

        var dependencies = visited.Distinct().ToList();

        foreach(var dependency in dependencies)
        {
            var dependencyProjectFileDirectory = Path.GetDirectoryName(dependency.ProjectDirectory);

            var copyFrom = Path.GetRelativePath(dockerfileContextDirectory, dependency.FullPath).Replace("..\\","");
            var copyTo = Path.GetRelativePath(dockerfileContextDirectory, dependencyProjectFileDirectory!).Replace("..\\","");
            copyFromTo.Add(copyFrom, copyTo);
        }

        return copyFromTo;
    }

    private async Task SaveDockerfileAsync(string dockerfileContent, string dockerfileName, string destinationDirectory, CancellationToken ct = default)
    {
        var destination = Path.Combine(destinationDirectory, dockerfileName);

        await File.WriteAllTextAsync(destination, dockerfileContent, ct);
    }
}
