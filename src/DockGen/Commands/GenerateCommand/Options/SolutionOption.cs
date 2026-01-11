using System.CommandLine;

namespace DockGen.Commands.GenerateCommand.Options;

public sealed class SolutionOption : Option<string>
{
    public SolutionOption() : base("--solution")
    {
        Description = "The path to the solution file";
        Aliases.Add("-s");
    }
}
