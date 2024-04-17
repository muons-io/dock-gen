using System.CommandLine;

namespace DockGen.Commands.GenerateCommand.Options;

public sealed class SolutionOption : Option<string>
{
    public SolutionOption() : base("--solution", "The path to the solution file")
    {
        AddAlias("-s");
    }
}