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

    public static void WriteConsole(IReadOnlyList<RuleTestCaseResult> results, TextWriter writer)
    {
        writer.WriteLine("Rule tests");
        writer.WriteLine();

        foreach (var ruleGroup in results.GroupBy(r => r.RuleId).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var ruleSymbol = ruleGroup.All(r => r.Outcome == TestOutcome.Passed) ? "✓" : "✗";
            writer.WriteLine($"{ruleSymbol} {ruleGroup.Key}");

            foreach (var result in ruleGroup)
            {
                var symbol = result.Outcome switch
                {
                    TestOutcome.Passed => "✓",
                    TestOutcome.Failed => "✗",
                    _ => "!"
                };

                writer.WriteLine($"  {symbol} {result.TestName}");
                if (result.Outcome != TestOutcome.Passed && result.FailureReason is not null)
                {
                    writer.WriteLine($"      {result.FailureReason}");
                }
            }

            writer.WriteLine();
        }

        var passed = results.Count(r => r.Outcome == TestOutcome.Passed);
        var failed = results.Count(r => r.Outcome == TestOutcome.Failed);
        var errored = results.Count(r => r.Outcome == TestOutcome.Errored);

        writer.WriteLine($"Tests: {results.Count}");
        writer.WriteLine($"Passed: {passed}");
        writer.WriteLine($"Failed: {failed}");
        if (errored > 0)
        {
            writer.WriteLine($"Errored: {errored}");
        }
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
