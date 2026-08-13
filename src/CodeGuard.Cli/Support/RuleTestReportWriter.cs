using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeGuard.Cli.Support;

/// <summary>Renders the per-rule, per-test-case results from <see cref="RuleTestRunner"/> for `rules test`.</summary>
public static class RuleTestReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static void WriteConsole(IReadOnlyList<RuleTestCaseResult> results, TextWriter writer, bool useColor = false)
    {
        writer.WriteLine("Rule tests");
        writer.WriteLine();

        foreach (var ruleGroup in results.GroupBy(r => r.RuleId).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            WriteRuleGroup(writer, ruleGroup, useColor);
        }

        writer.WriteLine();
        WriteSummary(writer, results);
    }

    private static void WriteRuleGroup(TextWriter writer, IGrouping<string, RuleTestCaseResult> ruleGroup, bool useColor)
    {
        var groupResults = ruleGroup.ToList();
        var passedCount = groupResults.Count(r => r.Outcome == TestOutcome.Passed);
        var allPassed = passedCount == groupResults.Count;

        var ruleSymbol = Symbol(allPassed ? TestOutcome.Passed : TestOutcome.Failed, useColor);
        writer.WriteLine($"{ruleSymbol} {ruleGroup.Key}  {passedCount}/{groupResults.Count} passed");

        if (allPassed)
        {
            return;
        }

        foreach (var result in groupResults.Where(r => r.Outcome != TestOutcome.Passed))
        {
            WriteCase(writer, result, useColor);
        }
    }

    private static void WriteCase(TextWriter writer, RuleTestCaseResult result, bool useColor)
    {
        writer.WriteLine($"  {Symbol(result.Outcome, useColor)} {result.TestName}");
        if (result.FailureReason is not null)
        {
            writer.WriteLine($"      {result.FailureReason}");
        }
    }

    private static string Symbol(TestOutcome outcome, bool useColor)
    {
        var symbol = outcome switch
        {
            TestOutcome.Passed => "✓",
            TestOutcome.Failed => "✗",
            _ => "!"
        };

        return useColor ? TestOutcomeColors.Colorize(outcome, symbol) : symbol;
    }

    private static void WriteSummary(TextWriter writer, IReadOnlyList<RuleTestCaseResult> results)
    {
        var passed = results.Count(r => r.Outcome == TestOutcome.Passed);
        var failed = results.Count(r => r.Outcome == TestOutcome.Failed);
        var errored = results.Count(r => r.Outcome == TestOutcome.Errored);

        var summary = $"Tests: {results.Count}  Passed: {passed}  Failed: {failed}";
        if (errored > 0)
        {
            summary += $"  Errored: {errored}";
        }

        writer.WriteLine(summary);
    }

    public static void WriteJson(IReadOnlyList<RuleTestCaseResult> results, TextWriter writer)
    {
        var summary = new RuleTestSummary(
            results.Count,
            results.Count(r => r.Outcome == TestOutcome.Passed),
            results.Count(r => r.Outcome == TestOutcome.Failed),
            results.Count(r => r.Outcome == TestOutcome.Errored),
            results);

        writer.WriteLine(JsonSerializer.Serialize(summary, JsonOptions));
    }

    private sealed record RuleTestSummary(
        int Total,
        int Passed,
        int Failed,
        int Errored,
        IReadOnlyList<RuleTestCaseResult> Results);
}
