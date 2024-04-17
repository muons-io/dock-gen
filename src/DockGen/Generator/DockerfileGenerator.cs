using System.Collections.Concurrent;
using Buildalyzer;
using DockGen.Constants;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;

namespace DockGen.Generator;

public sealed class DockerfileGenerator(ILogger<DockerfileGenerator> logger)
{
    private readonly ILogger<DockerfileGenerator> _logger = logger;
    private readonly ConcurrentDictionary<string, IAnalyzerResult> _analyzerCache = new();
    private readonly ConcurrentDictionary<string, List<string>> _dependencyTree = new();

    public async Task<ExitCodes> GenerateDockerfileAsync(string? targetFramework, string? solutionPath, string? projectPath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(solutionPath);
        
        await Task.Yield();
        
        _logger.LogInformation("Solution Path: {SolutionPath}, Project Path {ProjectPath}", solutionPath, projectPath);
        
        var manager = new AnalyzerManager(solutionPath);
        
        BuildDependencyTree(manager, projectPath, targetFramework);
        
        var projects = manager.SolutionFile.ProjectsInOrder
            .Where(x => x.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
            .ToList();
        foreach (var currentProjectPath in projects.Select(x => x.AbsolutePath))
        {
            if (!string.IsNullOrEmpty(projectPath) && currentProjectPath != projectPath)
            {
                continue;
            }
            
            var analyzerResult = _analyzerCache
                .Single(x => x.Value.ProjectFilePath == currentProjectPath)
                .Value;
            if (analyzerResult.Properties.TryGetValue(MSBuildProperties.GeneralProperties.OutputType, out var outputType)
                && outputType != "Exe")
            {
                _logger.LogInformation("Skipping library project {ProjectPath}", currentProjectPath);
                continue;
            }
            
            if (!DockerfileBuilderHelpers.TryGetBuildImage(analyzerResult, out var buildImage))
            {
                _logger.LogError("Failed to get build image");
                return ExitCodes.Failure;
            }
        
            if (!DockerfileBuilderHelpers.TryGetBaseImage(analyzerResult, out var baseImage))
            {
                _logger.LogError("Failed to get base image");
                return ExitCodes.Failure;
            }
            
            if (!analyzerResult.Properties.TryGetValue(MSBuildProperties.GeneralProperties.TargetFileName, out var targetFileName) || string.IsNullOrEmpty(targetFileName))
            {
                _logger.LogError("Failed to get target file name");
                return ExitCodes.Failure;
            }
            
            var projectFileDirectory = Path.GetDirectoryName(currentProjectPath);
            if (projectFileDirectory is null)
            {
                _logger.LogError("Failed to get project file directory");
                return ExitCodes.Failure;
            }

            var dependencies = _dependencyTree[currentProjectPath];
            
            var copyFromTo = PrepareCopyDictionary(solutionPath, dependencies);
            var initialCopyFromTo = PrepareInitialCopyDictionary(solutionPath, analyzerResult);
            
            var relativeProjectPath = Path.GetRelativePath(solutionPath, projectFileDirectory).Replace("..\\","");
            
            var builder = new DockerfileBuilder
            {
                BaseImage = baseImage,
                BuildImage = buildImage,
                ProjectDirectory = relativeProjectPath,
                ProjectFile = analyzerResult.Analyzer.ProjectFile.Name,
                WorkDir = "/app",
                Copy = copyFromTo,
                InitialCopy = initialCopyFromTo,
                TargetFileName = targetFileName
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
        
        return copyFromTo;
    }
    
    private static Dictionary<string, string> PrepareCopyDictionary(string solutionPath, List<string> dependencies)
    {
        var copyFromTo = new Dictionary<string, string>();
        foreach(var dependencyPath in dependencies)
        {
            var dependencyProjectFileDirectory = Path.GetDirectoryName(dependencyPath);
                
            var copyFrom = Path.GetRelativePath(solutionPath, dependencyPath).Replace("..\\","");
            var copyTo = Path.GetRelativePath(solutionPath, dependencyProjectFileDirectory!).Replace("..\\","");
            copyFromTo.Add(copyFrom, copyTo);
        }

        return copyFromTo;
    }

    private void BuildDependencyTree(IAnalyzerManager manager, string? projectPath, string? targetFramework)
    {
        var projects = manager.SolutionFile.ProjectsInOrder
            .Where(x => x.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
            .ToList();

        Parallel.ForEach(projects.Select(x => x.AbsolutePath), currentProjectPath =>
        {
            if (!string.IsNullOrEmpty(projectPath) && currentProjectPath != projectPath)
            {
                return;
            }

            var projectAnalyzer = manager.GetProject(currentProjectPath);

            var projectDependencies = new Dictionary<string, IAnalyzerResult>();
            EvaluateProjectReferences(ref projectDependencies, manager, targetFramework, projectAnalyzer);

            foreach (var dependency in projectDependencies)
            {
                if (_analyzerCache.ContainsKey(dependency.Key))
                {
                    continue;
                }

                _analyzerCache.TryAdd(dependency.Key, dependency.Value);
            }
            
            _dependencyTree.TryAdd(currentProjectPath, projectDependencies.Keys.ToList());

        });
    }

    private void EvaluateProjectReferences(ref Dictionary<string, IAnalyzerResult> projects, IAnalyzerManager manager, string? targetFramework, IProjectAnalyzer projectAnalyzer)
    {
        if (projects.TryGetValue(projectAnalyzer.ProjectFile.Path, out _))
        {
            return;
        }
        
        var analyzerResults = projectAnalyzer.Build();
        if (string.IsNullOrEmpty(targetFramework) && analyzerResults.TargetFrameworks.Any())
        {
            targetFramework = analyzerResults.TargetFrameworks.First();
        }
        
        if (!analyzerResults.TryGetTargetFramework(targetFramework, out var analyzerResult))
        {
            _logger.LogWarning("Failed to analyze project for target framework {TargetFramework}", targetFramework);
            return;
        }
        
        projects.Add(analyzerResult.ProjectFilePath, analyzerResult);
        
        foreach (var reference in analyzerResult.ProjectReferences)
        {
            if (projects.ContainsKey(reference))
            {
                continue;
            }

            var projectDependency = manager.GetProject(reference);
            
            EvaluateProjectReferences(ref projects, manager, targetFramework, projectDependency);
        }
    }

    private async Task SaveDockerfileAsync(string dockerfileContent, string dockerfileName, string destinationDirectory, CancellationToken ct = default)
    {
        var destination = Path.Combine(destinationDirectory, dockerfileName);
        
        await File.WriteAllTextAsync(destination, dockerfileContent, ct);
    }
}