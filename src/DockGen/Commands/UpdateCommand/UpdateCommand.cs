using System.CommandLine;
using DockGen.Commands.GenerateCommand.Options;

namespace DockGen.Commands.UpdateCommand;

public sealed class UpdateCommand : Command
{
    public static readonly DirectoryOption DirectoryOption = new();
    public static readonly SolutionOption SolutionOption = new();
    public static readonly ProjectOption ProjectOption = new();
    public static readonly AnalyzerOption AnalyzerOption = new();

    public static readonly Argument<bool> MultiArchOption = new("--multi-arch")
    {
        Description = "Build for multiple architectures. Default is true",
        DefaultValueFactory = _ => true
    };

    public static readonly Argument<bool> OnlyReferencesOption = new("--only-references")
    {
        Description = "Update only the project reference COPY section in existing Dockerfile. Default is false",
        DefaultValueFactory = _ => false
    };

    public UpdateCommand() : base("update", "Update existing Dockerfile")
    {
        Aliases.Add("upd");
        Aliases.Add("u");

        Options.Add(DirectoryOption);
        Options.Add(SolutionOption);
        Options.Add(ProjectOption);
        Options.Add(AnalyzerOption);

        Arguments.Add(MultiArchOption);
        Arguments.Add(OnlyReferencesOption);
    }
}
