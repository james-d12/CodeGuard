using CodeGuard.Cli.Support;
using CodeGuard.Configuration.GlobalConfig;

namespace CodeGuard.Cli.Tests;

public class ValidationProvenanceGathererTests
{
    private const string RepoRoot = "/work/repo";
    private const string RulesCache = "/cache/rules";

    private static readonly string[] OriginUrl = ["config", "--get", "remote.origin.url"];
    private static readonly string[] HeadSha = ["rev-parse", "HEAD"];
    private static readonly string[] HeadBranch = ["rev-parse", "--abbrev-ref", "HEAD"];

    [Fact]
    public void Gather_OutsideGit_ReturnsNullsAndDirectorySource()
    {
        var gatherer = new ValidationProvenanceGatherer(new FakeGit(), _ => null);

        var result = gatherer.Gather(RepoRoot, [new RuleSourceReference(RuleSourceKind.Directory, "/r/rules", "/r/rules")]);

        Assert.Null(result.Repository);
        Assert.Null(result.Commit);
        Assert.Null(result.Branch);
        var source = Assert.Single(result.RuleSources);
        Assert.Equal("directory", source.Kind);
        Assert.Equal("/r/rules", source.Location);
        Assert.Null(source.Commit);
    }

    [Fact]
    public void Gather_FromGit_ReadsRemoteCommitAndBranch()
    {
        var git = new FakeGit
        {
            [(RepoRoot, OriginUrl)] = "git@github.com:acme/app.git\n",
            [(RepoRoot, HeadSha)] = "abc123\n",
            [(RepoRoot, HeadBranch)] = "feature/x\n"
        };

        var result = new ValidationProvenanceGatherer(git, _ => null).Gather(RepoRoot, []);

        Assert.Equal("ssh://github.com/acme/app", result.Repository);
        Assert.Equal("abc123", result.Commit);
        Assert.Equal("feature/x", result.Branch);
    }

    [Fact]
    public void Gather_DetachedHeadWithoutCi_ReportsNoBranch()
    {
        var git = new FakeGit { [(RepoRoot, HeadBranch)] = "HEAD\n" };

        var result = new ValidationProvenanceGatherer(git, _ => null).Gather(RepoRoot, []);

        Assert.Null(result.Branch);
    }

    [Fact]
    public void Gather_GitHubActions_EnvironmentOverridesGit()
    {
        var git = new FakeGit
        {
            [(RepoRoot, OriginUrl)] = "https://git.local/old.git",
            [(RepoRoot, HeadSha)] = "localsha",
            [(RepoRoot, HeadBranch)] = "HEAD"
        };
        var env = Env(
            ("GITHUB_REPOSITORY", "acme/app"),
            ("GITHUB_SERVER_URL", "https://github.com/"),
            ("GITHUB_SHA", "ci-sha"),
            ("GITHUB_REF_NAME", "42/merge"),
            ("GITHUB_HEAD_REF", "fix/thing"));

        var result = new ValidationProvenanceGatherer(git, env).Gather(RepoRoot, []);

        Assert.Equal("https://github.com/acme/app", result.Repository);
        Assert.Equal("ci-sha", result.Commit);
        Assert.Equal("fix/thing", result.Branch);
    }

    [Fact]
    public void Gather_GitHubActions_FallsBackToRefNameWithoutHeadRef()
    {
        var env = Env(("GITHUB_REPOSITORY", "acme/app"), ("GITHUB_REF_NAME", "main"));

        var result = new ValidationProvenanceGatherer(new FakeGit(), env).Gather(RepoRoot, []);

        Assert.Equal("https://github.com/acme/app", result.Repository);
        Assert.Equal("main", result.Branch);
    }

    [Fact]
    public void Gather_GitLabCi_UsesProjectUrlCommitAndRefName()
    {
        var env = Env(
            ("CI_PROJECT_URL", "https://gitlab.example.com/group/project"),
            ("CI_COMMIT_SHA", "gl-sha"),
            ("CI_COMMIT_REF_NAME", "release"));

        var result = new ValidationProvenanceGatherer(new FakeGit(), env).Gather(RepoRoot, []);

        Assert.Equal("https://gitlab.example.com/group/project", result.Repository);
        Assert.Equal("gl-sha", result.Commit);
        Assert.Equal("release", result.Branch);
    }

    [Fact]
    public void Gather_AzurePipelines_UsesBuildVariables()
    {
        var env = Env(
            ("BUILD_REPOSITORY_URI", "https://dev.azure.com/org/project/_git/repo"),
            ("BUILD_SOURCEVERSION", "az-sha"),
            ("BUILD_SOURCEBRANCHNAME", "develop"));

        var result = new ValidationProvenanceGatherer(new FakeGit(), env).Gather(RepoRoot, []);

        Assert.Equal("https://dev.azure.com/org/project/_git/repo", result.Repository);
        Assert.Equal("az-sha", result.Commit);
        Assert.Equal("develop", result.Branch);
    }

    [Fact]
    public void Gather_CiUrlWithCredentials_IsStripped()
    {
        var env = Env(("CI_PROJECT_URL", "https://oauth2:glpat-secret@gitlab.example.com/group/project.git"));

        var result = new ValidationProvenanceGatherer(new FakeGit(), env).Gather(RepoRoot, []);

        Assert.Equal("https://gitlab.example.com/group/project", result.Repository);
        Assert.DoesNotContain("secret", result.Repository, StringComparison.Ordinal);
    }

    [Fact]
    public void Gather_GitRemoteWithCredentials_IsStripped()
    {
        var git = new FakeGit { [(RepoRoot, OriginUrl)] = "https://user:tok@dev.example.com/org/repo.git" };

        var result = new ValidationProvenanceGatherer(git, _ => null).Gather(RepoRoot, []);

        Assert.Equal("https://dev.example.com/org/repo", result.Repository);
    }

    [Fact]
    public void Gather_GitRuleSource_ReportsRemoteAndCacheCommit()
    {
        var git = new FakeGit { [(RulesCache, HeadSha)] = "rules-sha" };
        var source = new RuleSourceReference(RuleSourceKind.Git, "https://u:p@rules.example.com/acme/rules.git", RulesCache);

        var result = new ValidationProvenanceGatherer(git, _ => null).Gather(RepoRoot, [source]);

        var rule = Assert.Single(result.RuleSources);
        Assert.Equal("git", rule.Kind);
        Assert.Equal("https://rules.example.com/acme/rules", rule.Location);
        Assert.Equal("rules-sha", rule.Commit);
    }

    [Fact]
    public void Gather_GitRuleSourceWithoutGit_KeepsLocationWithNullCommit()
    {
        var source = new RuleSourceReference(RuleSourceKind.Git, "https://rules.example.com/acme/rules.git", RulesCache);

        var result = new ValidationProvenanceGatherer(new FakeGit(), _ => null).Gather(RepoRoot, [source]);

        var rule = Assert.Single(result.RuleSources);
        Assert.Equal("https://rules.example.com/acme/rules", rule.Location);
        Assert.Null(rule.Commit);
    }

    [Fact]
    public void ProcessGitCommandRunner_NonRepositoryDirectory_ReturnsNull()
    {
        var notARepo = Path.Combine(Path.GetTempPath(), "codeguard-provenance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(notARepo);
        try
        {
            Assert.Null(new ProcessGitCommandRunner().TryRun(notARepo, "rev-parse", "HEAD"));
        }
        finally
        {
            Directory.Delete(notARepo, recursive: true);
        }
    }

    [Fact]
    public void ProcessGitCommandRunner_MissingDirectory_ReturnsNull()
    {
        var missing = Path.Combine(Path.GetTempPath(), "codeguard-missing-" + Guid.NewGuid().ToString("N"));

        Assert.Null(new ProcessGitCommandRunner().TryRun(missing, "rev-parse", "HEAD"));
    }

    private static Func<string, string?> Env(params (string Name, string Value)[] values)
    {
        var map = values.ToDictionary(v => v.Name, v => v.Value, StringComparer.Ordinal);
        return name => map.GetValueOrDefault(name);
    }

    /// <summary>Keyed by (working directory, git arguments). Unmapped calls behave as "git fails".</summary>
    private sealed class FakeGit : IGitCommandRunner
    {
        private readonly Dictionary<(string Dir, string Args), string> _responses = new();

        public string? this[(string Dir, string[] Args) key]
        {
            set => _responses[(key.Dir, string.Join(' ', key.Args))] = value!;
        }

        public string? TryRun(string workingDirectory, params string[] args) =>
            _responses.TryGetValue((workingDirectory, string.Join(' ', args)), out var output) ? output : null;
    }
}
