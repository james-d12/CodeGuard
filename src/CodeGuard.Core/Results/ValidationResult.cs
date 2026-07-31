using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Core.Results;

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
    Severity Severity,
    string Message,
    string? File,
    int? Line,
    int? Column,
    string? Symbol,
    string? Project,
    string? Remediation,
    IReadOnlyList<string> DocumentationReferences);
