using System.Text.Json;
using CodeGuard.Core.Results;
using CodeGuard.Reporting.Provenance;
using CodeGuard.Reporting.Sarif;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Core.Tests.Reporting;

public class SarifViolationReporterTests
{
    private static ValidationResult EmptyPassingResult() => new(
        ValidationStatus.Passed, RulesEvaluated: 0, RulesPassed: 0, RulesFailed: 0, RulesErrored: 0,
        Violations: [], EvaluationErrors: [], EvaluatedAtUtc: DateTimeOffset.UtcNow);

    [Fact]
    public async Task WriteAsync_WithProvenance_WritesVersionControlDetailsAndRuleSources()
    {
        var provenance = new ValidationProvenance(
            Repository: "https://github.com/acme/app",
            Commit: "abc123",
            Branch: "main",
            RuleSources: [new RuleSetSource("git", "https://rules.example.com/acme/rules", "rules-sha")]);

        var writer = new StringWriter();
        await new SarifViolationReporter(provenance, "1.2.3").WriteAsync(EmptyPassingResult(), writer);

        using var document = JsonDocument.Parse(writer.ToString());
        var run = document.RootElement.GetProperty("runs")[0];

        var driver = run.GetProperty("tool").GetProperty("driver");
        Assert.Equal("1.2.3", driver.GetProperty("version").GetString());

        var vcs = run.GetProperty("versionControlProvenance")[0];
        Assert.Equal("https://github.com/acme/app", vcs.GetProperty("repositoryUri").GetString());
        Assert.Equal("abc123", vcs.GetProperty("revisionId").GetString());
        Assert.Equal("main", vcs.GetProperty("branch").GetString());

        var ruleSource = run.GetProperty("properties").GetProperty("ruleSources")[0];
        Assert.Equal("git", ruleSource.GetProperty("kind").GetString());
        Assert.Equal("https://rules.example.com/acme/rules", ruleSource.GetProperty("location").GetString());
        Assert.Equal("rules-sha", ruleSource.GetProperty("commit").GetString());
    }

    [Fact]
    public async Task WriteAsync_WithoutProvenance_OmitsVersionControlDetails()
    {
        var writer = new StringWriter();
        await new SarifViolationReporter().WriteAsync(EmptyPassingResult(), writer);

        using var document = JsonDocument.Parse(writer.ToString());
        var run = document.RootElement.GetProperty("runs")[0];

        Assert.False(run.TryGetProperty("versionControlProvenance", out _));
        Assert.False(run.TryGetProperty("properties", out _));
        Assert.False(string.IsNullOrEmpty(run.GetProperty("tool").GetProperty("driver").GetProperty("version").GetString()));
    }

    [Fact]
    public async Task WriteAsync_WithoutRepositoryUri_OmitsVersionControlDetailsButKeepsRuleSources()
    {
        var provenance = new ValidationProvenance(
            Repository: null, Commit: "abc123", Branch: null,
            RuleSources: [new RuleSetSource("directory", "/repo/rules", null)]);

        var writer = new StringWriter();
        await new SarifViolationReporter(provenance, "1.2.3").WriteAsync(EmptyPassingResult(), writer);

        using var document = JsonDocument.Parse(writer.ToString());
        var run = document.RootElement.GetProperty("runs")[0];

        Assert.False(run.TryGetProperty("versionControlProvenance", out _));
        Assert.Equal("/repo/rules", run.GetProperty("properties").GetProperty("ruleSources")[0].GetProperty("location").GetString());
    }

    [Fact]
    public async Task WriteAsync_ProducesValidSarifLog()
    {
        var result = new ValidationResult(
            Status: ValidationStatus.Failed,
            RulesEvaluated: 1,
            RulesPassed: 0,
            RulesFailed: 1,
            RulesErrored: 0,
            Violations:
            [
                new Violation(
                    RuleId: "DDD-ENTITY-001",
                    RuleVersion: 3,
                    Severity: Severity.Error,
                    Message: "'Contoso.Domain.Entities.LegacyThing' must inherit from 'Contoso.Domain.Entity<TId>'.",
                    File: "LegacyThing.cs",
                    Line: 5,
                    Column: 14,
                    Symbol: "Contoso.Domain.Entities.LegacyThing",
                    Project: "Contoso.Domain",
                    Remediation: "Inherit from Contoso.Domain.Entity<TId>.",
                    DocumentationReferences: [])
            ],
            EvaluationErrors: [],
            EvaluatedAtUtc: DateTimeOffset.UtcNow);

        var writer = new StringWriter();
        await new SarifViolationReporter().WriteAsync(result, writer);
        var output = writer.ToString();

        using var document = JsonDocument.Parse(output);
        var root = document.RootElement;

        Assert.Equal("2.1.0", root.GetProperty("version").GetString());

        var run = root.GetProperty("runs")[0];
        Assert.Equal("codeguard", run.GetProperty("tool").GetProperty("driver").GetProperty("name").GetString());

        var rules = run.GetProperty("tool").GetProperty("driver").GetProperty("rules");
        Assert.Equal("DDD-ENTITY-001", rules[0].GetProperty("id").GetString());
        Assert.Equal(3, rules[0].GetProperty("properties").GetProperty("version").GetInt32());

        var sarifResult = run.GetProperty("results")[0];
        Assert.Equal("DDD-ENTITY-001", sarifResult.GetProperty("ruleId").GetString());
        Assert.Equal("error", sarifResult.GetProperty("level").GetString());

        var location = sarifResult.GetProperty("locations")[0].GetProperty("physicalLocation");
        Assert.Equal("LegacyThing.cs", location.GetProperty("artifactLocation").GetProperty("uri").GetString());
        Assert.Equal(5, location.GetProperty("region").GetProperty("startLine").GetInt32());
        Assert.Equal(14, location.GetProperty("region").GetProperty("startColumn").GetInt32());
    }

    [Fact]
    public async Task WriteAsync_MapsSeverityToSarifLevel()
    {
        var result = new ValidationResult(
            ValidationStatus.Failed, RulesEvaluated: 1, RulesPassed: 0, RulesFailed: 1, RulesErrored: 0,
            Violations:
            [
                new Violation("RULE-INFO", 1, Severity.Info, "info message", null, null, null, null, null, null, []),
                new Violation("RULE-WARN", 1, Severity.Warning, "warning message", null, null, null, null, null, null, []),
                new Violation("RULE-CRIT", 1, Severity.Critical, "critical message", null, null, null, null, null, null, [])
            ],
            EvaluationErrors: [],
            EvaluatedAtUtc: DateTimeOffset.UtcNow);

        var writer = new StringWriter();
        await new SarifViolationReporter().WriteAsync(result, writer);

        using var document = JsonDocument.Parse(writer.ToString());
        var results = document.RootElement.GetProperty("runs")[0].GetProperty("results");

        Assert.Equal("note", results[0].GetProperty("level").GetString());
        // "warning" is the SARIF spec's implicit default level, so the SDK omits it rather than serializing it explicitly.
        Assert.False(results[1].TryGetProperty("level", out _));
        Assert.Equal("error", results[2].GetProperty("level").GetString());
        Assert.False(results[0].TryGetProperty("locations", out _));
    }

    [Fact]
    public async Task WriteAsync_MapsEvaluationErrorsToToolExecutionNotifications()
    {
        var result = new ValidationResult(
            ValidationStatus.PartiallyEvaluated, RulesEvaluated: 1, RulesPassed: 0, RulesFailed: 0, RulesErrored: 1,
            Violations: [],
            EvaluationErrors: [new RuleEvaluationError("BROKEN-001", "System.InvalidOperationException", "boom", null)],
            EvaluatedAtUtc: DateTimeOffset.UtcNow);

        var writer = new StringWriter();
        await new SarifViolationReporter().WriteAsync(result, writer);

        using var document = JsonDocument.Parse(writer.ToString());
        var invocation = document.RootElement.GetProperty("runs")[0].GetProperty("invocations")[0];

        Assert.False(invocation.GetProperty("executionSuccessful").GetBoolean());
        var notification = invocation.GetProperty("toolExecutionNotifications")[0];
        Assert.Equal("BROKEN-001", notification.GetProperty("descriptor").GetProperty("id").GetString());
        Assert.Equal("error", notification.GetProperty("level").GetString());
        Assert.Contains("System.InvalidOperationException", notification.GetProperty("message").GetProperty("text").GetString());
        Assert.Contains("boom", notification.GetProperty("message").GetProperty("text").GetString());
    }

    [Fact]
    public async Task WriteAsync_MapsAnalysisWarningsToToolExecutionNotifications()
    {
        var result = new ValidationResult(
            ValidationStatus.Failed, RulesEvaluated: 1, RulesPassed: 0, RulesFailed: 1, RulesErrored: 0,
            Violations: [],
            EvaluationErrors: [],
            EvaluatedAtUtc: DateTimeOffset.UtcNow,
            AnalysisWarnings: [new AnalysisWarning(
                "MSBUILD-WORKSPACE", "Unable to find fallback package folder 'X'.", "Contoso.Domain",
                "/repo/src/Contoso.Domain/Contoso.Domain.csproj")]);

        var writer = new StringWriter();
        await new SarifViolationReporter().WriteAsync(result, writer);

        using var document = JsonDocument.Parse(writer.ToString());
        var invocation = document.RootElement.GetProperty("runs")[0].GetProperty("invocations")[0];

        var notification = invocation.GetProperty("toolExecutionNotifications")[0];
        Assert.Equal("MSBUILD-WORKSPACE", notification.GetProperty("descriptor").GetProperty("id").GetString());
        Assert.Equal("Unable to find fallback package folder 'X'.", notification.GetProperty("message").GetProperty("text").GetString());
    }

    [Fact]
    public async Task WriteAsync_MapsEndPositionsAndMetadata()
    {
        var result = CreateResult(new Violation(
            RuleId: "DDD-ENTITY-001", RuleVersion: 1, Severity: Severity.Error, Message: "msg",
            File: "LegacyThing.cs", Line: 5, Column: 14, Symbol: null, Project: null, Remediation: null,
            DocumentationReferences: [], EndLine: 5, EndColumn: 22,
            Metadata: new Dictionary<string, string> { ["expectedBase"] = "Entity<TId>" }));

        var sarifResult = await WriteSingleResultAsync(result);

        var region = sarifResult.GetProperty("locations")[0].GetProperty("physicalLocation").GetProperty("region");
        Assert.Equal(5, region.GetProperty("startLine").GetInt32());
        Assert.Equal(14, region.GetProperty("startColumn").GetInt32());
        Assert.Equal(5, region.GetProperty("endLine").GetInt32());
        Assert.Equal(22, region.GetProperty("endColumn").GetInt32());
        Assert.Equal("Entity<TId>", sarifResult.GetProperty("properties").GetProperty("expectedBase").GetString());
    }

    [Fact]
    public async Task WriteAsync_WithoutEndSpan_EmitsStartOnlyRegion()
    {
        var result = CreateResult(new Violation(
            RuleId: "DDD-ENTITY-001", RuleVersion: 1, Severity: Severity.Error, Message: "msg",
            File: "LegacyThing.cs", Line: 5, Column: 14, Symbol: null, Project: null, Remediation: null,
            DocumentationReferences: []));

        var sarifResult = await WriteSingleResultAsync(result);

        var region = sarifResult.GetProperty("locations")[0].GetProperty("physicalLocation").GetProperty("region");
        Assert.Equal(5, region.GetProperty("startLine").GetInt32());
        Assert.Equal(14, region.GetProperty("startColumn").GetInt32());
        Assert.False(region.TryGetProperty("endLine", out _));
        Assert.False(region.TryGetProperty("endColumn", out _));
        Assert.False(sarifResult.TryGetProperty("properties", out _));
    }

    [Fact]
    public async Task WriteAsync_PutsExpectedActualAndEvidenceInPropertyBag()
    {
        var result = CreateResult(new Violation(
            RuleId: "DDD-ENTITY-001", RuleVersion: 1, Severity: Severity.Error, Message: "msg",
            File: "LegacyThing.cs", Line: 5, Column: 14, Symbol: null, Project: null, Remediation: null,
            DocumentationReferences: [],
            Expected: "Contoso.Domain.Entity<*>", Actual: "Contoso.Other.Base",
            Evidence: ["Contoso.Domain.Entities.LegacyThing", "Contoso.Domain.Entities.Other"]));

        var sarifResult = await WriteSingleResultAsync(result);

        var properties = sarifResult.GetProperty("properties");
        Assert.Equal("Contoso.Domain.Entity<*>", properties.GetProperty("expected").GetString());
        Assert.Equal("Contoso.Other.Base", properties.GetProperty("actual").GetString());
        var evidence = properties.GetProperty("evidence");
        Assert.Equal(JsonValueKind.Array, evidence.ValueKind);
        Assert.Equal(2, evidence.GetArrayLength());
        Assert.Equal("Contoso.Domain.Entities.LegacyThing", evidence[0].GetString());
    }

    [Fact]
    public async Task WriteAsync_MetadataKeyWins_OverStructuredEvidenceKey()
    {
        var result = CreateResult(new Violation(
            RuleId: "R-1", RuleVersion: 1, Severity: Severity.Error, Message: "msg",
            File: null, Line: null, Column: null, Symbol: null, Project: null, Remediation: null,
            DocumentationReferences: [],
            Expected: "structured", Actual: null, Evidence: null,
            Metadata: new Dictionary<string, string> { ["expected"] = "from metadata" }));

        var sarifResult = await WriteSingleResultAsync(result);

        Assert.Equal("from metadata", sarifResult.GetProperty("properties").GetProperty("expected").GetString());
    }

    private static ValidationResult CreateResult(Violation violation) => new(
        Status: ValidationStatus.Failed, RulesEvaluated: 1, RulesPassed: 0, RulesFailed: 1, RulesErrored: 0,
        Violations: [violation], EvaluationErrors: [], EvaluatedAtUtc: DateTimeOffset.UtcNow);

    private static async Task<JsonElement> WriteSingleResultAsync(ValidationResult result)
    {
        var writer = new StringWriter();
        await new SarifViolationReporter().WriteAsync(result, writer);
        using var document = JsonDocument.Parse(writer.ToString());
        return document.RootElement.GetProperty("runs")[0].GetProperty("results")[0].Clone();
    }
}
