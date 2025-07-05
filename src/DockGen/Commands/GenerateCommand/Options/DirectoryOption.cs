using System.CommandLine;

namespace DockGen.Commands.GenerateCommand.Options;

public sealed class DirectoryOption : Option<string>
{
    public DirectoryOption() : base("--directory", "The path to the directory")
    {
        AddAlias("-d");
    }
}