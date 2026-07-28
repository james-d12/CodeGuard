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

    public static Option<string?> CreateRulesSourceOption() => new("--rules-source")
    {
        Description = "Ad-hoc rules location (local directory or git URL) to validate against, " +
                      "bypassing any .rulesengine/config.yml or `rules-engine setup` configuration. Not persisted."
    };

    public static Option<string?> CreateBranchOption() => new("--branch")
    {
        Description = "Git branch to use with --rules-source (default: the repo's default branch). Ignored for a local directory source."
    };
}
