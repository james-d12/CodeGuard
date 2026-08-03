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
