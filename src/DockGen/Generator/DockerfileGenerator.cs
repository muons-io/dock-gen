using System.Collections.Concurrent;
using System.Diagnostics;
using Buildalyzer;
using DockGen.Constants;
using DockGen.Generator.Extractors;
using DockGen.Generator.Models;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;

namespace DockGen.Generator;

public sealed class DockerfileGenerator(ILogger<DockerfileGenerator> logger, IExtractor extractor)
{
    private readonly ILogger<DockerfileGenerator> _logger = logger;
    private readonly IExtractor _extractor = extractor;
    
    private readonly ConcurrentDictionary<string, IAnalyzerResult> _analyzerCache = new();
    private readonly ConcurrentDictionary<string, List<string>> _dependencyTree = new();

    // TODO: pass arguments in single configuration parameter
    public async Task<ExitCodes> GenerateDockerfileAsync(string? targetFramework, string? solutionPath, string? projectPath, bool multiArch, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(solutionPath);
        
        await Task.Yield();
        
        _logger.LogInformation("Solution Path: {SolutionPath}, Project Path {ProjectPath}", solutionPath, projectPath);
        
        var manager = new AnalyzerManager(solutionPath);
        
        BuildDependencyTree(manager, projectPath, targetFramework, ct);
        
        var projects = manager.SolutionFile.ProjectsInOrder
            .Where(x => x.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
            .ToList();
        foreach (var currentProjectPath in projects.Select(x => x.AbsolutePath))
        {
            if (!string.IsNullOrEmpty(projectPath) && currentProjectPath != projectPath)
            {
                continue;
            }

            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Operation cancelled");
                return ExitCodes.Failure;
            }
            
            var projectFileDirectory = Path.GetDirectoryName(currentProjectPath);
            if (projectFileDirectory is null)
            {
                _logger.LogError("Failed to get project file directory");
                return ExitCodes.Failure;
            }
            
            var analyzerResult = _analyzerCache
                .Single(x => x.Value.ProjectFilePath == currentProjectPath)
                .Value;
            var outputTypeResult = await _extractor.Extract(new OutputTypeExtractRequest(analyzerResult), ct);
            if (!outputTypeResult.Extracted || !outputTypeResult.Value.Equals("Exe", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Skipping library project {ProjectPath}", currentProjectPath);
                continue;
            }
            
            var buildImageResult = await _extractor.Extract(new ContainerBuildImageExtractRequest(analyzerResult), ct);
            if (!buildImageResult.Extracted)
            {
                _logger.LogError("Failed to get build image");
                return ExitCodes.Failure;
            }
            
            var baseImageResult = await _extractor.Extract(new ContainerBaseImageExtractRequest(analyzerResult), ct);
            if (!baseImageResult.Extracted)
            {
                _logger.LogError("Failed to get base image");
                return ExitCodes.Failure;
            }
            
            var targetFileNameResult = await _extractor.Extract(new TargetFileNameExtractRequest(analyzerResult), ct);
            if (!targetFileNameResult.Extracted)
            {
                _logger.LogError("Failed to get target file name");
                return ExitCodes.Failure;
            }
            
            var copyFromTo = PrepareCopyDictionary(solutionPath, currentProjectPath);
            var initialCopyFromTo = PrepareInitialCopyDictionary(solutionPath, analyzerResult);
            
            var relativeProjectPath = Path.GetRelativePath(solutionPath, projectFileDirectory).Replace("..\\","");
            
            var containerPorts = await _extractor.Extract(new ContainerPortExtractRequest(analyzerResult), ct);
            
            var builder = new DockerfileBuilder
            {
                BaseImage = baseImageResult.Value,
                BuildImage = buildImageResult.Value,
                ProjectDirectory = relativeProjectPath,
                ProjectFile = analyzerResult.Analyzer.ProjectFile.Name,
                WorkDir = "/app",
                Copy = copyFromTo,
                AdditionalCopy = initialCopyFromTo,
                TargetFileName = targetFileNameResult.Value,
                Expose = containerPorts.Extracted ? containerPorts.Value : new List<ContainerPort>(),
                MultiArch = multiArch
            };
        
            var dockerfile = builder.Build();

            var dockerfileName = "Dockerfile";
            var destinationDirectory = projectFileDirectory;
            
            await SaveDockerfileAsync(dockerfile, dockerfileName, destinationDirectory, ct);
        }
        
        return ExitCodes.Success;
    }

    private static Dictionary<string, string> PrepareInitialCopyDictionary(string solutionPath, IAnalyzerResult analyzerResult)
    {
        // get Directory.Build.props, Directory.Build.targets, and NuGet.Config, Directory.Packages.props for project
        // based on project properties
        // and copy it to the root of the project
        var copyFromTo = new Dictionary<string, string>();
        
        if (analyzerResult.Properties.TryGetValue(MSBuildProperties.GeneralProperties.ImportDirectoryBuildProps, out var importDirectoryBuildProps)
            && importDirectoryBuildProps == MSBuildProperties.True && analyzerResult.Properties.TryGetValue(MSBuildProperties.GeneralProperties.DirectoryBuildPropsPath, out var directoryBuildPropsPath))
        {
            var relativeBuildPropsPath = Path.GetRelativePath(solutionPath, directoryBuildPropsPath).Replace("..\\","");
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
            var relativePackagesPropsPath = Path.GetRelativePath(solutionPath, directoryPackagesPropsPath).Replace("..\\","");
            var relativePackagesPropsDirectory = Path.GetDirectoryName(relativePackagesPropsPath)?.Replace("..\\","");
            if (string.IsNullOrEmpty(relativePackagesPropsDirectory))
            {
                relativePackagesPropsDirectory = ".";
            }
            
            copyFromTo.Add(relativePackagesPropsPath, relativePackagesPropsDirectory);
        }
        
        // check if solution directory contains nuget.config and if so add it to copyFromTo;
        var solutionDirectory = Path.GetDirectoryName(solutionPath);
        if (!string.IsNullOrEmpty(solutionDirectory))
        {
            // we need to also make sure we use the correct casing, as in linux the file system is case sensitive
            var nugetConfigPath = Directory.GetFiles(solutionDirectory, "nuget.config", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (!string.IsNullOrEmpty(nugetConfigPath))
            {
                var relativeNugetConfigPath = Path.GetRelativePath(solutionPath, nugetConfigPath).Replace("..\\","");
                copyFromTo.Add(relativeNugetConfigPath, ".");
            }
        }
        
        return copyFromTo;
    }
    
    private Dictionary<string, string> PrepareCopyDictionary(string solutionPath, string currentProjectPath)
    {
        var copyFromTo = new Dictionary<string, string>();
        
        HashSet<string> visited = new();
        Stack<string> projectStack = new();
        projectStack.Push(currentProjectPath);
        
        while (projectStack.Count > 0)
        {
            var project = projectStack.Pop();
            if (visited.Contains(project))
            {
                continue;
            }
            
            var projectDependencies = _dependencyTree[project];
            foreach (var dependency in projectDependencies)
            {
                projectStack.Push(dependency);
            }
            
            visited.Add(project);
        }
        
        var dependencies = visited.Distinct().ToList();
        
        foreach(var dependencyPath in dependencies)
        {
            var dependencyProjectFileDirectory = Path.GetDirectoryName(dependencyPath);
                
            var copyFrom = Path.GetRelativePath(solutionPath, dependencyPath).Replace("..\\","");
            var copyTo = Path.GetRelativePath(solutionPath, dependencyProjectFileDirectory!).Replace("..\\","");
            copyFromTo.Add(copyFrom, copyTo);
        }

        return copyFromTo;
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
    
    private async Task SaveDockerfileAsync(string dockerfileContent, string dockerfileName, string destinationDirectory, CancellationToken ct = default)
    {
        var destination = Path.Combine(destinationDirectory, dockerfileName);
        
        await File.WriteAllTextAsync(destination, dockerfileContent, ct);
    }
}