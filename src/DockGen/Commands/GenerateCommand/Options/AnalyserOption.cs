using System.CommandLine;
using System.CommandLine.Completions;
using DockGen.Generator.Constants;

namespace DockGen.Commands.GenerateCommand.Options;

public sealed class AnalyzerOption : Option<string>
{
    public AnalyzerOption() : base("--analyzer")
    {
        Description = $"""
                       The name of the analyzer to use. Available options: 
                       - {DockGenConstants.SimpleAnalyzerName} 
                       - {DockGenConstants.DesignBuildTimeAnalyzerName} (default)";
                       - {DockGenConstants.FastAnalyzerName}
                       """;
        Required = false;
        Aliases.Add("-a");
        DefaultValueFactory = _ => DockGenConstants.DesignBuildTimeAnalyzerName;
    }

    public override IEnumerable<CompletionItem> GetCompletions(CompletionContext context)
    {
        yield return new CompletionItem(DockGenConstants.SimpleAnalyzerName, "Simple analyzer that generates Dockerfile based on project type.");
        yield return new CompletionItem(DockGenConstants.DesignBuildTimeAnalyzerName, "Design build time analyzer that generates Dockerfile obtained via design build time information.");
        yield return new CompletionItem(DockGenConstants.FastAnalyzerName, "Design build time analyzer that generates Dockerfile obtained via design build time information.");
    }
}
