using CodeGuard.Configuration.GlobalConfig;
using CodeGuard.Reporting.Provenance;

namespace CodeGuard.Cli.Support;

/// <summary>Runs `git` for provenance. Returns null on any failure so provenance can never fail a validation.</summary>
public interface IGitCommandRunner
{
    /// <returns>Trimmed stdout, or null when git is missing, the directory is not a repository, or the command fails.</returns>
    string? TryRun(string workingDirectory, params string[] args);
}

/// <summary>Shells out to the system git via the same process helper <see cref="GitRuleSourceSync"/> uses.</summary>
public sealed class ProcessGitCommandRunner : IGitCommandRunner
{
    public string? TryRun(string workingDirectory, params string[] args)
    {
        try
        {
            var (exitCode, stdout, _) = GitRuleSourceSync.RunCore(args, workingDirectory);
            return exitCode == 0 ? stdout.Trim() : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Covers git not being installed (Win32Exception) and a missing working directory.
            return null;
        }
    }
}

/// <summary>
/// Builds the <see cref="ValidationProvenance"/> for a run. Each field comes from the CI environment
/// when a known CI variable is set, and otherwise from git in the repository root, field by field.
/// Environment and git access are injected so the precedence rules can be unit-tested.
/// </summary>
public sealed class ValidationProvenanceGatherer
{
    private readonly IGitCommandRunner _git;
    private readonly Func<string, string?> _environment;

    public ValidationProvenanceGatherer(IGitCommandRunner git, Func<string, string?> environment)
    {
        _git = git;
        _environment = environment;
    }

    public static ValidationProvenanceGatherer Default { get; } =
        new(new ProcessGitCommandRunner(), name => Environment.GetEnvironmentVariable(name));

    public ValidationProvenance Gather(string repoRoot, IReadOnlyList<RuleSourceReference> ruleSources)
    {
        var repository = RepositoryUrlNormalizer.Normalize(FirstNonEmpty(
            GitHubRepositoryUrl(), Env("CI_PROJECT_URL"), Env("BUILD_REPOSITORY_URI")));
        var commit = FirstNonEmpty(Env("GITHUB_SHA"), Env("CI_COMMIT_SHA"), Env("BUILD_SOURCEVERSION"));
        // GITHUB_HEAD_REF is the source branch of a pull request; GITHUB_REF_NAME would be "N/merge" there.
        var branch = FirstNonEmpty(
            Env("GITHUB_HEAD_REF"), Env("GITHUB_REF_NAME"), Env("CI_COMMIT_REF_NAME"), Env("BUILD_SOURCEBRANCHNAME"));

        repository ??= RepositoryUrlNormalizer.Normalize(_git.TryRun(repoRoot, "config", "--get", "remote.origin.url"));
        commit ??= Nullify(_git.TryRun(repoRoot, "rev-parse", "HEAD"));
        if (branch is null)
        {
            var headName = Nullify(_git.TryRun(repoRoot, "rev-parse", "--abbrev-ref", "HEAD"));
            // "HEAD" means detached; there is no branch to report.
            branch = headName is "HEAD" ? null : headName;
        }

        return new ValidationProvenance(
            Repository: repository,
            Commit: commit,
            Branch: branch,
            RuleSources: ruleSources.Select(ToRuleSetSource).ToList());
    }

    private RuleSetSource ToRuleSetSource(RuleSourceReference source) => source.Kind switch
    {
        // The cache is a clone, so its HEAD pins the exact rules used. The location is the remote URL, not the cache path.
        RuleSourceKind.Git => new RuleSetSource(
            "git",
            RepositoryUrlNormalizer.Normalize(source.Location) ?? source.Location,
            Nullify(_git.TryRun(source.LocalPath, "rev-parse", "HEAD"))),
        _ => new RuleSetSource("directory", source.LocalPath, null)
    };

    private string? GitHubRepositoryUrl()
    {
        var repository = Env("GITHUB_REPOSITORY");
        if (repository is null)
        {
            return null;
        }

        var server = (Env("GITHUB_SERVER_URL") ?? "https://github.com").TrimEnd('/');
        return $"{server}/{repository}";
    }

    private string? Env(string name) => Nullify(_environment(name));

    private static string? FirstNonEmpty(params string?[] values) => values.FirstOrDefault(v => v is not null);

    private static string? Nullify(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
