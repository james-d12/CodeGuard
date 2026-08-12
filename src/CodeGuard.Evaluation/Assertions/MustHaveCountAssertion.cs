using System.Text.Json.Nodes;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Assertions;

/// <summary>
/// Generalizes <see cref="MustExistAssertion"/>/<see cref="MustNotExistAssertion"/> from existence
/// to counting: asserts that the number of matches for a nested <c>selector:</c> (resolved against
/// the outer candidate the same way <c>must_exist</c> does, via <see cref="SelectorTemplateResolver"/>)
/// falls within <see cref="min"/>/<see cref="max"/>, or equals <see cref="exactly"/>. <c>min: 1</c> is
/// equivalent to <c>must_exist</c>; <c>exactly: 0</c> is equivalent to <c>must_not_exist</c> - those
/// two remain as the simpler, more readable form for pure existence checks.
/// </summary>
public sealed class MustHaveCountAssertion(
    JsonObject selectorTemplate,
    Func<JsonObject, ITargetSelector> selectorFactory,
    int? min,
    int? max,
    int? exactly) : IAssertion
{
    public string Kind => "must_have_count";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        var selector = selectorFactory(SelectorTemplateResolver.Resolve(selectorTemplate, candidate));
        var count = selector.SelectCandidates(model).Count();

        if (exactly is not null && count != exactly)
        {
            return AssertionOutcome.Failure(
                $"Expected exactly {exactly} match(es) for a '{selector.Kind}' selector, but found {count}.");
        }

        if (min is not null && count < min)
        {
            return AssertionOutcome.Failure(
                $"Expected at least {min} match(es) for a '{selector.Kind}' selector, but found {count}.");
        }

        if (max is not null && count > max)
        {
            return AssertionOutcome.Failure(
                $"Expected at most {max} match(es) for a '{selector.Kind}' selector, but found {count}.");
        }

        return AssertionOutcome.Success();
    }
}
