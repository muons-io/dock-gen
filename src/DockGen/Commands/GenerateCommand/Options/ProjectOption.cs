using System.CommandLine;

namespace DockGen.Commands.GenerateCommand.Options;

public sealed class ProjectOption : Option<string>
{
    public ProjectOption() : base("--project", "The path to the project file")
    {
        AddAlias("-p");
    }
}