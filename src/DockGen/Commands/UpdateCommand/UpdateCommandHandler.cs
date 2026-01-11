using System.CommandLine;
using DockGen.Commands.GenerateCommand;
using DockGen.Generator;
using DockGen.Generator.Constants;
using Microsoft.Extensions.Logging;

namespace DockGen.Commands.UpdateCommand;

public sealed class UpdateCommandHandler : ICommandHandler<UpdateCommand>
{
    private readonly ILogger<UpdateCommandHandler> _logger;
    private readonly DockerfileGenerator _dockerfileGenerator;
    private readonly IAnalyzer _analyzer;

    public UpdateCommandHandler(
        ILogger<UpdateCommandHandler> logger,
        DockerfileGenerator dockerfileGenerator,
        IAnalyzer analyzer)
    {
        _logger = logger;
        _dockerfileGenerator = dockerfileGenerator;
        _analyzer = analyzer;
    }

    public async Task<int> HandleAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var directoryPath = parseResult.GetValue(UpdateCommand.DirectoryOption);
        var solutionPath = parseResult.GetValue(UpdateCommand.SolutionOption);
        var projectPath = parseResult.GetValue(UpdateCommand.ProjectOption);
        var analyzerOption = parseResult.GetValue(UpdateCommand.AnalyzerOption);

        var multiArch = parseResult.GetValue(UpdateCommand.MultiArchOption);
        var onlyReferences = parseResult.GetValue(UpdateCommand.OnlyReferencesOption);

        var workingDirectory = GetWorkingDirectory(directoryPath, solutionPath, projectPath);

        var analyzerRequest = new AnalyzerRequest(
            WorkingDirectory: workingDirectory,
            RelativeDirectory: string.IsNullOrEmpty(directoryPath) ? null : Path.GetRelativePath(workingDirectory, directoryPath),
            RelativeSolutionPath: string.IsNullOrEmpty(solutionPath) ? null : Path.GetRelativePath(workingDirectory, solutionPath),
            RelativeProjectPath: string.IsNullOrEmpty(projectPath) ? null : Path.GetRelativePath(workingDirectory, projectPath),
            Analyzer: analyzerOption ?? DockGenConstants.SimpleAnalyzerName
        );

        var projects = await _analyzer.AnalyseAsync(analyzerRequest, cancellationToken);

        var generatorConfiguration = new GeneratorConfiguration
        {
            DockerfileContextDirectory = workingDirectory,
            MultiArch = multiArch,
        };

        _logger.LogInformation("Updating Dockerfiles for {ProjectCount} projects", projects.Count);

        try
        {
            foreach (var project in projects)
            {
                await _dockerfileGenerator.UpdateDockerfileAsync(generatorConfiguration, project, onlyReferences, cancellationToken);
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Dockerfiles");
            return 1;
        }
    }

    private static string GetWorkingDirectory(string? directoryPath, string? solutionPath, string? projectPath)
    {
        if (!string.IsNullOrEmpty(directoryPath))
        {
            return Path.GetFullPath(directoryPath);
        }

        if (!string.IsNullOrEmpty(solutionPath))
        {
            return Path.GetDirectoryName(Path.GetFullPath(solutionPath)) ?? Directory.GetCurrentDirectory();
        }

        if (!string.IsNullOrEmpty(projectPath))
        {
            return Path.GetDirectoryName(Path.GetFullPath(projectPath)) ?? Directory.GetCurrentDirectory();
        }

        return Directory.GetCurrentDirectory();
    }
}
