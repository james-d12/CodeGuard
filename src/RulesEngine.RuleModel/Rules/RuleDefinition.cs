using RulesEngine.RuleModel.Assertions;
using RulesEngine.RuleModel.Conditions;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.RuleModel.Rules;

public sealed class RuleDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Standard { get; init; }
    public Severity Severity { get; init; } = Severity.Warning;
    public EnforcementMetadata Enforcement { get; init; } = new();
    public IReadOnlyList<string> Tags { get; init; } = [];
    public string? Remediation { get; init; }
    public IReadOnlyList<string> Documentation { get; init; } = [];
    public bool Enabled { get; init; } = true;
    public bool Illustrative { get; init; }

    public required ITargetSelector Target { get; init; }
    public IConditionNode? When { get; init; }
    public required IReadOnlyList<IAssertion> Assertions { get; init; }
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
