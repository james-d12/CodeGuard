using CodeGuard.Core.Results;
using CodeGuard.Reporting.Contracts;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Core.Tests.Reporting;

public class ValidationReportMapperTests
{
    [Theory]
    [InlineData(ValidationStatus.Passed, "passed")]
    [InlineData(ValidationStatus.Failed, "failed")]
    [InlineData(ValidationStatus.PartiallyEvaluated, "partiallyEvaluated")]
    public void ToV1_MapsStatusToWireValue(ValidationStatus status, string expected)
    {
        var report = ValidationReportMapper.ToV1(CreateResult(status), TimeSpan.Zero, "1.2.3");

        Assert.Equal(expected, report.Status);
    }

    [Fact]
    public void ToV1_SetsSchemaVersionAndToolVersion()
    {
        var report = ValidationReportMapper.ToV1(CreateResult(ValidationStatus.Passed), TimeSpan.Zero, "1.2.3");

        Assert.Equal("1.0", report.SchemaVersion);
        Assert.Equal("1.2.3", report.ToolVersion);
    }

    [Fact]
    public void ToV1_ConvertsDurationToWholeMilliseconds()
    {
        var report = ValidationReportMapper.ToV1(
            CreateResult(ValidationStatus.Passed), TimeSpan.FromMilliseconds(1234.9), "1.0.0");

        Assert.Equal(1234, report.DurationMs);
    }

    [Fact]
    public void ToV1_CountsMetricsAndViolationsBySeverity()
    {
        var result = new ValidationResult(
            Status: ValidationStatus.Failed,
            RulesEvaluated: 5,
            RulesPassed: 2,
            RulesFailed: 2,
            RulesErrored: 1,
            Violations:
            [
                CreateViolation("R-1", Severity.Info),
                CreateViolation("R-2", Severity.Error),
                CreateViolation("R-2", Severity.Error),
                CreateViolation("R-3", Severity.Critical)
            ],
            EvaluationErrors: [new RuleEvaluationError("R-4", "System.Exception", "boom", null)],
            EvaluatedAtUtc: DateTimeOffset.UtcNow,
            AnalysisWarnings: [new AnalysisWarning("MSBUILD-WORKSPACE", "partial load", null, null)]);

        var report = ValidationReportMapper.ToV1(result, TimeSpan.Zero, "1.0.0");

        Assert.Equal(5, report.Metrics.RulesEvaluated);
        Assert.Equal(2, report.Metrics.RulesPassed);
        Assert.Equal(2, report.Metrics.RulesFailed);
        Assert.Equal(1, report.Metrics.RulesErrored);
        Assert.Equal(4, report.Metrics.ViolationCount);
        Assert.Equal(new SeverityCountsV1(Info: 1, Warning: 0, Error: 2, Critical: 1), report.Metrics.ViolationsBySeverity);
        Assert.Equal(1, report.Metrics.EvaluationErrorCount);
        Assert.Equal(1, report.Metrics.AnalysisWarningCount);
    }

    [Fact]
    public void ToV1_MapsViolationFieldsAndSeverityWireValue()
    {
        var violation = new Violation(
            RuleId: "DDD-ENTITY-001",
            RuleVersion: 3,
            Severity: Severity.Warning,
            Message: "msg",
            File: "LegacyThing.cs",
            Line: 5,
            Column: 14,
            Symbol: "Contoso.Domain.Entities.LegacyThing",
            Project: "Contoso.Domain",
            Remediation: "fix it",
            DocumentationReferences: ["docs/x.md"]);
        var result = CreateResult(ValidationStatus.Failed, violations: [violation]);

        var mapped = ValidationReportMapper.ToV1(result, TimeSpan.Zero, "1.0.0").Violations.Single();

        Assert.Equal("DDD-ENTITY-001", mapped.RuleId);
        Assert.Equal(3, mapped.RuleVersion);
        Assert.Equal("warning", mapped.Severity);
        Assert.Equal("LegacyThing.cs", mapped.File);
        Assert.Equal(5, mapped.Line);
        Assert.Equal(14, mapped.Column);
        Assert.Equal("Contoso.Domain.Entities.LegacyThing", mapped.Symbol);
        Assert.Equal("Contoso.Domain", mapped.Project);
        Assert.Equal("fix it", mapped.Remediation);
        Assert.Equal(["docs/x.md"], mapped.DocumentationReferences);
    }

    [Fact]
    public void ResolveToolVersion_StripsBuildMetadataSuffix()
    {
        var version = ValidationReportMapper.ResolveToolVersion(typeof(ValidationReportMapperTests).Assembly);

        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.DoesNotContain('+', version);
    }

    private static Violation CreateViolation(string ruleId, Severity severity) => new(
        RuleId: ruleId,
        RuleVersion: 1,
        Severity: severity,
        Message: "msg",
        File: null,
        Line: null,
        Column: null,
        Symbol: null,
        Project: null,
        Remediation: null,
        DocumentationReferences: []);

    private static ValidationResult CreateResult(ValidationStatus status, IReadOnlyList<Violation>? violations = null) => new(
        Status: status,
        RulesEvaluated: 1,
        RulesPassed: 1,
        RulesFailed: 0,
        RulesErrored: 0,
        Violations: violations ?? [],
        EvaluationErrors: [],
        EvaluatedAtUtc: DateTimeOffset.UtcNow);

    [Fact]
    public void ToV1_MapsEndPositionAndMetadata()
    {
        var violation = new Violation(
            RuleId: "R-1", RuleVersion: 1, Severity: Severity.Error, Message: "msg",
            File: "F.cs", Line: 1, Column: 2, Symbol: null, Project: null, Remediation: null,
            DocumentationReferences: [], EndLine: 3, EndColumn: 4,
            Metadata: new Dictionary<string, string> { ["k"] = "v" });
        var result = new ValidationResult(
            Status: ValidationStatus.Failed, RulesEvaluated: 1, RulesPassed: 0, RulesFailed: 1, RulesErrored: 0,
            Violations: [violation], EvaluationErrors: [], EvaluatedAtUtc: DateTimeOffset.UtcNow);

        var mapped = ValidationReportMapper.ToV1(result, TimeSpan.Zero, "1.0.0").Violations.Single();

        Assert.Equal(3, mapped.EndLine);
        Assert.Equal(4, mapped.EndColumn);
        Assert.Equal("v", mapped.Metadata!["k"]);
    }

    [Fact]
    public void ToV1_MapsExpectedActualAndEvidence()
    {
        var violation = new Violation(
            RuleId: "R-1", RuleVersion: 1, Severity: Severity.Error, Message: "msg",
            File: "F.cs", Line: 1, Column: 2, Symbol: null, Project: null, Remediation: null,
            DocumentationReferences: [],
            Expected: "Contoso.Domain.Entity<*>", Actual: "Contoso.Other.Base",
            Evidence: ["Contoso.Domain.Entities.LegacyThing"]);
        var result = new ValidationResult(
            Status: ValidationStatus.Failed, RulesEvaluated: 1, RulesPassed: 0, RulesFailed: 1, RulesErrored: 0,
            Violations: [violation], EvaluationErrors: [], EvaluatedAtUtc: DateTimeOffset.UtcNow);

        var mapped = ValidationReportMapper.ToV1(result, TimeSpan.Zero, "1.0.0").Violations.Single();

        Assert.Equal("Contoso.Domain.Entity<*>", mapped.Expected);
        Assert.Equal("Contoso.Other.Base", mapped.Actual);
        Assert.Equal(["Contoso.Domain.Entities.LegacyThing"], mapped.Evidence);
    }

    [Fact]
    public void ToV1_LeavesEvidenceFieldsNull_WhenNotProduced()
    {
        var violation = new Violation(
            RuleId: "R-1", RuleVersion: 1, Severity: Severity.Error, Message: "msg",
            File: null, Line: null, Column: null, Symbol: null, Project: null, Remediation: null,
            DocumentationReferences: []);
        var result = new ValidationResult(
            Status: ValidationStatus.Failed, RulesEvaluated: 1, RulesPassed: 0, RulesFailed: 1, RulesErrored: 0,
            Violations: [violation], EvaluationErrors: [], EvaluatedAtUtc: DateTimeOffset.UtcNow);

        var mapped = ValidationReportMapper.ToV1(result, TimeSpan.Zero, "1.0.0").Violations.Single();

        Assert.Null(mapped.Expected);
        Assert.Null(mapped.Actual);
        Assert.Null(mapped.Evidence);
    }
}
