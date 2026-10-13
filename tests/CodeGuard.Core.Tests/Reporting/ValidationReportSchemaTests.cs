using System.Text.Json;
using CodeGuard.Core.Results;
using CodeGuard.Reporting.Json;
using CodeGuard.Reporting.Provenance;
using CodeGuard.RuleModel.Rules;
using Json.Schema;

namespace CodeGuard.Core.Tests.Reporting;

public class ValidationReportSchemaTests
{
    private const string EmbeddedResourceName = "CodeGuard.Reporting.Schemas.validation-report.v1.schema.json";

    // JsonSchema.Net registers schemas globally by $id, so parse the report schema once per process.
    private static readonly Lazy<JsonSchema> ReportSchema = new(() => JsonSchema.FromText(LoadEmbeddedSchemaText()));

    [Fact]
    public void EmbeddedSchema_IsValidDraft202012Document()
    {
        using var document = JsonDocument.Parse(LoadEmbeddedSchemaText());

        var results = MetaSchemas.Draft202012.Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });

        Assert.True(results.IsValid, Describe(results));
    }

    [Fact]
    public async Task Output_WithViolations_ConformsToSchema()
    {
        var result = new ValidationResult(
            Status: ValidationStatus.Failed, RulesEvaluated: 2, RulesPassed: 1, RulesFailed: 1, RulesErrored: 0,
            Violations:
            [
                new Violation(
                    RuleId: "DDD-ENTITY-001", RuleVersion: 1, Severity: Severity.Error,
                    Message: "msg", File: "LegacyThing.cs", Line: 5, Column: 14,
                    Symbol: "Contoso.Domain.Entities.LegacyThing", Project: "Contoso.Domain",
                    Remediation: "fix", DocumentationReferences: ["docs/a.md"]),
                new Violation(
                    RuleId: "NAMING-001", RuleVersion: 2, Severity: Severity.Info,
                    Message: "msg", File: null, Line: null, Column: null,
                    Symbol: null, Project: null, Remediation: null, DocumentationReferences: [])
            ],
            EvaluationErrors: [],
            EvaluatedAtUtc: DateTimeOffset.UtcNow);

        var output = await WriteAsync(result, TimeSpan.FromMilliseconds(250));

        AssertConforms(output);
    }

    [Fact]
    public async Task Output_WithNoViolations_ConformsToSchema()
    {
        var result = new ValidationResult(
            ValidationStatus.Passed, RulesEvaluated: 1, RulesPassed: 1, RulesFailed: 0, RulesErrored: 0,
            Violations: [], EvaluationErrors: [], EvaluatedAtUtc: DateTimeOffset.UtcNow);

        var output = await WriteAsync(result, TimeSpan.Zero);

        AssertConforms(output);
    }

    [Fact]
    public async Task Output_WithEvaluationErrorsAndAnalysisWarnings_ConformsToSchema()
    {
        var result = new ValidationResult(
            ValidationStatus.PartiallyEvaluated, RulesEvaluated: 1, RulesPassed: 0, RulesFailed: 0, RulesErrored: 1,
            Violations: [],
            EvaluationErrors: [new RuleEvaluationError("BROKEN-001", "System.InvalidOperationException", "boom", "at Foo.Bar()")],
            EvaluatedAtUtc: DateTimeOffset.UtcNow,
            AnalysisWarnings: [new AnalysisWarning("MSBUILD-WORKSPACE", "partial load", "Contoso.Domain", "/repo/x.csproj")]);

        var output = await WriteAsync(result, TimeSpan.FromSeconds(3));

        AssertConforms(output);

        using var document = JsonDocument.Parse(output);
        Assert.Equal("partiallyEvaluated", document.RootElement.GetProperty("status").GetString());
        Assert.Equal(3000, document.RootElement.GetProperty("durationMs").GetInt64());
    }

    [Fact]
    public void Schema_RejectsUnknownTopLevelProperty()
    {
        const string document = """
            {
              "schemaVersion": "1.0", "toolVersion": "1.0.0", "status": "passed",
              "evaluatedAtUtc": "2026-01-01T00:00:00+00:00", "durationMs": 0,
              "metrics": {
                "rulesEvaluated": 0, "rulesPassed": 0, "rulesFailed": 0, "rulesErrored": 0,
                "violationCount": 0,
                "violationsBySeverity": { "info": 0, "warning": 0, "error": 0, "critical": 0 },
                "evaluationErrorCount": 0, "analysisWarningCount": 0
              },
              "violations": [], "evaluationErrors": [], "analysisWarnings": [],
              "rulesEvaluated": 0
            }
            """;

        using var parsed = JsonDocument.Parse(document);
        var results = ReportSchema.Value.Evaluate(parsed.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });

        Assert.False(results.IsValid);
    }

    [Fact]
    public async Task Output_WithProvenance_ConformsToSchemaAndCarriesFields()
    {
        var result = new ValidationResult(
            ValidationStatus.Passed, RulesEvaluated: 1, RulesPassed: 1, RulesFailed: 0, RulesErrored: 0,
            Violations: [], EvaluationErrors: [], EvaluatedAtUtc: DateTimeOffset.UtcNow);
        var provenance = new ValidationProvenance(
            Repository: "https://github.com/acme/app",
            Commit: "abc123",
            Branch: "main",
            RuleSources:
            [
                new RuleSetSource("git", "https://rules.example.com/acme/rules", "rules-sha"),
                new RuleSetSource("directory", "/repo/rules", null)
            ]);

        var writer = new StringWriter();
        await new JsonViolationReporter("1.2.3", TimeSpan.Zero, provenance).WriteAsync(result, writer);
        var output = writer.ToString();

        AssertConforms(output);
        using var document = JsonDocument.Parse(output);
        var root = document.RootElement;
        Assert.Equal("https://github.com/acme/app", root.GetProperty("repository").GetString());
        Assert.Equal("abc123", root.GetProperty("commit").GetString());
        Assert.Equal("main", root.GetProperty("branch").GetString());
        var sources = root.GetProperty("ruleSources");
        Assert.Equal("git", sources[0].GetProperty("kind").GetString());
        Assert.Equal("rules-sha", sources[0].GetProperty("commit").GetString());
        Assert.False(sources[1].TryGetProperty("commit", out _));
    }

    [Fact]
    public async Task Output_WithUnknownProvenance_OmitsFields()
    {
        var result = new ValidationResult(
            ValidationStatus.Passed, RulesEvaluated: 0, RulesPassed: 0, RulesFailed: 0, RulesErrored: 0,
            Violations: [], EvaluationErrors: [], EvaluatedAtUtc: DateTimeOffset.UtcNow);
        var provenance = new ValidationProvenance(null, null, null, []);

        var writer = new StringWriter();
        await new JsonViolationReporter("1.2.3", TimeSpan.Zero, provenance).WriteAsync(result, writer);
        var output = writer.ToString();

        AssertConforms(output);
        using var document = JsonDocument.Parse(output);
        Assert.False(document.RootElement.TryGetProperty("repository", out _));
        Assert.False(document.RootElement.TryGetProperty("commit", out _));
        Assert.False(document.RootElement.TryGetProperty("branch", out _));
        Assert.Empty(document.RootElement.GetProperty("ruleSources").EnumerateArray());
    }

    [Fact]
    public void Schema_RejectsCredentialFieldsInRuleSource()
    {
        const string document = """
            {
              "schemaVersion": "1.0", "toolVersion": "1.0.0", "status": "passed",
              "evaluatedAtUtc": "2026-01-01T00:00:00+00:00", "durationMs": 0,
              "metrics": {
                "rulesEvaluated": 0, "rulesPassed": 0, "rulesFailed": 0, "rulesErrored": 0,
                "violationCount": 0,
                "violationsBySeverity": { "info": 0, "warning": 0, "error": 0, "critical": 0 },
                "evaluationErrorCount": 0, "analysisWarningCount": 0
              },
              "violations": [], "evaluationErrors": [], "analysisWarnings": [],
              "ruleSources": [ { "kind": "git", "location": "https://u:p@host/x", "token": "p" } ]
            }
            """;

        using var parsed = JsonDocument.Parse(document);
        var results = ReportSchema.Value.Evaluate(parsed.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });

        Assert.False(results.IsValid);
    }

    private static async Task<string> WriteAsync(ValidationResult result, TimeSpan duration)
    {
        var writer = new StringWriter();
        await new JsonViolationReporter("1.2.3", duration).WriteAsync(result, writer);
        return writer.ToString();
    }

    private static void AssertConforms(string output)
    {
        using var document = JsonDocument.Parse(output);

        var results = ReportSchema.Value.Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });

        Assert.True(results.IsValid, $"Output does not conform to validation-report.v1 schema: {Describe(results)}\n{output}");
    }

    private static string Describe(EvaluationResults results) =>
        string.Join("; ", (results.Details ?? [])
            .Where(detail => detail.Errors is { Count: > 0 })
            .SelectMany(detail => detail.Errors!.Select(e => $"{detail.InstanceLocation}: {e.Value}")));

    private static string LoadEmbeddedSchemaText()
    {
        var assembly = typeof(JsonViolationReporter).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{EmbeddedResourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
