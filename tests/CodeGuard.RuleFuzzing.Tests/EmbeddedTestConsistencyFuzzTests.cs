using System.Text.Json.Nodes;
using CodeGuard.Configuration.Parsing;
using CodeGuard.Configuration.Testing;
using CodeGuard.RuleFuzzing.Tests.Generation;
using CodeGuard.RuleFuzzing.Tests.Models;
using CodeGuard.RuleFuzzing.Tests.Oracles;
using CodeGuard.RuleModel.Rules;
using CsCheck;
using Xunit.Abstractions;

namespace CodeGuard.RuleFuzzing.Tests;

/// <summary>
/// A generated rule and one of the fixed virtual setups are independently evaluated once (via
/// <c>RuleEvaluator.EvaluateRule</c> directly) to observe the real pass/fail outcome, then attached
/// together as the rule's own embedded <c>tests:</c> case with <c>expect</c> set to match that observed
/// outcome. Re-parsing the whole document and running it through <c>RuleTestRunner.Run</c> - the exact
/// path <c>codeguard rules test</c> uses - must agree, since by construction the expectation is correct.
/// Vacuous cases (the target selects nothing) are skipped: <c>RuleTestRunner</c> deliberately reports
/// those as <c>Errored</c> rather than a plain pass/fail (see its own doc comment), which is a documented
/// difference in behavior, not a disagreement this oracle is checking for.
/// </summary>
public sealed class EmbeddedTestConsistencyFuzzTests(ITestOutputHelper output)
{
    private static readonly PipelineHarness Harness = new();

    [Fact]
    public void RuleTestRunner_AgreesWithDirectEvaluation_OnAJointlyConstructedCase()
    {
        var gen = RuleDocumentGenerator.CompatibleRule(Harness.Catalog)
            .Select(Gen.OneOfConst(RepositoryModelFixtures.RawSetups.ToArray()), (rule, setup) => (Rule: rule, Setup: setup));
        var iterations = RuleFuzzOptions.Iterations("RULEFUZZ_ITERATIONS_EMBEDDED_TEST", 200);

        gen.Sample(input =>
        {
            RuleDefinition rule;
            try
            {
                rule = Harness.Parse(input.Rule);
            }
            catch (RuleParsingException ex) when (KnownParameterConstraints.IsTolerable(ex.Code))
            {
                return;
            }

            if (rule.Target is null)
            {
                return; // analyzer-shaped rules never appear here (CompatibleRule only builds selector-shaped ones)
            }

            var model = TestSetupBuilder.Build((JsonObject)input.Setup.Setup.DeepClone());
            if (!rule.Target.SelectCandidates(model).Any())
            {
                return; // vacuous - RuleTestRunner's own guard reports this as Errored, not pass/fail
            }

            var directResult = Harness.Evaluator.Evaluate([rule], model);
            if (directResult.EvaluationErrors.Count > 0)
            {
                return; // covered by the other oracles; not what this one is checking
            }

            var expect = directResult.Violations.Count == 0 ? "pass" : "fail";
            var documentWithTest = (JsonObject)input.Rule.DeepClone();
            documentWithTest["tests"] = new JsonArray(new JsonObject
            {
                ["name"] = "fuzz-consistency",
                ["setup"] = (JsonObject)input.Setup.Setup.DeepClone(),
                ["expect"] = expect
            });

            var ruleWithTest = Harness.Parse(documentWithTest);
            var testResults = RuleTestRunner.Run(ruleWithTest);
            var result = Assert.Single(testResults);

            Assert.True(
                result.Outcome == TestOutcome.Passed,
                $"RuleTestRunner disagreed with direct evaluation (expected '{expect}', outcome " +
                $"{result.Outcome}: {result.FailureReason}).\nDocument: {documentWithTest.ToJsonString()}");
        }, writeLine: output.WriteLine, iter: iterations, threads: 1);
    }
}
