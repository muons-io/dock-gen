using System.CommandLine;
using DockGen.Commands.GenerateCommand.Options;

namespace DockGen.Commands.GenerateCommand;

public sealed class GenerateCommand : Command
{
    public static readonly DirectoryOption DirectoryOption = new ();
    public static readonly SolutionOption SolutionOption = new();
    public static readonly ProjectOption ProjectOption = new ();
    public static readonly AnalyzerOption AnalyzerOption = new ();

    public static readonly Argument<bool> MultiArchOption = new("--multi-arch")
    {
        Description = "Build for multiple architectures. Default is true",
        DefaultValueFactory = _ => true
    };

    public GenerateCommand() : base("generate", "Generate Dockerfile")
    {
        Aliases.Add("gen");
        Aliases.Add("g");

        Options.Add(DirectoryOption);
        Options.Add(SolutionOption);
        Options.Add(ProjectOption);
        Options.Add(AnalyzerOption);

        Arguments.Add(MultiArchOption);
    }
}
