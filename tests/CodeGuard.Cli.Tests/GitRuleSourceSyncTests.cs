using System.Diagnostics;
using CodeGuard.Cli.Support;

namespace CodeGuard.Cli.Tests;

/// <summary>
/// Exercises GitRuleSourceSync against a throwaway local repo used as the "remote" - no network
/// access needed, `git clone`/`fetch` work fine against a local filesystem path.
/// </summary>
public class GitRuleSourceSyncTests : IDisposable
{
    private const string Branch = "main";

    private readonly string _remoteDir = Directory.CreateTempSubdirectory("rulesengine-gitsync-remote-").FullName;
    private readonly string _destinationDir = Path.Combine(Directory.CreateTempSubdirectory("rulesengine-gitsync-dest-").FullName, "clone");

    public GitRuleSourceSyncTests()
    {
        RunGit(_remoteDir, "init", "-b", Branch);
        RunGit(_remoteDir, "config", "user.email", "test@example.com");
        RunGit(_remoteDir, "config", "user.name", "Test");
        CommitFile("first.txt", "one");
    }

    [Fact]
    public void SyncOrClone_Clones_WhenDestinationDoesNotExistYet()
    {
        var result = GitRuleSourceSync.SyncOrClone(_remoteDir, Branch, _destinationDir);

        Assert.Equal(GitSyncResult.Cloned, result);
        Assert.True(File.Exists(Path.Combine(_destinationDir, "first.txt")));
    }

    [Fact]
    public void SyncOrClone_ReportsAlreadyUpToDate_WhenNoNewCommits()
    {
        GitRuleSourceSync.SyncOrClone(_remoteDir, Branch, _destinationDir);

        var result = GitRuleSourceSync.SyncOrClone(_remoteDir, Branch, _destinationDir);

        Assert.Equal(GitSyncResult.AlreadyUpToDate, result);
    }

    [Fact]
    public void SyncOrClone_FastForwards_WhenRemoteHasNewCommits()
    {
        GitRuleSourceSync.SyncOrClone(_remoteDir, Branch, _destinationDir);
        CommitFile("second.txt", "two");

        var result = GitRuleSourceSync.SyncOrClone(_remoteDir, Branch, _destinationDir);

        Assert.Equal(GitSyncResult.FastForwarded, result);
        Assert.True(File.Exists(Path.Combine(_destinationDir, "second.txt")));
    }

    [Fact]
    public void SyncOrClone_ReportsBlocked_AndLeavesCacheUntouched_WhenLocalCacheHasUncommittedChanges()
    {
        GitRuleSourceSync.SyncOrClone(_remoteDir, Branch, _destinationDir);
        File.WriteAllText(Path.Combine(_destinationDir, "first.txt"), "locally modified");

        var result = GitRuleSourceSync.SyncOrClone(_remoteDir, Branch, _destinationDir);

        Assert.Equal(GitSyncResult.Blocked, result);
        Assert.Equal("locally modified", File.ReadAllText(Path.Combine(_destinationDir, "first.txt")));
    }

    [Fact]
    public void SyncOrClone_ReportsBlocked_WhenLocalCacheHasDivergedFromRemote()
    {
        GitRuleSourceSync.SyncOrClone(_remoteDir, Branch, _destinationDir);
        RunGit(_destinationDir, "config", "user.email", "test@example.com");
        RunGit(_destinationDir, "config", "user.name", "Test");
        RunGit(_destinationDir, "commit", "--allow-empty", "-m", "local-only commit");
        CommitFile("second.txt", "two"); // remote moves forward too, so histories diverge

        var result = GitRuleSourceSync.SyncOrClone(_remoteDir, Branch, _destinationDir);

        Assert.Equal(GitSyncResult.Blocked, result);
    }

    private void CommitFile(string name, string contents)
    {
        File.WriteAllText(Path.Combine(_remoteDir, name), contents);
        RunGit(_remoteDir, "add", ".");
        RunGit(_remoteDir, "commit", "-m", $"add {name}");
    }

    private static void RunGit(string workingDirectory, params string[] args)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo)!;
        process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {string.Join(' ', args)} failed: {stderr}");
        }
    }

    public void Dispose()
    {
        Directory.Delete(_remoteDir, recursive: true);
        var destinationParent = Path.GetDirectoryName(_destinationDir)!;
        if (Directory.Exists(destinationParent))
        {
            Directory.Delete(destinationParent, recursive: true);
        }
    }
}
