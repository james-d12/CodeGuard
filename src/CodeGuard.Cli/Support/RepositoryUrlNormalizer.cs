using System.Text.RegularExpressions;

namespace CodeGuard.Cli.Support;

/// <summary>
/// Turns a git remote (URL or scp-style <c>user@host:path</c>) into a stable identity that is safe to
/// write into a report: userinfo (<c>user:token@</c>) is always removed, query/fragment dropped, a
/// trailing <c>/</c> and <c>.git</c> stripped, and scp-style remotes rewritten as <c>ssh://host/path</c>.
/// Credentials must never reach a report, so this runs on every remote before it is stored.
/// </summary>
public static partial class RepositoryUrlNormalizer
{
    // Authority ends at the last '@' before the first '/', so a password containing '@' is still removed.
    [GeneratedRegex(@"^(?<scheme>[a-zA-Z][a-zA-Z0-9+.\-]*://)[^/]*@")]
    private static partial Regex UserInfo();

    // Requires a host of 2+ characters so Windows drive letters (C:\repo) are not mistaken for hosts.
    [GeneratedRegex(@"^(?:[^@/\s]+@)?(?<host>[^:/\s]{2,}):(?<path>.+)$")]
    private static partial Regex ScpLike();

    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var value = raw.Trim();

        if (value.Contains("://", StringComparison.Ordinal))
        {
            value = UserInfo().Replace(value, "${scheme}");
            var cut = value.IndexOfAny(['?', '#']);
            if (cut >= 0)
            {
                value = value[..cut];
            }
        }
        else
        {
            var match = ScpLike().Match(value);
            if (match.Success)
            {
                value = $"ssh://{match.Groups["host"].Value}/{match.Groups["path"].Value.TrimStart('/')}";
            }
        }

        value = value.TrimEnd('/');
        if (value.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^4].TrimEnd('/');
        }

        return value.Length == 0 ? null : value;
    }
}
