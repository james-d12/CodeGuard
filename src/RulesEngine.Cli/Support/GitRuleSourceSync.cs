using System.Diagnostics;

namespace RulesEngine.Cli.Support;

public enum GitSyncResult
{
    Cloned,
    AlreadyUpToDate,
    FastForwarded,
    Blocked
}

/// <summary>
/// Shells out to the system `git` binary rather than a managed git library (e.g. LibGit2Sharp) -
/// this reuses the developer's existing SSH keys/credential helpers for free, and avoids adding
/// another per-platform native-dependency surface on top of the Buildalyzer/MSBuild one already
/// documented in docs/IMPLEMENTATION_STATUS.md. Every operation here is non-destructive: fetch,
/// rev-parse, and `pull --ff-only` only - a diverged or dirty cache is reported as
/// <see cref="GitSyncResult.Blocked"/> rather than force-reset.
/// </summary>
public static class GitRuleSourceSync
{
    public static void Clone(string url, string? branch, string destinationDir)
    {
        var parentDir = Path.GetDirectoryName(Path.GetFullPath(destinationDir));
        if (!string.IsNullOrEmpty(parentDir))
        {
            Directory.CreateDirectory(parentDir);
        }

        var args = branch is null
            ? new List<string> { "clone", url, destinationDir }
            : new List<string> { "clone", "--branch", branch, url, destinationDir };

        Run(args, workingDirectory: null);
    }

    /// <summary>
    /// Clones into <paramref name="destinationDir"/> if it doesn't exist yet (or is empty).
    /// Otherwise fetches and fast-forwards if the cache is cleanly behind the remote; a dirty
    /// working tree or a diverged history is left untouched and reported as Blocked.
    /// </summary>
    public static GitSyncResult SyncOrClone(string url, string? branch, string destinationDir)
    {
        if (!Directory.Exists(destinationDir) || !Directory.EnumerateFileSystemEntries(destinationDir).Any())
        {
            Clone(url, branch, destinationDir);
            return GitSyncResult.Cloned;
        }

        var status = Capture(["status", "--porcelain"], destinationDir);
        if (!string.IsNullOrWhiteSpace(status))
        {
            return GitSyncResult.Blocked;
        }

        var fetchArgs = branch is null ? new[] { "fetch", "origin" } : new[] { "fetch", "origin", branch };
        Run(fetchArgs, destinationDir);

        var upstreamRef = branch is null ? "origin/HEAD" : $"origin/{branch}";
        var localHead = Capture(["rev-parse", "HEAD"], destinationDir).Trim();
        var upstreamHead = Capture(["rev-parse", upstreamRef], destinationDir).Trim();

        if (string.Equals(localHead, upstreamHead, StringComparison.Ordinal))
        {
            return GitSyncResult.AlreadyUpToDate;
        }

        var (isAncestorExitCode, _, _) = RunCore(["merge-base", "--is-ancestor", "HEAD", upstreamRef], destinationDir);
        if (isAncestorExitCode != 0)
        {
            return GitSyncResult.Blocked;
        }

        var pullArgs = branch is null ? new[] { "pull", "--ff-only", "origin" } : new[] { "pull", "--ff-only", "origin", branch };
        Run(pullArgs, destinationDir);
        return GitSyncResult.FastForwarded;
    }

    private static string Capture(IReadOnlyList<string> args, string workingDirectory)
    {
        var (exitCode, stdout, stderr) = RunCore(args, workingDirectory);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"git {string.Join(' ', args)} failed: {stderr.Trim()}");
        }

        return stdout;
    }

    private static void Run(IReadOnlyList<string> args, string? workingDirectory)
    {
        var (exitCode, _, stderr) = RunCore(args, workingDirectory);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"git {string.Join(' ', args)} failed: {stderr.Trim()}");
        }
    }

    private static (int ExitCode, string Stdout, string Stderr) RunCore(IReadOnlyList<string> args, string? workingDirectory)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        if (workingDirectory is not null)
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the git process - is git installed and on PATH?");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, stdout, stderr);
    }
}
