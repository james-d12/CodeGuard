using CodeGuard.Configuration.Testing;
using CodeGuard.Core.Evaluation;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Cli.Support;

public enum TestOutcome
{
    Passed,
    Failed,
    Errored
}

public sealed record RuleTestCaseResult(string RuleId, string TestName, TestOutcome Outcome, string? FailureReason);

/// <summary>
/// Runs a rule's embedded `tests:` cases through the same <see cref="RuleEvaluator"/> used by real
/// repository validation - see docs/RULES_TEST_DESIGN.md ("Execution model"). One test case's setup
/// or evaluation blowing up must not abort the others, mirroring <see cref="RuleEvaluator.Evaluate"/>'s
/// own per-rule exception isolation.
/// </summary>
public static class RuleTestRunner
{
    public static IReadOnlyList<RuleTestCaseResult> Run(RuleDefinition rule)
    {
        var evaluator = new RuleEvaluator();
        return rule.Tests.Select(testCase => RunTestCase(evaluator, rule, testCase)).ToList();
    }

    private static RuleTestCaseResult RunTestCase(RuleEvaluator evaluator, RuleDefinition rule, RuleTestCase testCase)
    {
        try
        {
            var model = TestSetupBuilder.Build(testCase.Setup);

            // A selector-backed rule whose target matches nothing trivially produces no violations, so
            // an `expect: pass` case would report green having exercised none of the rule's assertions.
            // That is almost always a setup that doesn't describe what the rule actually selects, so
            // report it rather than letting it count as evidence. Analyzer-backed rules have no Target
            // and run against the whole model, so the check doesn't apply to them. An `expect: fail`
            // case needs no such guard - zero candidates means zero violations, which already fails.
            if (testCase.Expect == TestExpectation.Pass
                && rule.Target is not null
                && !rule.Target.SelectCandidates(model).Any())
            {
                return new RuleTestCaseResult(
                    rule.Id,
                    testCase.Name,
                    TestOutcome.Errored,
                    $"Vacuous test: target '{rule.Target.Kind}' matched no candidates in this setup, so the " +
                    "rule's assertions never ran. Describe state the target actually selects.");
            }

            var violations = evaluator.EvaluateRule(rule, model);
            var passed = testCase.Expect == TestExpectation.Pass
                ? violations.Count == 0
                : violations.Count > 0;

            var failureReason = passed
                ? null
                : testCase.Expect == TestExpectation.Pass
                    ? $"Expected no violations but got {violations.Count}: {string.Join("; ", violations.Select(v => v.Message))}"
                    : "Expected at least one violation but the rule passed.";

            return new RuleTestCaseResult(rule.Id, testCase.Name, passed ? TestOutcome.Passed : TestOutcome.Failed, failureReason);
        }
        catch (Exception ex)
        {
            return new RuleTestCaseResult(rule.Id, testCase.Name, TestOutcome.Errored, ex.Message);
        }
    }
}
