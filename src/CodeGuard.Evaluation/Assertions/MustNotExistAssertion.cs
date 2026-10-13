using System.Text.Json.Nodes;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustNotExistAssertion(JsonObject selectorTemplate, Func<JsonObject, ITargetSelector> selectorFactory) : IAssertion
{
    public string Kind => "must_not_exist";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        var selector = selectorFactory(SelectorTemplateResolver.Resolve(selectorTemplate, candidate));
        var matches = selector.SelectCandidates(model).ToList();

        return matches.Count > 0
            ? AssertionOutcome.Failure(
                $"Expected no matches for a '{selector.Kind}' selector, but found at least one.",
                "no matches",
                $"{matches.Count} match(es)",
                matches.Select(CandidateDescriptor.Describe).ToList())
            : AssertionOutcome.Success();
    }
}
