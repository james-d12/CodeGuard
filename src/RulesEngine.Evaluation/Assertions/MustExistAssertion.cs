using System.Text.Json.Nodes;
using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Assertions;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Evaluation.Assertions;

public sealed class MustExistAssertion(JsonObject selectorTemplate, Func<JsonObject, ITargetSelector> selectorFactory) : IAssertion
{
    public string Kind => "must_exist";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        var selector = selectorFactory(SelectorTemplateResolver.Resolve(selectorTemplate, candidate));
        return selector.SelectCandidates(model).Any()
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"Expected at least one match for a '{selector.Kind}' selector, but found none.");
    }
}
