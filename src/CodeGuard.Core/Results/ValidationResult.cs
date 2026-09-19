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
    DateTimeOffset EvaluatedAtUtc,
    IReadOnlyList<AnalysisWarning> AnalysisWarnings = null!) // ??= [] below - a positional record parameter can't use a collection-expression default directly
{
    public IReadOnlyList<AnalysisWarning> AnalysisWarnings { get; init; } = AnalysisWarnings ?? [];
}

public enum ValidationStatus
{
    Passed,
    Failed,
    PartiallyEvaluated
}

public sealed record Violation(
    string RuleId,
    int RuleVersion,
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

/// <summary>
/// A non-rule signal about the completeness of the analysis model itself - e.g. a project that
/// MSBuildWorkspace could not fully load (see CodeGuard.Analyzers.MSBuild's WorkspaceFailedHandler).
/// Unlike a <see cref="RuleEvaluationError"/>, the rule engine ran fine here - but the underlying data
/// it evaluated may be missing information for the affected project, so semantic-dependent rules
/// (dependency/call-site checks) may under- or over-report for it.
/// </summary>
public sealed record AnalysisWarning(
    string Code,
    string Message,
    string? Project,
    string? FilePath);
