using CodeGuard.Configuration.Parsing;
using CodeGuard.RuleFuzzing.Tests.Generation;
using CodeGuard.RuleFuzzing.Tests.Models;
using CodeGuard.RuleFuzzing.Tests.Oracles;
using CodeGuard.RuleModel.Rules;
using CsCheck;
using Xunit.Abstractions;

namespace CodeGuard.RuleFuzzing.Tests;

/// <summary>
/// Static/dynamic agreement oracle: a rule whose top-level assertion is guaranteed to statically
/// mismatch its selector (<see cref="RuleDocumentGenerator.IncompatibleRule"/>) must be flagged by
/// <c>RuleSetAnalyzer.UnreachableRules</c>, and must dynamically violate on every applicable candidate.
/// A rule whose mismatch is nested inside a quantifier assertion
/// (<see cref="RuleDocumentGenerator.NestedIncompatibleRule"/>) must be flagged too, since
/// <c>RuleSetAnalyzer.FindUnreachableAssertions</c> checks nested assertions against their nested selector.
/// </summary>
public sealed class UnreachableRuleCrossCheckFuzzTests(ITestOutputHelper output)
{
    private static readonly PipelineHarness Harness = new();

    [Fact]
    public void TopLevelMismatch_IsFlaggedByAnalyzer_AndAlwaysViolatesDynamically()
    {
        var gen = RuleDocumentGenerator.IncompatibleRule(Harness.Catalog);
        var iterations = RuleFuzzOptions.Iterations("RULEFUZZ_ITERATIONS_UNREACHABLE", 3000);

        gen.Sample(document =>
        {
            RuleDefinition rule;
            try
            {
                rule = Harness.Parse(document);
            }
            catch (RuleParsingException ex) when (KnownParameterConstraints.IsTolerable(ex.Code))
            {
                return;
            }

            var report = Harness.AnalyzeSingleRule(rule, document);
            Assert.True(
                report.UnreachableRules.Any(issue => issue.RuleId == rule.Id),
                $"Expected '{rule.Id}' to be flagged as unreachable by RuleSetAnalyzer.\nDocument: {document.ToJsonString()}");

            foreach (var (name, model) in RepositoryModelFixtures.All)
            {
                var result = Harness.Evaluator.Evaluate([rule], model);
                var intolerable = result.EvaluationErrors.Where(e => !KnownParameterConstraints.IsTolerableEvaluationError(e.ExceptionType)).ToList();
                Assert.True(
                    intolerable.Count == 0,
                    $"Mismatched rule crashed evaluating against '{name}': " +
                    string.Join("; ", intolerable.Select(e => $"{e.ExceptionType}: {e.Message}")) +
                    $"\nDocument: {document.ToJsonString()}");

                if (result.EvaluationErrors.Count > 0 || rule.Target is null)
                {
                    continue;
                }

                var candidateCount = rule.Target.SelectCandidates(model).Count();
                Assert.True(
                    candidateCount == 0 || result.Violations.Count == candidateCount,
                    $"Expected every one of {candidateCount} candidate(s) in '{name}' to violate a statically-mismatched " +
                    $"assertion, but got {result.Violations.Count} violation(s).\nDocument: {document.ToJsonString()}");
            }
        }, writeLine: output.WriteLine, iter: iterations, threads: 1);
    }

    [Fact]
    public void NestedMismatch_IsFlaggedByAnalyzer_AndNeverCrashesEvaluation()
    {
        var gen = RuleDocumentGenerator.NestedIncompatibleRule(Harness.Catalog);
        var iterations = RuleFuzzOptions.Iterations("RULEFUZZ_ITERATIONS_NESTED_UNREACHABLE", 3000);

        gen.Sample(document =>
        {
            RuleDefinition rule;
            try
            {
                rule = Harness.Parse(document);
            }
            catch (RuleParsingException ex) when (KnownParameterConstraints.IsTolerable(ex.Code))
            {
                return;
            }

            var report = Harness.AnalyzeSingleRule(rule, document);
            Assert.True(
                report.UnreachableRules.Any(issue => issue.RuleId == rule.Id && issue.Location.StartsWith("assertions[0].assertions[", StringComparison.Ordinal)),
                $"Expected '{rule.Id}' to have its nested mismatch flagged by RuleSetAnalyzer.\nDocument: {document.ToJsonString()}");

            foreach (var (name, model) in RepositoryModelFixtures.All)
            {
                var result = Harness.Evaluator.Evaluate([rule], model);
                var intolerable = result.EvaluationErrors.Where(e => !KnownParameterConstraints.IsTolerableEvaluationError(e.ExceptionType)).ToList();
                Assert.True(
                    intolerable.Count == 0,
                    $"Nested-mismatch rule crashed evaluating against '{name}': " +
                    string.Join("; ", intolerable.Select(e => $"{e.ExceptionType}: {e.Message}")) +
                    $"\nDocument: {document.ToJsonString()}");
            }
        }, writeLine: output.WriteLine, iter: iterations, threads: 1);
    }
}
