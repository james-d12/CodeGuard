using CodeGuard.Configuration.GlobalConfig;

namespace CodeGuard.Cli.Support;

/// <summary>
/// One configured rules location as the CLI resolved it: the source as the user wrote it (a git URL
/// or directory), and the local directory the rules were actually loaded from (the git cache for git).
/// </summary>
public sealed record RuleSourceReference(RuleSourceKind Kind, string Location, string LocalPath);

/// <summary>Which tier of <see cref="CliRepositoryContext.Resolve"/>'s precedence chain produced the rules paths.</summary>
public enum RulesSourceProvenance
{
    CliOverride,
    GlobalSettings,
    RepositoryConfig,
    Default
}
