using System.CommandLine;
using DockGen.Generator;
using DockGen.Generator.Constants;
using Microsoft.Extensions.Logging;

namespace DockGen.Commands.GenerateCommand;

public sealed class GenerateCommandHandler : ICommandHandler<GenerateCommand>
{
    private readonly ILogger<GenerateCommandHandler> _logger;
    private readonly DockerfileGenerator _dockerfileGenerator;
    private readonly IAnalyzer _analyzer;

    public GenerateCommandHandler(
        ILogger<GenerateCommandHandler> logger,
        DockerfileGenerator dockerfileGenerator,
        IAnalyzer analyzer)
    {
        _logger = logger;
        _dockerfileGenerator = dockerfileGenerator;
        _analyzer = analyzer;
    }

    public async Task<int> HandleAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var directoryPath = parseResult.GetValue(GenerateCommand.DirectoryOption);
        var solutionPath = parseResult.GetValue(GenerateCommand.SolutionOption);
        var projectPath = parseResult.GetValue(GenerateCommand.ProjectOption);
        var analyzerOption = parseResult.GetValue(GenerateCommand.AnalyzerOption);

        var multiArch = parseResult.GetValue(GenerateCommand.MultiArchOption);

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

        _logger.LogInformation("Generating Dockerfiles for {ProjectCount} projects", projects.Count);

        try
        {
            foreach(var project in projects)
            {
                await _dockerfileGenerator.GenerateDockerfileAsync(generatorConfiguration, project, cancellationToken);
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Dockerfiles");
            return 1;
        }
    }

    private string GetWorkingDirectory(string? directoryPath, string? solutionPath, string? projectPath)
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
