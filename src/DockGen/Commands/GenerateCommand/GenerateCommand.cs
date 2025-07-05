using System.CommandLine;
using DockGen.Commands.GenerateCommand.Options;

namespace DockGen.Commands.GenerateCommand;

public sealed class GenerateCommand : Command
{
    public static readonly DirectoryOption DirectoryOption = new ();
    public static readonly SolutionOption SolutionOption = new();
    public static readonly ProjectOption ProjectOption = new ();
    public static readonly AnalyzerOption AnalyzerOption = new ();

    public static readonly Argument<bool> NoSolutionOption = new("--no-solution", "Don't use solution file. Default is false");
    public static readonly Argument<bool> MultiArchOption = new("--multi-arch", () => true, "Build for multiple architectures. Default is true");

    public GenerateCommand() : base("generate", "Generate Dockerfile")
    {
        AddAlias("gen");
        AddAlias("g");

        AddOption(DirectoryOption);
        AddOption(SolutionOption);
        AddOption(ProjectOption);
        AddOption(AnalyzerOption);

        AddArgument(NoSolutionOption);
        AddArgument(MultiArchOption);
    }
}
