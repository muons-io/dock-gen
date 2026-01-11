using System.CommandLine;

namespace DockGen.Logging;

public static class LogLevelOptions
{
    public static readonly Option<bool> Detailed = new("--verbose")
    {
        Description = "Enable detailed logging (includes trace output).",
        Arity = ArgumentArity.Zero,
        Recursive = true
    };

    static LogLevelOptions()
    {
        Detailed.Aliases.Add("--debug");
        Detailed.Aliases.Add("--trace");
    }
}
