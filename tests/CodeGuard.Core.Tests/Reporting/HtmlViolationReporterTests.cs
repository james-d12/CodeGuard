using CodeGuard.Core.Results;
using CodeGuard.Reporting.Html;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Core.Tests.Reporting;

public class HtmlViolationReporterTests
{
    [Fact]
    public async Task WriteAsync_EscapesHtmlSensitiveCharactersInViolationText()
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
                    Message: "<script>alert('xss')</script> & \"quoted\"",
                    File: "LegacyThing.cs",
                    Line: 5,
                    Column: 14,
                    Symbol: "Contoso.Domain.Entities.LegacyThing",
                    Project: "Contoso.Domain",
                    Remediation: null,
                    DocumentationReferences: [])
            ],
            EvaluationErrors: [],
            EvaluatedAtUtc: DateTimeOffset.UtcNow);

        var writer = new StringWriter();
        await new HtmlViolationReporter().WriteAsync(result, writer);
        var output = writer.ToString();

        Assert.DoesNotContain("<script>alert", output);
        Assert.Contains("&lt;script&gt;", output);
        Assert.Contains("&amp;", output);
        Assert.Contains("&quot;quoted&quot;", output);
    }

    [Fact]
    public async Task WriteAsync_IncludesFilterControlsAndDataAttributes()
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
                    Message: "must inherit from Entity<TId>.",
                    File: "LegacyThing.cs",
                    Line: 5,
                    Column: 14,
                    Symbol: "Contoso.Domain.Entities.LegacyThing",
                    Project: "Contoso.Domain",
                    Remediation: null,
                    DocumentationReferences: [])
            ],
            EvaluationErrors: [],
            EvaluatedAtUtc: DateTimeOffset.UtcNow);

        var writer = new StringWriter();
        await new HtmlViolationReporter().WriteAsync(result, writer);
        var output = writer.ToString();

        Assert.Contains("id=\"rule-id-filter\"", output);
        Assert.Contains("id=\"message-search\"", output);
        Assert.Contains("id=\"project-filter\"", output);
        Assert.Contains("data-severity=\"Error\"", output);
        Assert.Contains("data-rule-id=\"DDD-ENTITY-001\"", output);
        Assert.Contains("data-project=\"Contoso.Domain\"", output);
    }

    [Fact]
    public async Task WriteAsync_IncludesSummaryCounts()
    {
        var result = new ValidationResult(
            Status: ValidationStatus.Failed,
            RulesEvaluated: 3,
            RulesPassed: 2,
            RulesFailed: 1,
            RulesErrored: 0,
            Violations:
            [
                new Violation(
                    RuleId: "DDD-ENTITY-001",
                    Severity: Severity.Error,
                    Message: "must inherit from Entity<TId>.",
                    File: "LegacyThing.cs",
                    Line: 5,
                    Column: 14,
                    Symbol: "Contoso.Domain.Entities.LegacyThing",
                    Project: "Contoso.Domain",
                    Remediation: null,
                    DocumentationReferences: [])
            ],
            EvaluationErrors: [],
            EvaluatedAtUtc: DateTimeOffset.UtcNow);

        var writer = new StringWriter();
        await new HtmlViolationReporter().WriteAsync(result, writer);
        var output = writer.ToString();

        Assert.Contains("Rules evaluated: 3, passed: 2, failed: 1, errored: 0", output);
        Assert.Contains("Status: Failed", output);
    }

    [Fact]
    public async Task WriteAsync_PrintsEmptyState_WhenNoViolations()
    {
        var result = new ValidationResult(
            ValidationStatus.Passed, RulesEvaluated: 1, RulesPassed: 1, RulesFailed: 0, RulesErrored: 0,
            Violations: [], EvaluationErrors: [], EvaluatedAtUtc: DateTimeOffset.UtcNow);

        var writer = new StringWriter();
        await new HtmlViolationReporter().WriteAsync(result, writer);
        var output = writer.ToString();

        Assert.Contains("Status: Passed", output);
        Assert.Contains("class=\"empty-state\"", output);
    }

    [Fact]
    public async Task WriteAsync_IsFullySelfContained()
    {
        var result = new ValidationResult(
            ValidationStatus.Passed, RulesEvaluated: 1, RulesPassed: 1, RulesFailed: 0, RulesErrored: 0,
            Violations: [], EvaluationErrors: [], EvaluatedAtUtc: DateTimeOffset.UtcNow);

        var writer = new StringWriter();
        await new HtmlViolationReporter().WriteAsync(result, writer);
        var output = writer.ToString();

        Assert.DoesNotContain("http://", output);
        Assert.DoesNotContain("https://", output);
        Assert.DoesNotContain("<script src=", output);
        Assert.DoesNotContain("<link", output);
    }

    [Fact]
    public async Task WriteAsync_ProducesWellFormedShell()
    {
        var result = new ValidationResult(
            ValidationStatus.Passed, RulesEvaluated: 1, RulesPassed: 1, RulesFailed: 0, RulesErrored: 0,
            Violations: [], EvaluationErrors: [], EvaluatedAtUtc: DateTimeOffset.UtcNow);

        var writer = new StringWriter();
        await new HtmlViolationReporter().WriteAsync(result, writer);
        var output = writer.ToString();

        Assert.StartsWith("<!DOCTYPE html>", output);
        Assert.Equal(1, CountOccurrences(output, "<style>"));
        Assert.Equal(1, CountOccurrences(output, "<script>"));
    }

    [Fact]
    public async Task WriteAsync_IncludesEvaluationErrorsSection()
    {
        var result = new ValidationResult(
            ValidationStatus.PartiallyEvaluated, RulesEvaluated: 1, RulesPassed: 0, RulesFailed: 0, RulesErrored: 1,
            Violations: [],
            EvaluationErrors: [new RuleEvaluationError("BROKEN-001", "System.InvalidOperationException", "boom", null)],
            EvaluatedAtUtc: DateTimeOffset.UtcNow);

        var writer = new StringWriter();
        await new HtmlViolationReporter().WriteAsync(result, writer);
        var output = writer.ToString();

        Assert.Contains("Rules that could not be evaluated", output);
        Assert.Contains("BROKEN-001", output);
        Assert.Contains("System.InvalidOperationException: boom", output);
        Assert.Contains("Rules evaluated: 1, passed: 0, failed: 0, errored: 1", output);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
