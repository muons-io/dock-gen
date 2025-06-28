using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using DockGen.Generator;
using Microsoft.Extensions.Logging;

namespace DockGen.Commands.GenerateCommand;

public sealed class GenerateCommandHandler : ICommandHandler
{
    private readonly ILogger<GenerateCommandHandler> _logger;
    private readonly DockerfileGenerator _dockerfileGenerator;

    public GenerateCommandHandler(
        ILogger<GenerateCommandHandler> logger,
        DockerfileGenerator dockerfileGenerator)
    {
        _logger = logger;
        _dockerfileGenerator = dockerfileGenerator;
    }

    public int Invoke(InvocationContext context)
    {
        throw new NotImplementedException();
    }

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        await Task.Yield();

        var projectPath = context.ParseResult.GetValueForOption(GenerateCommand.ProjectOption);
        var solutionPath = context.ParseResult.GetValueForOption(GenerateCommand.SolutionOption);
        var noSolution = context.ParseResult.GetValueForArgument(GenerateCommand.NoSolutionOption);
        var multiArch = context.ParseResult.GetValueForArgument(GenerateCommand.MultiArchOption);

        if (string.IsNullOrEmpty(solutionPath) && !TryFindSolutionPath(out solutionPath) && !noSolution)
        {
            _logger.LogError("Failed to find solution path");
            return (int)ExitCodes.Failure;
        }

        if (!string.IsNullOrEmpty(solutionPath) && !File.Exists(solutionPath))
        {
            _logger.LogError("Solution file does not exist");
            return (int)ExitCodes.Failure;
        }

        if (!string.IsNullOrEmpty(projectPath) && !File.Exists(projectPath))
        {
            _logger.LogError("Project file does not exist");
            return (int)ExitCodes.Failure;
        }

        var configuration = new DockerfileGeneratorConfiguration
        {
            TargetFramework = null,
            SolutionPath = solutionPath,
            ProjectPath = projectPath,
            MultiArch = multiArch,
        };

        var generatorConfiguration = new GeneratorConfiguration
        {
            DockerfileContextDirectory = Directory.GetCurrentDirectory(),
            MultiArch = multiArch,
        };

        // var result = await _dockerfileGenerator.GenerateDockerfileAsync(generatorConfiguration);

        var result = 0;
        return (int)result;
    }

    private bool TryFindSolutionPath([NotNullWhen(true)] out string? solutionPath)
    {
        solutionPath = null;
        try
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var solutionFiles = Directory.GetFiles(currentDirectory, "*.sln?");
            if (solutionFiles.Length == 0)
            {
                _logger.LogError("No solution files found");
                return false;
            }

            if (solutionFiles.Length > 1)
            {
                _logger.LogError("Multiple solution files found");
                return false;
            }

            solutionPath = solutionFiles[0];
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find solution path");
            solutionPath = null;
            return false;
        }
    }
}



public sealed class UpdateCommandHandler : ICommandHandler
{
    private readonly ILogger<UpdateCommandHandler> _logger;
    private readonly DockerfileGenerator _dockerfileGenerator;

    public UpdateCommandHandler(
        ILogger<UpdateCommandHandler> logger,
        DockerfileGenerator dockerfileGenerator)
    {
        _logger = logger;
        _dockerfileGenerator = dockerfileGenerator;
    }

    public int Invoke(InvocationContext context)
    {
        throw new NotImplementedException();
    }

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        await Task.Yield();

        var projectPath = context.ParseResult.GetValueForOption(UpdateCommand.ProjectOption);
        var solutionPath = context.ParseResult.GetValueForOption(UpdateCommand.SolutionOption);
        var noSolution = context.ParseResult.GetValueForArgument(UpdateCommand.NoSolutionOption);
        var multiArch = context.ParseResult.GetValueForArgument(UpdateCommand.MultiArchOption);
        var onlyUpdateCopy = context.ParseResult.GetValueForArgument(UpdateCommand.OnlyUpdateCopyOption);

        if (string.IsNullOrEmpty(solutionPath) && !TryFindSolutionPath(out solutionPath) && !noSolution)
        {
            _logger.LogError("Failed to find solution path");
            return (int)ExitCodes.Failure;
        }

        if (!string.IsNullOrEmpty(solutionPath) && !File.Exists(solutionPath))
        {
            _logger.LogError("Solution file does not exist");
            return (int)ExitCodes.Failure;
        }

        if (!string.IsNullOrEmpty(projectPath) && !File.Exists(projectPath))
        {
            _logger.LogError("Project file does not exist");
            return (int)ExitCodes.Failure;
        }

        var configuration = new DockerfileGeneratorConfiguration
        {
            TargetFramework = null,
            SolutionPath = solutionPath,
            ProjectPath = projectPath,
            MultiArch = multiArch,
            OnlyUpdateCopyIfExists = onlyUpdateCopy
        };

        // var result = await _dockerfileGenerator.GenerateDockerfileAsync(configuration);

        var result = 0;
        return (int)result;
    }

    private bool TryFindSolutionPath([NotNullWhen(true)] out string? solutionPath)
    {
        solutionPath = null;
        try
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var solutionFiles = Directory.GetFiles(currentDirectory, "*.sln?");
            if (solutionFiles.Length == 0)
            {
                _logger.LogError("No solution files found");
                return false;
            }

            if (solutionFiles.Length > 1)
            {
                _logger.LogError("Multiple solution files found");
                return false;
            }

            solutionPath = solutionFiles[0];
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find solution path");
            solutionPath = null;
            return false;
        }
    }
}

public static class SolutionPathHelper
{
    public static bool TryFindSolutionPath([NotNullWhen(true)] out string? solutionPath)
    {
        solutionPath = null;
        try
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var solutionFiles = Directory.GetFiles(currentDirectory, "*.sln?");
            if (solutionFiles.Length == 0)
            {
                return false;
            }

            if (solutionFiles.Length > 1)
            {
                return false;
            }

            solutionPath = solutionFiles[0];
            return true;
        }
        catch
        {
            solutionPath = null;
            return false;
        }
    }
}
