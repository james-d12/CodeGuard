namespace CodeGuard.Reporting.Contracts;

/// <summary>
/// Versioned, public JSON result contract for <c>codeguard validate --format json</c>. Deliberately
/// independent of the internal <see cref="CodeGuard.Core.Results.ValidationResult"/> model, so internal
/// refactors do not change the output. Schema: <c>Schemas/validation-report.v1.schema.json</c>.
/// </summary>
public sealed record ValidationReportV1(
    string SchemaVersion,
    string ToolVersion,
    string Status,
    DateTimeOffset EvaluatedAtUtc,
    long DurationMs,
    ValidationReportMetricsV1 Metrics,
    IReadOnlyList<ViolationV1> Violations,
    IReadOnlyList<EvaluationErrorV1> EvaluationErrors,
    IReadOnlyList<AnalysisWarningV1> AnalysisWarnings,
    string? Repository = null,
    string? Commit = null,
    string? Branch = null,
    IReadOnlyList<RuleSetSourceV1>? RuleSources = null);

public sealed record ValidationReportMetricsV1(
    int RulesEvaluated,
    int RulesPassed,
    int RulesFailed,
    int RulesErrored,
    int ViolationCount,
    SeverityCountsV1 ViolationsBySeverity,
    int EvaluationErrorCount,
    int AnalysisWarningCount);

public sealed record SeverityCountsV1(int Info, int Warning, int Error, int Critical);

public sealed record ViolationV1(
    string RuleId,
    int RuleVersion,
    string Severity,
    string Message,
    string? File,
    int? Line,
    int? Column,
    string? Symbol,
    string? Project,
    string? Remediation,
    IReadOnlyList<string> DocumentationReferences);

public sealed record EvaluationErrorV1(
    string RuleId,
    string ExceptionType,
    string Message,
    string? StackTrace);

public sealed record AnalysisWarningV1(
    string Code,
    string Message,
    string? Project,
    string? FilePath);

public sealed record RuleSetSourceV1(string Kind, string Location, string? Commit);
