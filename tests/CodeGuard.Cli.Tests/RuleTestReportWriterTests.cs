using CodeGuard.Cli.Support;

namespace CodeGuard.Cli.Tests;

public class RuleTestReportWriterTests
{
    [Fact]
    public void WriteConsole_AllCasesPassed_CollapsesToSingleLineWithoutPerCaseLines()
    {
        var results = new[]
        {
            new RuleTestCaseResult("RULE-001", "Case One", TestOutcome.Passed, null),
            new RuleTestCaseResult("RULE-001", "Case Two", TestOutcome.Passed, null)
        };

        var writer = new StringWriter();
        RuleTestReportWriter.WriteConsole(results, writer);
        var output = writer.ToString();

        Assert.Contains("✓ RULE-001  2/2 passed", output);
        Assert.DoesNotContain("Case One", output);
        Assert.DoesNotContain("Case Two", output);
    }

    [Fact]
    public void WriteConsole_MixedOutcomes_ListsOnlyNonPassingCases()
    {
        var results = new[]
        {
            new RuleTestCaseResult("RULE-002", "Passing Case", TestOutcome.Passed, null),
            new RuleTestCaseResult("RULE-002", "Failing Case", TestOutcome.Failed, "boom"),
            new RuleTestCaseResult("RULE-002", "Erroring Case", TestOutcome.Errored, "kaboom")
        };

        var writer = new StringWriter();
        RuleTestReportWriter.WriteConsole(results, writer);
        var output = writer.ToString();

        Assert.Contains("✗ RULE-002  1/3 passed", output);
        Assert.Contains("✗ Failing Case", output);
        Assert.Contains("boom", output);
        Assert.Contains("! Erroring Case", output);
        Assert.Contains("kaboom", output);
        Assert.DoesNotContain("Passing Case", output);
    }

    [Fact]
    public void WriteConsole_WithColorEnabled_WrapsOutcomeSymbolsInAnsiCodes()
    {
        var results = new[] { new RuleTestCaseResult("RULE-003", "Only Case", TestOutcome.Passed, null) };

        var writer = new StringWriter();
        RuleTestReportWriter.WriteConsole(results, writer, useColor: true);
        var output = writer.ToString();

        Assert.Contains("\x1b[32m✓\x1b[0m", output, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteConsole_WithColorDisabled_ContainsNoAnsiCodes()
    {
        var results = new[]
        {
            new RuleTestCaseResult("RULE-004", "Failing Case", TestOutcome.Failed, "boom")
        };

        var writer = new StringWriter();
        RuleTestReportWriter.WriteConsole(results, writer, useColor: false);
        var output = writer.ToString();

        Assert.DoesNotContain('\x1b', output);
    }

    [Fact]
    public void WriteConsole_Summary_IsSingleLineAndOmitsErroredWhenZero()
    {
        var results = new[]
        {
            new RuleTestCaseResult("RULE-005", "Case One", TestOutcome.Passed, null),
            new RuleTestCaseResult("RULE-005", "Case Two", TestOutcome.Failed, "boom")
        };

        var writer = new StringWriter();
        RuleTestReportWriter.WriteConsole(results, writer);
        var output = writer.ToString();

        Assert.Contains("Tests: 2  Passed: 1  Failed: 1", output);
        Assert.DoesNotContain("Errored:", output);
    }

    [Fact]
    public void WriteConsole_Summary_IncludesErroredWhenNonZero()
    {
        var results = new[]
        {
            new RuleTestCaseResult("RULE-006", "Case One", TestOutcome.Passed, null),
            new RuleTestCaseResult("RULE-006", "Case Two", TestOutcome.Errored, "kaboom")
        };

        var writer = new StringWriter();
        RuleTestReportWriter.WriteConsole(results, writer);
        var output = writer.ToString();

        Assert.Contains("Tests: 2  Passed: 1  Failed: 0  Errored: 1", output);
    }
}
