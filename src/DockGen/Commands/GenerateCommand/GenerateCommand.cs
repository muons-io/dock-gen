using System.CommandLine;
using DockGen.Commands.GenerateCommand.Options;

namespace DockGen.Commands.GenerateCommand;

public sealed class GenerateCommand : Command
{
    public static readonly ProjectOption ProjectOption = new ();
    public static readonly SolutionOption SolutionOption = new();
    
    public static readonly Argument<bool> NoSolutionOption = new("--no-solution", "Don't use solution file");

    public GenerateCommand() : base("generate", "Generate Dockerfile")
    {
        AddAlias("gen");
        AddAlias("g");
        
        AddOption(ProjectOption);
        AddOption(SolutionOption);
    }
}