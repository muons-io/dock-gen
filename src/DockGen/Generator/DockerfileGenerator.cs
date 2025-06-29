using DockGen.Constants;
using DockGen.Generator.Extractors;
using DockGen.Generator.Models;
using Microsoft.Extensions.Logging;

namespace DockGen.Generator;

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
