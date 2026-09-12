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
/// docs/HIGH_LEVEL_AI_ASSISTING.md §6/§19. Deliberately minimal: <see cref="RuleSource.Document"/>/
/// <see cref="RuleSource.Section"/> are free text, never resolved against a real file or used for
/// grouping/lookup by the engine - a prior field with the same intent (`RuleDefinition.Standard`,
/// see docs/IMPLEMENTATION_STATUS.md) was removed after two incompatible authoring conventions
/// collided, and the fix here is to give the field no convention to violate in the first place.
/// </summary>
public sealed class RuleMetadata
{
    public RuleSource? Source { get; init; }
}

/// <param name="Document">Free text naming the source document, e.g. "Architecture Standards".</param>
/// <param name="Section">Free text naming a section/heading within that document, if any.</param>
/// <param name="Statement">A paraphrase (not a verbatim quote) of the source requirement, if any.</param>
public sealed record RuleSource(string Document, string? Section, string? Statement);
