namespace CodeGuard.Reporting.Provenance;

/// <summary>
/// Where a validation run came from: the analysed repository and the rule sets it was evaluated
/// against. Gathered by the CLI (git and CI environment) and handed to the reporters, which never
/// shell out themselves. Every field is optional; unknown values are null and omitted from output.
/// </summary>
/// <param name="Repository">Normalised remote URL, with any credentials or userinfo removed.</param>
/// <param name="Commit">Commit SHA of the analysed checkout.</param>
/// <param name="Branch">Branch name of the analysed checkout (null when detached and not known from CI).</param>
/// <param name="RuleSources">Rule sets used, one per configured rules location.</param>
public sealed record ValidationProvenance(
    string? Repository,
    string? Commit,
    string? Branch,
    IReadOnlyList<RuleSetSource> RuleSources);

/// <param name="Kind">"git" or "directory".</param>
/// <param name="Location">Normalised git URL (credentials removed) or local directory path.</param>
/// <param name="Commit">HEAD of the rules cache for git sources; null for directories.</param>
public sealed record RuleSetSource(string Kind, string Location, string? Commit);
