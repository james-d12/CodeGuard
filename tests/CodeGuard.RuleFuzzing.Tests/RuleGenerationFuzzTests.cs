using CodeGuard.Configuration.Parsing;
using CodeGuard.RuleFuzzing.Tests.Generation;
using CodeGuard.RuleFuzzing.Tests.Models;
using CodeGuard.RuleFuzzing.Tests.Oracles;
using CodeGuard.RuleModel.Rules;
using CsCheck;
using Xunit.Abstractions;

namespace CodeGuard.RuleFuzzing.Tests;

/// <summary>
/// The primary crash-fuzzing oracle: a compatible-pairing rule, generated from the engine's own
/// capability catalog, must never crash evaluation. See docs/RULE_FUZZING_PLAN.md.
/// </summary>
public sealed class RuleGenerationFuzzTests(ITestOutputHelper output)
{
    private static readonly PipelineHarness Harness = new();

    [Fact]
    public void CompatibleRules_ValidateSchema_ParseCleanly_AndNeverCrashEvaluation()
    {
        var gen = RuleDocumentGenerator.CompatibleRule(Harness.Catalog);
        var iterations = RuleFuzzOptions.Iterations("RULEFUZZ_ITERATIONS", 300);

        gen.Sample(document =>
        {
            // 1. Schema-validity: the generator only ever emits schema-legal shapes, so this must
            // never throw - a throw here is a generator/schema drift, not a tolerated case.
            Harness.ValidateSchema(document);

            // 2. Parseability: succeed, or fail with a documented, deliberately-manufactured
            // cross-parameter constraint (see KnownParameterConstraints). Anything else is a bug.
            RuleDefinition rule;
            try
            {
                rule = Harness.Parse(document);
            }
            catch (RuleParsingException ex) when (KnownParameterConstraints.IsTolerable(ex.Code))
            {
                return;
            }

            // 3. Zero-crash evaluation (primary oracle) + idempotence, against every representative model.
            foreach (var (name, model) in RepositoryModelFixtures.All)
            {
                var first = Harness.Evaluator.Evaluate([rule], model);
                var firstIntolerable = first.EvaluationErrors.Where(e => !KnownParameterConstraints.IsTolerableEvaluationError(e.ExceptionType)).ToList();
                Assert.True(
                    firstIntolerable.Count == 0,
                    $"Rule crashed evaluating against the '{name}' fixture: " +
                    string.Join("; ", firstIntolerable.Select(e => $"{e.ExceptionType}: {e.Message}")) +
                    $"\nDocument: {document.ToJsonString()}");

                if (first.EvaluationErrors.Count > 0)
                {
                    continue; // tolerated nested-template validation failure (see KnownParameterConstraints) - nothing further to compare
                }

                var second = Harness.Evaluator.Evaluate([rule], model);
                Assert.Equal(first.Violations.Count, second.Violations.Count);
                Assert.Equal(first.EvaluationErrors.Count, second.EvaluationErrors.Count);
            }
        }, writeLine: output.WriteLine, iter: iterations, threads: 1);
    }

    [Fact]
    public void AnalyzerRules_ValidateSchema_ParseCleanly_AndNeverCrashEvaluation()
    {
        var gen = RuleDocumentGenerator.AnalyzerRule(Harness.Catalog);
        var iterations = RuleFuzzOptions.Iterations("RULEFUZZ_ITERATIONS_ANALYZER", 150);

        gen.Sample(document =>
        {
            Harness.ValidateSchema(document);

            RuleDefinition rule;
            try
            {
                rule = Harness.Parse(document);
            }
            catch (RuleParsingException ex) when (KnownParameterConstraints.IsTolerable(ex.Code))
            {
                return;
            }

            foreach (var (name, model) in RepositoryModelFixtures.All)
            {
                var result = Harness.Evaluator.Evaluate([rule], model);
                var intolerable = result.EvaluationErrors.Where(e => !KnownParameterConstraints.IsTolerableEvaluationError(e.ExceptionType)).ToList();
                Assert.True(
                    intolerable.Count == 0,
                    $"Analyzer rule crashed evaluating against the '{name}' fixture: " +
                    string.Join("; ", intolerable.Select(e => $"{e.ExceptionType}: {e.Message}")) +
                    $"\nDocument: {document.ToJsonString()}");
            }
        }, writeLine: output.WriteLine, iter: iterations, threads: 1);
    }
}
