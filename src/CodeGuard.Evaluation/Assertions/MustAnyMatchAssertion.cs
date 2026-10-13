using System.Text.Json.Nodes;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Assertions;

/// <summary>
/// Existential quantifier: at least one match for a nested <c>selector:</c> must satisfy every
/// nested <c>assertions:</c> entry. Fails when the nested selector produces no matches at all, or
/// when none of its matches satisfy every nested assertion. See <see cref="MustAllMatchAssertion"/>
/// for the shared design notes.
/// </summary>
public sealed class MustAnyMatchAssertion(
    JsonObject selectorTemplate,
    Func<JsonObject, ITargetSelector> selectorFactory,
    IReadOnlyList<IAssertion> assertions) : IAssertion
{
    public string Kind => "must_any_match";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        var selector = selectorFactory(SelectorTemplateResolver.Resolve(selectorTemplate, candidate));
        var matches = selector.SelectCandidates(model).ToList();

        // Single pass: each (match, assertion) pair is evaluated at most once. Within a match, evaluation
        // stops at the first failing assertion (as assertions.All does), and the search stops at the first
        // fully satisfying match, since no failure data is needed then. The failure outcomes collected here
        // are the ones reported, so nothing is re-evaluated to build the failure message or evidence.
        var nestedFailures = new List<AssertionOutcome>(matches.Count);
        foreach (var match in matches)
        {
            var failure = FirstFailure(match, model);
            if (failure is null)
            {
                return AssertionOutcome.Success();
            }

            nestedFailures.Add(failure);
        }

        return AssertionOutcome.Failure(
            $"Expected at least one match for a '{selector.Kind}' selector to satisfy every nested assertion, but none of the {matches.Count} match(es) did.",
            CandidateDescriptor.CommonValue(nestedFailures.Select(outcome => outcome.Expected)),
            $"0 of {matches.Count} match(es) satisfied every nested assertion",
            matches.Count == 0 ? null : CandidateDescriptor.CapEvidence(matches.Select(CandidateDescriptor.Describe)));
    }

    /// <summary>Returns the first nested outcome that fails for <paramref name="match"/>, or null when all pass.</summary>
    private AssertionOutcome? FirstFailure(object match, RepositoryModel model)
    {
        foreach (var assertion in assertions)
        {
            var outcome = assertion.Evaluate(match, model);
            if (!outcome.Passed)
            {
                return outcome;
            }
        }

        return null;
    }
}
