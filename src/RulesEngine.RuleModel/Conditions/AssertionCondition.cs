using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.RuleModel.Conditions;

public sealed class AssertionCondition(IAssertion assertion) : IConditionNode
{
    public bool Evaluate(object candidate, RepositoryModel model) =>
        assertion.Evaluate(candidate, model).Passed;
}
