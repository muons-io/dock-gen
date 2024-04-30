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
        
        var result = await _dockerfileGenerator.GenerateDockerfileAsync(null, solutionPath, projectPath);
        
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