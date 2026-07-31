using CodeGuard.Analysis.AnalysisModel;

namespace CodeGuard.RuleModel.Conditions;

public sealed class NotCondition(IConditionNode child) : IConditionNode
{
    public bool Evaluate(object candidate, RepositoryModel model) => !child.Evaluate(candidate, model);
}
