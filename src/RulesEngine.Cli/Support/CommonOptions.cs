using System.CommandLine;

namespace RulesEngine.Cli.Support;

/// <summary>--path/--config are shared by every command that resolves a CliRepositoryContext.</summary>
public static class CommonOptions
{
    public static Option<string?> CreatePathOption() => new("--path")
    {
        Description = "Repository root to operate against (default: current directory)."
    };

    public static Option<string?> CreateConfigOption() => new("--config")
    {
        Description = "Explicit .rulesengine config.yml path (default: <path>/.rulesengine/config.yml if present)."
    };
}
