using System.Text.Json.Nodes;
using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Assertions;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Evaluation.Assertions;

public sealed class MustNotExistAssertion(JsonObject selectorTemplate, Func<JsonObject, ITargetSelector> selectorFactory) : IAssertion
{
    public string Kind => "must_not_exist";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        var selector = selectorFactory(SelectorTemplateResolver.Resolve(selectorTemplate, candidate));
        return selector.SelectCandidates(model).Any()
            ? AssertionOutcome.Failure($"Expected no matches for a '{selector.Kind}' selector, but found at least one.")
            : AssertionOutcome.Success();
    }
}
