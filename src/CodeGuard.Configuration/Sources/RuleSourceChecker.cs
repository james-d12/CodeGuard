using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Configuration.Sources;

public enum RuleSourceIssueKind
{
    FileMissing,
    SectionNotFound,
    SectionAmbiguous,
    ContentChanged,
    FingerprintMissing
}

/// <summary>
/// One rule's <c>metadata.source.file</c> link not matching what's currently on disk - see
/// docs/done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md. <see cref="RecordedStatement"/>/
/// <see cref="CurrentContent"/>/<see cref="ComputedFingerprint"/> are evidence for a human to review,
/// never a judgement that the rule itself is wrong. Populated per <see cref="Kind"/>:
/// <see cref="CurrentContent"/>/<see cref="ComputedFingerprint"/> only for
/// <see cref="RuleSourceIssueKind.ContentChanged"/>/<see cref="RuleSourceIssueKind.FingerprintMissing"/>
/// (the file/section did resolve, so there's something to fingerprint - these two kinds are also the
/// only ones `rules validate --update-fingerprints` acts on); <see cref="AmbiguousHeadingLines"/> only
/// for <see cref="RuleSourceIssueKind.SectionAmbiguous"/>; <see cref="PreviousFingerprint"/> only for
/// <see cref="RuleSourceIssueKind.ContentChanged"/> (the stored value that no longer matches).
/// </summary>
public sealed record RuleSourceIssue(
    string RuleId,
    string SourceFile,
    RuleSourceIssueKind Kind,
    string File,
    string? Section,
    string? RecordedStatement,
    string? CurrentContent,
    string? ComputedFingerprint,
    IReadOnlyList<int> AmbiguousHeadingLines,
    string? PreviousFingerprint = null);

public sealed record RuleSourceCheckReport(IReadOnlyList<RuleSourceIssue> Issues);

/// <summary>
/// Resolves every rule's <c>metadata.source.file</c> link (if any) against the repository and reports
/// drift - see docs/done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md. Called only from
/// <c>codeguard rules validate</c>; unlike <see cref="CodeGuard.Configuration.Analysis.RuleSetAnalyzer"/>
/// this does real file I/O outside the rules directory, scoped to rules that opt in via
/// <c>source.file</c> - everything else is skipped at zero cost.
/// </summary>
public static class RuleSourceChecker
{
    /// <param name="rules">
    /// Successfully parsed rules only (<c>RuleSetValidationReport.Rules</c>, not <c>.Issues</c>) - a
    /// rule that failed schema validation has no parsed <see cref="RuleDefinition"/> to check.
    /// </param>
    /// <param name="repoRoot">Repository root that <c>source.file</c> paths are resolved against.</param>
    public static RuleSourceCheckReport Check(IReadOnlyList<(RuleDefinition Rule, string SourceFile)> rules, string repoRoot)
    {
        var issues = new List<RuleSourceIssue>();

        foreach (var (rule, sourceFile) in rules)
        {
            if (rule.Metadata?.Source is not { File: { } file } source)
            {
                continue;
            }

            var resolvedPath = Path.GetFullPath(Path.Combine(repoRoot, file));
            if (!File.Exists(resolvedPath))
            {
                issues.Add(new RuleSourceIssue(
                    rule.Id, sourceFile, RuleSourceIssueKind.FileMissing, file, source.Section,
                    source.Statement, null, null, []));
                continue;
            }

            var resolution = MarkdownSourceResolver.Resolve(File.ReadAllText(resolvedPath), source.Section);

            switch (resolution.Kind)
            {
                case MarkdownResolutionKind.SectionNotFound:
                    issues.Add(new RuleSourceIssue(
                        rule.Id, sourceFile, RuleSourceIssueKind.SectionNotFound, file, source.Section,
                        source.Statement, null, null, []));
                    continue;
                case MarkdownResolutionKind.SectionAmbiguous:
                    issues.Add(new RuleSourceIssue(
                        rule.Id, sourceFile, RuleSourceIssueKind.SectionAmbiguous, file, source.Section,
                        source.Statement, null, null, resolution.AmbiguousHeadingLines));
                    continue;
            }

            if (source.Fingerprint is null)
            {
                issues.Add(new RuleSourceIssue(
                    rule.Id, sourceFile, RuleSourceIssueKind.FingerprintMissing, file, source.Section,
                    source.Statement, resolution.Content, resolution.Fingerprint, []));
            }
            else if (!string.Equals(source.Fingerprint, resolution.Fingerprint, StringComparison.Ordinal))
            {
                issues.Add(new RuleSourceIssue(
                    rule.Id, sourceFile, RuleSourceIssueKind.ContentChanged, file, source.Section,
                    source.Statement, resolution.Content, resolution.Fingerprint, [], source.Fingerprint));
            }
        }

        return new RuleSourceCheckReport(issues);
    }
}
