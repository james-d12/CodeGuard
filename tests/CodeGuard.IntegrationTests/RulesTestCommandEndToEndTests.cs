using System.Runtime.CompilerServices;
using CodeGuard.Cli.Support;
using CodeGuard.Configuration.Loading;

namespace CodeGuard.IntegrationTests;

public class RulesTestCommandEndToEndTests
{
    [Fact]
    public void RuleTestRunner_ForRuleWithPassAndFailCases_ReportsBothAsPassed()
    {
        var results = RunEmbeddedTests();

        Assert.Equal(2, results.Count);

        var passCase = Assert.Single(results, r => r.TestName == "Entity inheriting from Entity<TId>");
        Assert.Equal(TestOutcome.Passed, passCase.Outcome);
        Assert.Null(passCase.FailureReason);

        // The embedded case expects a violation (expect: fail) and gets one, so the test case
        // itself is reported as Passed - Outcome reflects whether the actual result matched the
        // case's expectation, not whether a violation occurred.
        var failCase = Assert.Single(results, r => r.TestName == "Entity not inheriting from Entity<TId>");
        Assert.Equal(TestOutcome.Passed, failCase.Outcome);
        Assert.Null(failCase.FailureReason);
    }

    [Fact]
    public void RuleTestReportWriter_ForRuleWithPassAndFailCases_SummarizesBothAsPassed()
    {
        var results = RunEmbeddedTests();

        var writer = new StringWriter();
        RuleTestReportWriter.WriteConsole(results, writer);
        var output = writer.ToString();

        Assert.Contains("✓ DDD-ENTITY-001", output);
        Assert.Contains("Tests: 2", output);
        Assert.Contains("Passed: 2", output);
        Assert.Contains("Failed: 0", output);
        Assert.DoesNotContain("Errored:", output);
    }

    private static IReadOnlyList<RuleTestCaseResult> RunEmbeddedTests()
    {
        var rules = RuleFileLoader.CreateDefault().LoadFromDirectory(GetExampleRulesDirectory());
        var rule = Assert.Single(rules, r => r.Id == "DDD-ENTITY-001");
        return RuleTestRunner.Run(rule);
    }

    private static string GetExampleRulesDirectory([CallerFilePath] string sourceFilePath = "") =>
        Path.Combine(Path.GetDirectoryName(sourceFilePath)!, "Fixtures", "ExampleRules");
}
