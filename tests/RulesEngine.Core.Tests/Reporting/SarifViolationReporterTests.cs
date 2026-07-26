using System.Text.Json;
using RulesEngine.Core.Results;
using RulesEngine.Reporting.Sarif;
using RulesEngine.RuleModel.Rules;

namespace RulesEngine.Core.Tests.Reporting;

public class SarifViolationReporterTests
{
    [Fact]
    public async Task WriteAsync_ProducesValidSarifLog()
    {
        var result = new ValidationResult(
            Status: ValidationStatus.Failed,
            RulesEvaluated: 1,
            RulesPassed: 0,
            RulesFailed: 1,
            Violations:
            [
                new Violation(
                    RuleId: "DDD-ENTITY-001",
                    StandardId: "DDD-001",
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
            EvaluatedAtUtc: DateTimeOffset.UtcNow);

        var writer = new StringWriter();
        await new SarifViolationReporter().WriteAsync(result, writer);
        var output = writer.ToString();

        using var document = JsonDocument.Parse(output);
        var root = document.RootElement;

        Assert.Equal("2.1.0", root.GetProperty("version").GetString());

        var run = root.GetProperty("runs")[0];
        Assert.Equal("rules-engine", run.GetProperty("tool").GetProperty("driver").GetProperty("name").GetString());

        var rules = run.GetProperty("tool").GetProperty("driver").GetProperty("rules");
        Assert.Equal("DDD-ENTITY-001", rules[0].GetProperty("id").GetString());

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
            ValidationStatus.Failed, RulesEvaluated: 1, RulesPassed: 0, RulesFailed: 1,
            Violations:
            [
                new Violation("RULE-INFO", null, Severity.Info, "info message", null, null, null, null, null, null, []),
                new Violation("RULE-WARN", null, Severity.Warning, "warning message", null, null, null, null, null, null, []),
                new Violation("RULE-CRIT", null, Severity.Critical, "critical message", null, null, null, null, null, null, [])
            ],
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
}
