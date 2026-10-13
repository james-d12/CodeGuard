using System.Text.Json.Nodes;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Assertions;

/// <summary>
/// Universal quantifier: every match for a nested <c>selector:</c> (resolved against the outer
/// candidate via <see cref="SelectorTemplateResolver"/>, same as <see cref="MustHaveCountAssertion"/>)
/// must satisfy every nested <c>assertions:</c> entry (an implicit AND across the list, mirroring
/// how a rule's top-level <c>assertions:</c> is combined). Vacuously passes when the nested
/// selector produces no matches, per standard for-all semantics. This is the primitive PRIMITIVES.md
/// calls <c>MustHaveAll</c>/the <c>All</c> logical combinator - the one genuinely missing piece of
/// the And/Or/Not vocabulary, since those three combine conditions for a single candidate rather
/// than quantifying over a set. See also <see cref="MustAnyMatchAssertion"/>/<see cref="MustNoneMatchAssertion"/>.
/// </summary>
public sealed class MustAllMatchAssertion(
    JsonObject selectorTemplate,
    Func<JsonObject, ITargetSelector> selectorFactory,
    IReadOnlyList<IAssertion> assertions) : IAssertion
{
    public string Kind => "must_all_match";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        var selector = selectorFactory(SelectorTemplateResolver.Resolve(selectorTemplate, candidate));
        var failedMatches = selector.SelectCandidates(model)
            .Select(match => (Match: match, Failures: assertions
                .Select(assertion => assertion.Evaluate(match, model))
                .Where(outcome => !outcome.Passed)
                .ToList()))
            .Where(entry => entry.Failures.Count > 0)
            .ToList();

        if (failedMatches.Count == 0)
        {
            return AssertionOutcome.Success();
        }

        var failures = failedMatches
            .SelectMany(entry => entry.Failures.Select(outcome => $"{CandidateDescriptor.Describe(entry.Match)}: {outcome.Message}"))
            .ToList();

        // Evidence is per failing match: the match's description, qualified by each nested outcome's
        // own evidence when it has any, so the offending references stay traceable to their match.
        var evidence = new List<string>();
        foreach (var entry in failedMatches)
        {
            var description = CandidateDescriptor.Describe(entry.Match);
            var nested = entry.Failures.SelectMany(outcome => outcome.Evidence ?? []).ToList();
            if (nested.Count == 0)
            {
                evidence.Add(description);
            }
            else
            {
                evidence.AddRange(nested.Select(item => $"{description}: {item}"));
            }
        }

        var nestedFailures = failedMatches.SelectMany(entry => entry.Failures).ToList();

        return AssertionOutcome.Failure(
            $"Not every match for a '{selector.Kind}' selector satisfied every nested assertion: {string.Join("; ", failures)}",
            CandidateDescriptor.CommonValue(nestedFailures.Select(outcome => outcome.Expected)),
            $"{failedMatches.Count} match(es) failed a nested assertion",
            CandidateDescriptor.CapEvidence(evidence));
    }
}
