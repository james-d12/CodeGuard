using CodeGuard.RuleModel.Analyzers;
using CodeGuard.RuleModel.Assertions;
using CodeGuard.RuleModel.Conditions;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.RuleModel.Rules;

public sealed class RuleDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int Version { get; init; } = 1;
    public Severity Severity { get; init; } = Severity.Warning;
    public EnforcementMetadata Enforcement { get; init; } = new();
    public IReadOnlyList<string> Tags { get; init; } = [];
    public string? Remediation { get; init; }
    public IReadOnlyList<string> Documentation { get; init; } = [];
    public bool Enabled { get; init; } = true;
    public bool Illustrative { get; init; }
    public RuleMetadata? Metadata { get; init; }
    public IReadOnlyList<RuleTestCase> Tests { get; init; } = [];

    public ITargetSelector? Target { get; init; }
    public IConditionNode? When { get; init; }
    public IReadOnlyList<IAssertion>? Assertions { get; init; }
    public ICustomAnalyzer? Analyzer { get; init; }
}

public enum Severity
{
    Info,
    Warning,
    Error,
    Critical
}

public enum EnforcementClassification
{
    Deterministic,
    PartiallyDeterministic,
    AiReview,
    HumanReview,
    NotCurrentlyEnforceable
}

public sealed class EnforcementMetadata
{
    public EnforcementClassification Classification { get; init; } = EnforcementClassification.Deterministic;
}

/// <summary>
/// Optional traceability back to the organisational documentation a rule was derived from - see
/// docs/HIGH_LEVEL_AI_ASSISTING.md §6/§19 and docs/done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md.
/// <see cref="RuleSource"/> supports two independent modes: free-text-only (just
/// <see cref="RuleSource.Document"/>/<see cref="RuleSource.Section"/>/<see cref="RuleSource.Statement"/>,
/// never resolved against a real file or used for grouping/lookup by the engine - a prior field with
/// the same intent (`RuleDefinition.Standard`, see docs/IMPLEMENTATION_STATUS.md) was removed after
/// two incompatible authoring conventions collided, and the fix here is to give the field no
/// convention to violate in the first place), and free-text-plus-checkable-link (also setting
/// <see cref="RuleSource.File"/>, which opts the rule into `codeguard rules validate` resolving and
/// fingerprinting the linked content to warn on drift). A rule using only the first three fields is
/// entirely unaffected by the second mode.
/// </summary>
public sealed class RuleMetadata
{
    public RuleSource? Source { get; init; }

    /// <summary>
    /// Opts a rule into version-drift checking (docs/RULE_VERSIONING_PLAN.md): `codeguard rules
    /// validate` recomputes a fingerprint of the rule's own enforceable body (`target`+`assertions`+
    /// `when`, or `analyzer`) and fails - not just warns, unlike <see cref="RuleSource.Fingerprint"/> -
    /// if <see cref="VersionFingerprint"/> is missing or no longer matches, prompting a human to
    /// bump <see cref="RuleDefinition.Version"/> and re-run `rules validate --update-fingerprints`
    /// to capture the new one. A rule that leaves this false is entirely unaffected, at zero cost.
    /// </summary>
    public bool TrackVersion { get; init; }

    /// <summary>
    /// `sha256:&lt;64 hex&gt;` of the rule's own canonicalized enforceable body, captured via
    /// `rules validate --update-fingerprints`. Only meaningful when <see cref="TrackVersion"/> is
    /// true; null until first captured.
    /// </summary>
    public string? VersionFingerprint { get; init; }
}

/// <param name="Document">Free text naming the source document, e.g. "Architecture Standards".</param>
/// <param name="Section">
/// Free text naming a section/heading within that document, if any. When <see cref="File"/> is also
/// set, this doubles as the exact-match heading `rules validate` looks up within that file to scope
/// the fingerprint - see docs/done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md. When <see cref="File"/> is
/// absent, this remains pure display text, never resolved.
/// </param>
/// <param name="Statement">
/// A paraphrase (not a verbatim quote) of the source requirement, if any. Display-only, even when
/// <see cref="File"/> is set - never used to search the linked document verbatim.
/// </param>
/// <param name="File">
/// Repo-relative path to the markdown document this rule was derived from, e.g.
/// "docs/architecture.md". Optional; setting it opts the rule into `codeguard rules validate`
/// resolving and fingerprinting the linked content (scoped to <see cref="Section"/> if set,
/// otherwise the whole file) and warning if it no longer matches <see cref="Fingerprint"/>.
/// </param>
/// <param name="Fingerprint">
/// `sha256:&lt;64 hex&gt;` of the resolved, normalized content, captured by hand or via
/// `rules validate --update-fingerprints`. Only meaningful when <see cref="File"/> is set.
/// </param>
public sealed record RuleSource(
    string Document, string? Section, string? Statement,
    string? File = null, string? Fingerprint = null);
