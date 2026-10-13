using System.Reflection;
using CodeGuard.Core.Results;
using CodeGuard.Reporting.Provenance;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Reporting.Contracts;

/// <summary>Maps the internal <see cref="ValidationResult"/> to the public <see cref="ValidationReportV1"/> contract.</summary>
public static class ValidationReportMapper
{
    public const string SchemaVersion = "1.0";

    /// <param name="provenance">Repository and rule-source provenance, when the caller gathered it. Null leaves those fields out.</param>
    public static ValidationReportV1 ToV1(
        ValidationResult result, TimeSpan duration, string toolVersion, ValidationProvenance? provenance = null)
    {
        var violations = result.Violations.Select(MapViolation).ToList();

        return new ValidationReportV1(
            SchemaVersion: SchemaVersion,
            ToolVersion: toolVersion,
            Status: MapStatus(result.Status),
            EvaluatedAtUtc: result.EvaluatedAtUtc,
            DurationMs: (long)duration.TotalMilliseconds,
            Metrics: new ValidationReportMetricsV1(
                RulesEvaluated: result.RulesEvaluated,
                RulesPassed: result.RulesPassed,
                RulesFailed: result.RulesFailed,
                RulesErrored: result.RulesErrored,
                ViolationCount: violations.Count,
                ViolationsBySeverity: CountBySeverity(result.Violations),
                EvaluationErrorCount: result.EvaluationErrors.Count,
                AnalysisWarningCount: result.AnalysisWarnings.Count),
            Violations: violations,
            EvaluationErrors: result.EvaluationErrors
                .Select(e => new EvaluationErrorV1(e.RuleId, e.ExceptionType, e.Message, e.StackTrace))
                .ToList(),
            AnalysisWarnings: result.AnalysisWarnings
                .Select(w => new AnalysisWarningV1(w.Code, w.Message, w.Project, w.FilePath))
                .ToList(),
            Repository: provenance?.Repository,
            Commit: provenance?.Commit,
            Branch: provenance?.Branch,
            RuleSources: provenance?.RuleSources
                .Select(r => new RuleSetSourceV1(r.Kind, r.Location, r.Commit))
                .ToList());
    }

    /// <summary>
    /// The assembly's informational version with any source-link "+commit" suffix removed
    /// (e.g. "0.3.1+abc123" becomes "0.3.1"). Falls back to "0.0.0" when no version is embedded.
    /// </summary>
    public static string ResolveToolVersion(Assembly assembly)
    {
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(informational))
        {
            return "0.0.0";
        }

        var plus = informational.IndexOf('+', StringComparison.Ordinal);
        return plus >= 0 ? informational[..plus] : informational;
    }

    // Explicit wire values rather than enum ToString(), so renaming an internal enum member cannot change the contract.
    private static string MapStatus(ValidationStatus status) => status switch
    {
        ValidationStatus.Passed => "passed",
        ValidationStatus.Failed => "failed",
        ValidationStatus.PartiallyEvaluated => "partiallyEvaluated",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown validation status.")
    };

    private static string MapSeverity(Severity severity) => severity switch
    {
        Severity.Info => "info",
        Severity.Warning => "warning",
        Severity.Error => "error",
        Severity.Critical => "critical",
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown severity.")
    };

    private static ViolationV1 MapViolation(Violation v) => new(
        RuleId: v.RuleId,
        RuleVersion: v.RuleVersion,
        Severity: MapSeverity(v.Severity),
        Message: v.Message,
        File: v.File,
        Line: v.Line,
        Column: v.Column,
        Symbol: v.Symbol,
        Project: v.Project,
        Remediation: v.Remediation,
        DocumentationReferences: v.DocumentationReferences);

    private static SeverityCountsV1 CountBySeverity(IReadOnlyList<Violation> violations)
    {
        int Count(Severity severity) => violations.Count(v => v.Severity == severity);
        return new SeverityCountsV1(
            Info: Count(Severity.Info),
            Warning: Count(Severity.Warning),
            Error: Count(Severity.Error),
            Critical: Count(Severity.Critical));
    }
}
