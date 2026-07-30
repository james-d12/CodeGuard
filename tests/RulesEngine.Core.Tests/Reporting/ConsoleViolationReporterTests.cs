using RulesEngine.Core.Results;
using RulesEngine.Reporting.Console;
using RulesEngine.RuleModel.Rules;

namespace RulesEngine.Core.Tests.Reporting;

public class ConsoleViolationReporterTests
{
    [Fact]
    public async Task WriteAsync_PrintsViolationDetailsAndSummary()
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
        await new ConsoleViolationReporter().WriteAsync(result, writer);
        var output = writer.ToString();

        Assert.Contains("[Error] DDD-ENTITY-001:", output);
        Assert.Contains("LegacyThing.cs(5,14)", output);
        Assert.Contains("remediation: Inherit from Contoso.Domain.Entity<TId>.", output);
        Assert.Contains("Rules evaluated: 1, passed: 0, failed: 1", output);
        Assert.Contains("Status: Failed", output);
    }

    [Fact]
    public async Task WriteAsync_PrintsSummaryOnly_WhenNoViolations()
    {
        var result = new ValidationResult(
            ValidationStatus.Passed, RulesEvaluated: 1, RulesPassed: 1, RulesFailed: 0,
            Violations: [], EvaluatedAtUtc: DateTimeOffset.UtcNow);

        var writer = new StringWriter();
        await new ConsoleViolationReporter().WriteAsync(result, writer);
        var output = writer.ToString();

        Assert.Contains("Status: Passed", output);
        Assert.DoesNotContain("[Error]", output);
    }

    [Fact]
    public async Task WriteAsync_WithColorEnabled_WrapsSeverityTagInAnsiCodes()
    {
        var result = SingleErrorResult();

        var writer = new StringWriter();
        await new ConsoleViolationReporter(useColor: true).WriteAsync(result, writer);
        var output = writer.ToString();

        Assert.Contains("[31m[Error][0m", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WriteAsync_WithColorDisabled_ContainsNoAnsiCodes()
    {
        var result = SingleErrorResult();

        var writer = new StringWriter();
        await new ConsoleViolationReporter(useColor: false).WriteAsync(result, writer);
        var output = writer.ToString();

        Assert.DoesNotContain("[", output, StringComparison.Ordinal);
    }

    private static ValidationResult SingleErrorResult() => new(
        Status: ValidationStatus.Failed,
        RulesEvaluated: 1,
        RulesPassed: 0,
        RulesFailed: 1,
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
        EvaluatedAtUtc: DateTimeOffset.UtcNow);
}
