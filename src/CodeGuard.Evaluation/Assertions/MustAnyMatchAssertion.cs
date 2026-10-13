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
        var satisfied = matches.Any(match => assertions.All(assertion => assertion.Evaluate(match, model).Passed));

        if (satisfied)
        {
            return AssertionOutcome.Success();
        }

        var nestedFailures = matches
            .SelectMany(match => assertions.Select(assertion => assertion.Evaluate(match, model)))
            .Where(outcome => !outcome.Passed)
            .ToList();

        return AssertionOutcome.Failure(
            $"Expected at least one match for a '{selector.Kind}' selector to satisfy every nested assertion, but none of the {matches.Count} match(es) did.",
            CandidateDescriptor.CommonValue(nestedFailures.Select(outcome => outcome.Expected)),
            $"0 of {matches.Count} match(es) satisfied every nested assertion",
            matches.Count == 0 ? null : matches.Select(CandidateDescriptor.Describe).ToList());
    }
}
