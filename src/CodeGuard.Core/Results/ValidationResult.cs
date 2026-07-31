using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Core.Results;

public sealed record ValidationResult(
    ValidationStatus Status,
    int RulesEvaluated,
    int RulesPassed,
    int RulesFailed,
    int RulesErrored,
    IReadOnlyList<Violation> Violations,
    IReadOnlyList<RuleEvaluationError> EvaluationErrors,
    DateTimeOffset EvaluatedAtUtc);

public enum ValidationStatus
{
    Passed,
    Failed,
    PartiallyEvaluated
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

/// <summary>A rule whose selector/assertion/analyzer threw instead of producing a pass/fail result.</summary>
public sealed record RuleEvaluationError(
    string RuleId,
    string ExceptionType,
    string Message,
    string? StackTrace);
