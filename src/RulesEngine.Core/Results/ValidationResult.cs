using RulesEngine.RuleModel.Rules;

namespace RulesEngine.Core.Results;

public sealed record ValidationResult(
    ValidationStatus Status,
    int RulesEvaluated,
    int RulesPassed,
    int RulesFailed,
    IReadOnlyList<Violation> Violations,
    DateTimeOffset EvaluatedAtUtc);

public enum ValidationStatus
{
    Passed,
    Failed
}

public sealed record Violation(
    string RuleId,
    string? StandardId,
    Severity Severity,
    string Message,
    string? File,
    int? Line,
    int? Column,
    string? Symbol,
    string? Project,
    string? Remediation,
    IReadOnlyList<string> DocumentationReferences);
