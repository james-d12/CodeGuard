using System.Text.Json.Nodes;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Assertions;

/// <summary>
/// Negative existential quantifier: no match for a nested <c>selector:</c> may satisfy every
/// nested <c>assertions:</c> entry (equivalently, every match must fail at least one nested
/// assertion). Vacuously passes when the nested selector produces no matches. See
/// <see cref="MustAllMatchAssertion"/> for the shared design notes.
/// </summary>
public sealed class MustNoneMatchAssertion(
    JsonObject selectorTemplate,
    Func<JsonObject, ITargetSelector> selectorFactory,
    IReadOnlyList<IAssertion> assertions) : IAssertion
{
    public string Kind => "must_none_match";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        var selector = selectorFactory(SelectorTemplateResolver.Resolve(selectorTemplate, candidate));
        var offenders = selector.SelectCandidates(model)
            .Where(match => assertions.All(assertion => assertion.Evaluate(match, model).Passed))
            .Select(CandidateDescriptor.Describe)
            .ToList();

        return offenders.Count == 0
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure(
                $"Expected no match for a '{selector.Kind}' selector to satisfy every nested assertion, but {offenders.Count} did: {string.Join(", ", offenders)}.");
    }
}
