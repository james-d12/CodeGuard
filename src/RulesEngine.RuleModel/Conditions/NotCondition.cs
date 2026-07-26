using RulesEngine.Analysis.AnalysisModel;

namespace RulesEngine.RuleModel.Conditions;

public sealed class NotCondition(IConditionNode child) : IConditionNode
{
    public bool Evaluate(object candidate, RepositoryModel model) => !child.Evaluate(candidate, model);
}
