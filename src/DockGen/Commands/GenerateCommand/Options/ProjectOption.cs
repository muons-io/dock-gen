using System.CommandLine;

namespace DockGen.Commands.GenerateCommand.Options;

public sealed class ProjectOption : Option<string>
{
    public ProjectOption() : base("--project")
    {
        Description = "The path to the project file to generate the Dockerfile for";
        Aliases.Add("-p");
    }
}
