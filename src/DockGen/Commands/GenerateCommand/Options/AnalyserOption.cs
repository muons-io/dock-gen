using System.CommandLine;
using System.CommandLine.Completions;
using DockGen.Generator.Constants;

namespace DockGen.Commands.GenerateCommand.Options;

public sealed class AnalyserOption : Option<string>
{
    public AnalyserOption() : base("--analyser", $"The name of the analyser to use. Available options: {DockGenConstants.SimpleAnalyserName}, {DockGenConstants.DesignBuildTimeAnalyserName} (default)")
    {
        IsRequired = false;
        AddAlias("-a");
        SetDefaultValue(DockGenConstants.DesignBuildTimeAnalyserName);
    }

    public override IEnumerable<CompletionItem> GetCompletions(CompletionContext context)
    {
        yield return new CompletionItem(DockGenConstants.SimpleAnalyserName, "Simple analyser that generates Dockerfile based on project type.");
        yield return new CompletionItem(DockGenConstants.DesignBuildTimeAnalyserName, "Design build time analyser that generates Dockerfile obtained via design build time information.");
    }
}
