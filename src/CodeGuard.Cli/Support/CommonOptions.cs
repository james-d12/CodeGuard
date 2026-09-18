using System.CommandLine;

namespace CodeGuard.Cli.Support;

/// <summary>--path/--config are shared by every command that resolves a CliRepositoryContext.</summary>
public static class CommonOptions
{
    public static Option<string?> CreatePathOption() => new("--path")
    {
        Description = "Repository root to operate against (default: current directory)."
    };

    public static Option<string?> CreateConfigOption() => new("--config")
    {
        Description = "Explicit .codeguard config.yml path (default: <path>/.codeguard/config.yml if present)."
    };

    public static Option<string?> CreateRulesSourceOption() => new("--rules-source")
    {
        Description = "Ad-hoc rules location (local directory or git URL) to validate against, " +
                      "bypassing any .codeguard/config.yml or `codeguard setup` configuration. Not persisted."
    };

    public static Option<string?> CreateBranchOption(
        string description = "Git branch to use with --rules-source (default: the repo's default branch). Ignored for a local directory source.")
        => new("--branch") { Description = description };

    /// <summary>Shared `--format` construction: description, allowed values and default are supplied
    /// per-command since they genuinely differ (e.g. `validate` allows sarif/html, `discover` allows
    /// markdown), but the Option/AcceptOnlyFromAmong/DefaultValueFactory boilerplate doesn't.</summary>
    public static Option<string> CreateFormatOption(string description, string defaultValue, params string[] allowedValues)
    {
        var option = new Option<string>("--format")
        {
            Description = description,
            DefaultValueFactory = _ => defaultValue
        };
        option.AcceptOnlyFromAmong(allowedValues);
        return option;
    }

    /// <summary>Shared `--color`/`--no-color` pair. <paramref name="colorExtraNote"/> appends a
    /// command-specific caveat to `--color`'s description (e.g. `validate`'s "Ignored when --output
    /// is set.", which doesn't apply to commands that never write to a file).</summary>
    public static (Option<bool> Color, Option<bool> NoColor) CreateColorOptions(string? colorExtraNote = null)
    {
        var colorDescription = "Force ANSI color in console output, even when redirected.";
        if (colorExtraNote is not null)
        {
            colorDescription += " " + colorExtraNote;
        }

        var color = new Option<bool>("--color") { Description = colorDescription };
        var noColor = new Option<bool>("--no-color")
        {
            Description = "Disable ANSI color in console output, even in an interactive terminal."
        };
        return (color, noColor);
    }

    public static Option<string> CreateVerbosityOption()
    {
        var option = new Option<string>("--verbosity")
        {
            Description = "Minimum log level written to stderr: debug, information, warning, error, or " +
                "critical (case-insensitive). Default: information.",
            DefaultValueFactory = _ => "information"
        };
        option.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<string>() ?? "information";
            try
            {
                CliLoggerFactory.ParseVerbosity(value);
            }
            catch (FormatException ex)
            {
                result.AddError(ex.Message);
            }
        });
        return option;
    }
}
