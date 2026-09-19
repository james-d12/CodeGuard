using CodeGuard.Configuration.Analysis;
using CodeGuard.Configuration.Loading;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Configuration.Versioning;

public enum RuleVersionIssueKind
{
    FingerprintMissing,
    ContentChanged
}

/// <summary>
/// One rule whose recorded <see cref="RuleDefinition.VersionFingerprint"/> doesn't reflect its
/// current enforceable body - either because it was never captured
/// (<see cref="RuleVersionIssueKind.FingerprintMissing"/>) or because the body changed since it was
/// (<see cref="RuleVersionIssueKind.ContentChanged"/>). <see cref="RecordedFingerprint"/> is null for
/// the former. Both kinds are acted on by `rules validate --update-fingerprints` and both fail
/// `rules validate`'s exit code, for every rule unconditionally - see docs/RULE_VERSIONING_PLAN.md
/// for why this is both mandatory (unlike the analogous, opt-in <c>RuleSourceChecker</c> drift check)
/// and stricter (a hard failure, not just a warning).
/// </summary>
public sealed record RuleVersionIssue(
    string RuleId, string SourceFile, RuleVersionIssueKind Kind, string? RecordedFingerprint, string ComputedFingerprint);

public sealed record RuleVersionCheckReport(IReadOnlyList<RuleVersionIssue> Issues)
{
    public bool IsValid => Issues.Count == 0;
}

/// <summary>
/// Recomputes every rule's enforceable-body fingerprint and compares it against
/// <see cref="RuleDefinition.VersionFingerprint"/> - unconditionally, for every rule, with no opt-in
/// gate (see docs/RULE_VERSIONING_PLAN.md for why: an earlier opt-in design was deliberately replaced
/// with this one). A rule that has never been through `rules validate --update-fingerprints` simply
/// reports <see cref="RuleVersionIssueKind.FingerprintMissing"/>. Unlike <c>RuleSourceChecker</c>,
/// this does no file I/O outside the rules directory (the "source" being checked is the rule's own
/// body, already on disk).
/// </summary>
public static class RuleVersionChecker
{
    public static RuleVersionCheckReport Check(IReadOnlyList<(RuleDefinition Rule, string SourceFile)> rules)
    {
        var issues = new List<RuleVersionIssue>();

        foreach (var (rule, sourceFile) in rules)
        {
            var document = RuleFileLoader.ReadDocument(sourceFile).AsObject();
            var computed = RuleBodyCanonicalizer.ComputeFingerprint(document);

            if (rule.VersionFingerprint is not { } recorded)
            {
                issues.Add(new RuleVersionIssue(rule.Id, sourceFile, RuleVersionIssueKind.FingerprintMissing, null, computed));
            }
            else if (!string.Equals(recorded, computed, StringComparison.Ordinal))
            {
                issues.Add(new RuleVersionIssue(rule.Id, sourceFile, RuleVersionIssueKind.ContentChanged, recorded, computed));
            }
        }

        return new RuleVersionCheckReport(issues);
    }
}
