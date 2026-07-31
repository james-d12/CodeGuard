using CodeGuard.Configuration.GlobalConfig;

namespace CodeGuard.Cli.Support;

/// <summary>
/// Shared between <c>SetupCommand</c> and the ad-hoc `--rules-source` override in
/// <see cref="CliRepositoryContext"/>: detects whether a location is a git URL or a local
/// directory, and resolves either to a local directory path usable as a `rules:` entry. Git
/// sources are cloned into the shared, content-addressed cache
/// (<see cref="GlobalSettingsPaths.RulesCacheDirectory"/>) on first use and reused thereafter -
/// this never auto-fetches on read, so repeated resolutions of the same URL are deterministic and
/// offline-capable (see docs/SETUP_COMMAND_PLAN.md).
/// </summary>
public static class RuleSourceResolver
{
    public static RuleSourceKind DetectKind(string location) =>
        LooksLikeGitUrl(location) ? RuleSourceKind.Git : RuleSourceKind.Directory;

    public static string ResolveToLocalPath(string location, string? branch, RuleSourceKind? kindOverride = null, string? cacheRootOverride = null)
    {
        var kind = kindOverride ?? DetectKind(location);

        if (kind == RuleSourceKind.Directory)
        {
            if (!Directory.Exists(location))
            {
                throw new DirectoryNotFoundException($"Rules directory '{location}' was not found.");
            }

            return Path.GetFullPath(location);
        }

        var cacheDir = GlobalSettingsPaths.RulesCacheDirectory(cacheRootOverride ?? GlobalSettingsPaths.ResolveRoot(), location);
        if (!Directory.Exists(cacheDir) || !Directory.EnumerateFileSystemEntries(cacheDir).Any())
        {
            GitRuleSourceSync.Clone(location, branch, cacheDir);
        }

        return cacheDir;
    }

    private static bool LooksLikeGitUrl(string location) =>
        location.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        location.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
        location.StartsWith("git@", StringComparison.OrdinalIgnoreCase) ||
        location.StartsWith("ssh://", StringComparison.OrdinalIgnoreCase) ||
        location.EndsWith(".git", StringComparison.OrdinalIgnoreCase);
}
