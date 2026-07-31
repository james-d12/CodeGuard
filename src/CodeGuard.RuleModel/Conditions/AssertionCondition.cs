using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.RuleModel.Conditions;

public sealed class AssertionCondition(IAssertion assertion) : IConditionNode
{
    public bool Evaluate(object candidate, RepositoryModel model) =>
        assertion.Evaluate(candidate, model).Passed;
}
