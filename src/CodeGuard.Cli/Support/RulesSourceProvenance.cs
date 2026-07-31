namespace CodeGuard.Cli.Support;

/// <summary>Which tier of <see cref="CliRepositoryContext.Resolve"/>'s precedence chain produced the rules paths.</summary>
public enum RulesSourceProvenance
{
    CliOverride,
    GlobalSettings,
    RepositoryConfig,
    Default
}
