using System.CommandLine;

namespace DockGen.Commands.GenerateCommand.Options;

public sealed class DirectoryOption : Option<string>
{
    public DirectoryOption() : base("--directory")
    {
        Description = "The path to the directory";
        Aliases.Add("-d");
    }
}
