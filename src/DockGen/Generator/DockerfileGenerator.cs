using DockGen.Generator.Properties;
using DockGen.Generator.Properties.Extractors;
using DockGen.Generator.Properties.Models;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace DockGen.Generator;

public sealed class DockerfileGenerator
{
    private readonly ILogger<DockerfileGenerator> _logger;
    private readonly IExtractor _extractor;
    private readonly IFileProvider _fileProvider;

    public DockerfileGenerator(ILogger<DockerfileGenerator> logger, IExtractor extractor, IFileProvider fileProvider)
    {
        _logger = logger;
        _extractor = extractor;
        _fileProvider = fileProvider;
    }

    public async Task<ExitCodes> GenerateDockerfileAsync(GeneratorConfiguration configuration, Project project, CancellationToken ct = default)
    {
        var outputTypeResult = await _extractor.ExtractAsync(new OutputTypeExtractRequest(project.Properties), ct);
        if (!outputTypeResult.Extracted || !outputTypeResult.Value.Equals("Exe", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogTrace("Skipping library project {ProjectPath}", project.FullPath);
            return ExitCodes.Failure;
        }

        var isTestProjectResult = await _extractor.ExtractAsync(new IsTestProjectExtractRequest(project.Properties), ct);
        if (isTestProjectResult.Extracted && isTestProjectResult.Value)
        {
            _logger.LogTrace("Skipping test project {ProjectPath}", project.FullPath);
            return ExitCodes.Failure;
        }

        var buildImageResult = await _extractor.ExtractAsync(new ContainerBuildImageExtractRequest(project.Properties), ct);
        if (!buildImageResult.Extracted)
        {
            _logger.LogError("Failed to get build image");
            return ExitCodes.Failure;
        }

        var baseImageResult = await _extractor.ExtractAsync(new ContainerBaseImageExtractRequest(project.Properties), ct);
        if (!baseImageResult.Extracted)
        {
            _logger.LogError("Failed to get base image");
            return ExitCodes.Failure;
        }

        var targetFileNameResult = await _extractor.ExtractAsync(new TargetFileNameExtractRequest(project.Properties), ct);
        if (!targetFileNameResult.Extracted)
        {
            _logger.LogError("Failed to get target file name");
            return ExitCodes.Failure;
        }

        var dockerfileContextDirectory = configuration.DockerfileContextDirectory;

        var copyFromTo = PrepareCopyDictionary(dockerfileContextDirectory, project);
        var initialCopyFromTo = InitialCopyDictionary(dockerfileContextDirectory, project);

        var relativeProjectPath = Path.GetRelativePath(dockerfileContextDirectory, project.ProjectDirectory);

        var containerPorts = await _extractor.ExtractAsync(new ContainerPortExtractRequest(project.Properties), ct);

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

        var destinationFile = Path.Combine(project.ProjectDirectory, dockerfileName);

        try
        {
            await SaveDockerfileAsync(dockerfile, destinationFile, ct);
            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Dockerfile");
            return ExitCodes.Failure;
        }
    }

    private static Dictionary<string, string> InitialCopyDictionary(string dockerfileContext, Project project)
    {
        var copyFromTo = new Dictionary<string, string>();

        foreach(var relevantFile in project.RelevantFiles)
        {
            // we need to make sure we use the correct casing, as in linux the file system is case sensitive
            var fileName = Path.GetFileName(relevantFile);
            if (string.IsNullOrEmpty(fileName))
            {
                continue;
            }

            var relativeFilePath = Path.GetRelativePath(dockerfileContext, relevantFile).Replace("..\\","");
            var relativeFileDirectory = Path.GetDirectoryName(relativeFilePath)?.Replace("..\\","");
            if (string.IsNullOrEmpty(relativeFileDirectory))
            {
                relativeFileDirectory = ".";
            }

            copyFromTo.Add(relativeFilePath, relativeFileDirectory);
        }

        return copyFromTo;
    }

    private Dictionary<string, string> PrepareCopyDictionary(string dockerfileContextDirectory, Project project)
    {
        var copyFromTo = new Dictionary<string, string>();

        var dependencies = project.Dependencies.ToList();

        // add the project file itself to the copy dictionary
        var projectFilePath = Path.Combine(project.ProjectDirectory, project.ProjectName);
        var projectFileDirectory = Path.GetDirectoryName(projectFilePath);
        var copyProjectFrom = Path.GetRelativePath(dockerfileContextDirectory, projectFilePath).Replace("..\\","");
        var copyProjectTo = Path.GetRelativePath(dockerfileContextDirectory, projectFileDirectory!).Replace("..\\","");
        copyFromTo.Add(copyProjectFrom, copyProjectTo);

        foreach(var dependency in dependencies)
        {
            var dependencyProjectFileDirectory = dependency.ProjectDirectory;

            var copyFrom = Path.GetRelativePath(dockerfileContextDirectory, dependency.FullPath).Replace("..\\","");
            var copyTo = Path.GetRelativePath(dockerfileContextDirectory, dependencyProjectFileDirectory!).Replace("..\\","");
            copyFromTo.Add(copyFrom, copyTo);
        }

        return copyFromTo;
    }

    private async Task SaveDockerfileAsync(string dockerfileContent, string dockerfilePath, CancellationToken ct = default)
    {
        await File.WriteAllTextAsync(dockerfilePath, dockerfileContent, ct);
    }
}
