using DockGen.Generator.Properties;
using DockGen.Generator.Properties.Extractors;
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

    public async Task GenerateDockerfileAsync(GeneratorConfiguration configuration, Project project, CancellationToken ct = default)
    {
        var outputTypeResult = await _extractor.ExtractAsync(new OutputTypeExtractRequest(project.Properties), ct);
        if (!outputTypeResult.Extracted || !outputTypeResult.Value.Equals("Exe", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogTrace(
                "Skipping Dockerfile for project {ProjectPath} because OutputType is not Exe (Extracted={Extracted}, Value={Value})",
                project.FullPath,
                outputTypeResult.Extracted,
                outputTypeResult.Extracted ? outputTypeResult.Value : "<missing>");
            return;
        }

        var isTestProjectResult = await _extractor.ExtractAsync(new IsTestProjectExtractRequest(project.Properties), ct);
        if (isTestProjectResult.Extracted && isTestProjectResult.Value)
        {
            _logger.LogTrace("Skipping Dockerfile for test project {ProjectPath}", project.FullPath);
            return;
        }

        var buildImageResult = await _extractor.ExtractAsync(new ContainerBuildImageExtractRequest(project.Properties), ct);
        if (!buildImageResult.Extracted)
        {
            _logger.LogError("Failed to get build image");
            return;
        }

        var baseImageResult = await _extractor.ExtractAsync(new ContainerBaseImageExtractRequest(project.Properties), ct);
        if (!baseImageResult.Extracted)
        {
            _logger.LogError("Failed to get base image");
            return;
        }

        var targetExtResult = await _extractor.ExtractAsync(new TargetExtExtractRequest(project.Properties), ct);
        var targetExt = targetExtResult.Extracted ? targetExtResult.Value : string.Empty;
        if (string.IsNullOrWhiteSpace(targetExt))
        {
            targetExt = ".dll";
        }

        string targetName;
        var targetFileNameResult = await _extractor.ExtractAsync(new TargetNameExtractRequest(project.Properties), ct);
        if (!targetFileNameResult.Extracted)
        {
            targetName = Path.GetFileNameWithoutExtension(project.ProjectName);
        }
        else
        {
            targetName = targetFileNameResult.Value;
        }

        var dockerfileContextDirectory = configuration.DockerfileContextDirectory;

        var copyFromTo = PrepareCopyDictionary(dockerfileContextDirectory, project);
        var initialCopyFromTo = InitialCopyDictionary(dockerfileContextDirectory, project);

        var relativeProjectPath = Path.GetRelativePath(dockerfileContextDirectory, project.ProjectDirectory);

        var builder = new DockerfileBuilder
        {
            BaseImage = baseImageResult.Value,
            BuildImage = buildImageResult.Value,
            ProjectDirectory = relativeProjectPath,
            ProjectFile = project.ProjectName,
            WorkDir = "/app",
            Copy = copyFromTo,
            AdditionalCopy = initialCopyFromTo,
            TargetFileName = $"{targetName}{targetExt}",
            MultiArch = configuration.MultiArch
        };

        var dockerfile = builder.Build();

        var dockerfileName = "Dockerfile";

        var destinationFile = Path.Combine(project.ProjectDirectory, dockerfileName);

        try
        {
            _logger.LogInformation("Saving Dockerfile for project: {ProjectName}", project.ProjectName);
            await SaveDockerfileAsync(dockerfile, destinationFile, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Dockerfile");
        }
    }

    public async Task UpdateDockerfileAsync(GeneratorConfiguration configuration, Project project, bool onlyReferences, CancellationToken ct = default)
    {
        var outputTypeResult = await _extractor.ExtractAsync(new OutputTypeExtractRequest(project.Properties), ct);
        if (!outputTypeResult.Extracted || !outputTypeResult.Value.Equals("Exe", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogTrace(
                "Skipping Dockerfile update for project {ProjectPath} because OutputType is not Exe (Extracted={Extracted}, Value={Value})",
                project.FullPath,
                outputTypeResult.Extracted,
                outputTypeResult.Extracted ? outputTypeResult.Value : "<missing>");
            return;
        }

        var isTestProjectResult = await _extractor.ExtractAsync(new IsTestProjectExtractRequest(project.Properties), ct);
        if (isTestProjectResult.Extracted && isTestProjectResult.Value)
        {
            _logger.LogTrace("Skipping Dockerfile update for test project {ProjectPath}", project.FullPath);
            return;
        }

        var buildImageResult = await _extractor.ExtractAsync(new ContainerBuildImageExtractRequest(project.Properties), ct);
        if (!buildImageResult.Extracted)
        {
            _logger.LogError("Failed to get build image");
            return;
        }

        var baseImageResult = await _extractor.ExtractAsync(new ContainerBaseImageExtractRequest(project.Properties), ct);
        if (!baseImageResult.Extracted)
        {
            _logger.LogError("Failed to get base image");
            return;
        }

        var targetExtResult = await _extractor.ExtractAsync(new TargetExtExtractRequest(project.Properties), ct);
        var targetExt = targetExtResult.Extracted ? targetExtResult.Value : string.Empty;
        if (string.IsNullOrWhiteSpace(targetExt))
        {
            targetExt = ".dll";
        }

        string targetName;
        var targetFileNameResult = await _extractor.ExtractAsync(new TargetNameExtractRequest(project.Properties), ct);
        if (!targetFileNameResult.Extracted)
        {
            targetName = Path.GetFileNameWithoutExtension(project.ProjectName);
        }
        else
        {
            targetName = targetFileNameResult.Value;
        }

        var dockerfileContextDirectory = configuration.DockerfileContextDirectory;

        var copyFromTo = PrepareCopyDictionary(dockerfileContextDirectory, project);
        var initialCopyFromTo = InitialCopyDictionary(dockerfileContextDirectory, project);

        var relativeProjectPath = Path.GetRelativePath(dockerfileContextDirectory, project.ProjectDirectory);

        var builder = new DockerfileBuilder
        {
            BaseImage = baseImageResult.Value,
            BuildImage = buildImageResult.Value,
            ProjectDirectory = relativeProjectPath,
            ProjectFile = project.ProjectName,
            WorkDir = "/app",
            Copy = copyFromTo,
            AdditionalCopy = initialCopyFromTo,
            TargetFileName = $"{targetName}{targetExt}",
            MultiArch = configuration.MultiArch
        };

        var dockerfileName = "Dockerfile";
        var destinationFile = Path.Combine(project.ProjectDirectory, dockerfileName);

        if (!File.Exists(destinationFile))
        {
            _logger.LogWarning("Dockerfile not found for project: {ProjectName}", project.ProjectName);
            return;
        }

        try
        {
            if (!onlyReferences)
            {
                var dockerfile = builder.Build();
                _logger.LogInformation("Updating Dockerfile for project: {ProjectName}", project.ProjectName);
                await SaveDockerfileAsync(dockerfile, destinationFile, ct);
                return;
            }

            var original = await File.ReadAllTextAsync(destinationFile, ct);
            var newCopyBlock = builder.BuildCopyRestoreBlock();

            if (!DockerfileCopySectionUpdater.TryUpdate(original, newCopyBlock, out var updated))
            {
                _logger.LogWarning("Failed to locate COPY section in Dockerfile for project: {ProjectName}", project.ProjectName);
                return;
            }

            _logger.LogInformation("Updating COPY section in Dockerfile for project: {ProjectName}", project.ProjectName);
            await SaveDockerfileAsync(updated, destinationFile, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Dockerfile");
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

    private static Dictionary<string, string> PrepareCopyDictionary(string dockerfileContextDirectory, Project project)
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
            var copyTo = Path.GetRelativePath(dockerfileContextDirectory, dependencyProjectFileDirectory).Replace("..\\","");
            copyFromTo.Add(copyFrom, copyTo);
        }

        return copyFromTo;
    }

    private static async Task SaveDockerfileAsync(string dockerfileContent, string dockerfilePath, CancellationToken ct = default)
    {
        await File.WriteAllTextAsync(dockerfilePath, dockerfileContent, ct);
    }
}
