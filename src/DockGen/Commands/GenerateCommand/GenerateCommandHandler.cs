using System.CommandLine.Invocation;
using DockGen.Generator;
using DockGen.Generator.Constants;
using Microsoft.Extensions.Logging;

namespace DockGen.Commands.GenerateCommand;

public sealed class GenerateCommandHandler : ICommandHandler
{
    private readonly ILogger<GenerateCommandHandler> _logger;
    private readonly DockerfileGenerator _dockerfileGenerator;
    private readonly IAnalyser _analyser;

    public GenerateCommandHandler(
        ILogger<GenerateCommandHandler> logger,
        DockerfileGenerator dockerfileGenerator,
        IAnalyser analyser)
    {
        _logger = logger;
        _dockerfileGenerator = dockerfileGenerator;
        _analyser = analyser;
    }

    public int Invoke(InvocationContext context)
    {
        throw new NotImplementedException();
    }

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var directoryPath = context.ParseResult.GetValueForOption(GenerateCommand.DirectoryOption);
        var solutionPath = context.ParseResult.GetValueForOption(GenerateCommand.SolutionOption);
        var projectPath = context.ParseResult.GetValueForOption(GenerateCommand.ProjectOption);
        var analyserOption = context.ParseResult.GetValueForOption(GenerateCommand.AnalyserOption);

        var multiArch = context.ParseResult.GetValueForArgument(GenerateCommand.MultiArchOption);

        var workingDirectory = GetWorkingDirectory(directoryPath, solutionPath, projectPath);

        var analyserRequest = new AnalyserRequest(
            WorkingDirectory: workingDirectory,
            RelativeDirectory: string.IsNullOrEmpty(directoryPath) ? null : Path.GetRelativePath(workingDirectory, directoryPath),
            RelativeSolutionPath: string.IsNullOrEmpty(solutionPath) ? null : Path.GetRelativePath(workingDirectory, solutionPath),
            RelativeProjectPath: string.IsNullOrEmpty(projectPath) ? null : Path.GetRelativePath(workingDirectory, projectPath),
            Analyser: analyserOption ?? DockGenConstants.SimpleAnalyserName
        );

        var projects = await _analyser.AnalyseAsync(analyserRequest, context.GetCancellationToken());

        var generatorConfiguration = new GeneratorConfiguration
        {
            DockerfileContextDirectory = workingDirectory,
            MultiArch = multiArch,
        };

        foreach(var project in projects)
        {
            await _dockerfileGenerator.GenerateDockerfileAsync(generatorConfiguration, project);
            _logger.LogInformation("Dockerfile generated for project: {ProjectName}", project.ProjectName);
        }

        var result = 0;
        return result;
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
