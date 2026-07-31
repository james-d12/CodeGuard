using System.Text.Json;
using CodeGuard.Core.Results;
using CodeGuard.Reporting.Json;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Core.Tests.Reporting;

public class JsonViolationReporterTests
{
    [Fact]
    public async Task WriteAsync_ProducesExpectedJsonShape()
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
        await new JsonViolationReporter().WriteAsync(result, writer);
        var output = writer.ToString();

        using var document = JsonDocument.Parse(output);
        var root = document.RootElement;

        Assert.Equal("failed", root.GetProperty("status").GetString());
        Assert.Equal(1, root.GetProperty("rulesEvaluated").GetInt32());

        var violation = root.GetProperty("violations")[0];
        Assert.Equal("DDD-ENTITY-001", violation.GetProperty("ruleId").GetString());
        Assert.Equal("error", violation.GetProperty("severity").GetString());
        Assert.Equal("LegacyThing.cs", violation.GetProperty("file").GetString());
        Assert.Equal(5, violation.GetProperty("line").GetInt32());
    }

    [Fact]
    public async Task WriteAsync_OmitsNullFields()
    {
        var result = new ValidationResult(
            ValidationStatus.Passed, RulesEvaluated: 1, RulesPassed: 1, RulesFailed: 0, RulesErrored: 0,
            Violations: [], EvaluationErrors: [], EvaluatedAtUtc: DateTimeOffset.UtcNow);

        var writer = new StringWriter();
        await new JsonViolationReporter().WriteAsync(result, writer);
        var output = writer.ToString();

        using var document = JsonDocument.Parse(output);
        Assert.Equal("passed", document.RootElement.GetProperty("status").GetString());
        Assert.Empty(document.RootElement.GetProperty("violations").EnumerateArray());
    }

    [Fact]
    public async Task WriteAsync_IncludesEvaluationErrors()
    {
        var result = new ValidationResult(
            ValidationStatus.PartiallyEvaluated, RulesEvaluated: 1, RulesPassed: 0, RulesFailed: 0, RulesErrored: 1,
            Violations: [],
            EvaluationErrors: [new RuleEvaluationError("BROKEN-001", "System.InvalidOperationException", "boom", "at Foo.Bar()")],
            EvaluatedAtUtc: DateTimeOffset.UtcNow);

        var writer = new StringWriter();
        await new JsonViolationReporter().WriteAsync(result, writer);
        var output = writer.ToString();

        using var document = JsonDocument.Parse(output);
        Assert.Equal("partiallyEvaluated", document.RootElement.GetProperty("status").GetString());
        Assert.Equal(1, document.RootElement.GetProperty("rulesErrored").GetInt32());

        var error = document.RootElement.GetProperty("evaluationErrors")[0];
        Assert.Equal("BROKEN-001", error.GetProperty("ruleId").GetString());
        Assert.Equal("System.InvalidOperationException", error.GetProperty("exceptionType").GetString());
        Assert.Equal("boom", error.GetProperty("message").GetString());
        Assert.Equal("at Foo.Bar()", error.GetProperty("stackTrace").GetString());
    }
}
