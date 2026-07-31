using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace CodeGuard.Configuration.GlobalConfig;

/// <summary>
/// Resolves the OS-appropriate user/app-data root for RuleEngine's global (cross-repo) config.
/// Deliberately does not use <see cref="Environment.SpecialFolder.ApplicationData"/> - .NET's Unix
/// implementation follows XDG (`~/.config`) for both Linux *and* macOS, which isn't macOS's native
/// `~/Library/Application Support` convention. The resolution logic is a pure function of
/// platform/env/home-dir so the Windows and macOS branches stay unit-testable from Linux CI
/// (this repo's CI only runs on ubuntu-latest).
/// </summary>
public static class GlobalSettingsPaths
{
    private const string AppFolderName = "CodeGuard";
    private const string LinuxAppFolderName = "codeguard";

    public static string ResolveRoot() =>
        ResolveRoot(CurrentPlatform(), Environment.GetEnvironmentVariable, HomeDirectory());

    public static string ResolveRoot(OSPlatform platform, Func<string, string?> getEnvironmentVariable, string homeDirectory)
    {
        if (platform == OSPlatform.Windows)
        {
            var appData = getEnvironmentVariable("APPDATA");
            var root = string.IsNullOrEmpty(appData) ? Path.Combine(homeDirectory, "AppData", "Roaming") : appData;
            return Path.Combine(root, AppFolderName);
        }

        if (platform == OSPlatform.OSX)
        {
            return Path.Combine(homeDirectory, "Library", "Application Support", AppFolderName);
        }

        // Linux (and any other Unix-like platform): XDG Base Directory spec.
        var xdgConfigHome = getEnvironmentVariable("XDG_CONFIG_HOME");
        var configRoot = string.IsNullOrEmpty(xdgConfigHome) ? Path.Combine(homeDirectory, ".config") : xdgConfigHome;
        return Path.Combine(configRoot, LinuxAppFolderName);
    }

    public static string SettingsFilePath(string root) => Path.Combine(root, "settings.yml");

    /// <summary>
    /// Content-addressed cache directory for a git rule source, derived from the URL rather than
    /// persisted - `setup` and the ad-hoc `--rules-source` flag both land on the same cache for the
    /// same URL, and nothing here can go stale.
    /// </summary>
    public static string RulesCacheDirectory(string root, string gitUrl) =>
        Path.Combine(root, "rules-cache", SanitizeForDirectoryName(gitUrl));

    private static string SanitizeForDirectoryName(string url)
    {
        var repoName = url.TrimEnd('/', '\\');
        var lastSeparator = repoName.LastIndexOfAny(['/', '\\']);
        if (lastSeparator >= 0)
        {
            repoName = repoName[(lastSeparator + 1)..];
        }

        if (repoName.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            repoName = repoName[..^".git".Length];
        }

        var sanitizedChars = repoName.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '-').ToArray();
        var sanitized = new string(sanitizedChars).Trim('-');
        if (sanitized.Length == 0)
        {
            sanitized = "rules";
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(url)))[..8];
        return $"{sanitized}-{hash}";
    }

    private static OSPlatform CurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return OSPlatform.Windows;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return OSPlatform.OSX;
        }

        return OSPlatform.Linux;
    }

    private static string HomeDirectory() =>
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
}
