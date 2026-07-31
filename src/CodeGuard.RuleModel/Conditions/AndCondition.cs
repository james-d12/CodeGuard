using CodeGuard.Analysis.AnalysisModel;

namespace CodeGuard.RuleModel.Conditions;

public sealed class AndCondition(IReadOnlyList<IConditionNode> children) : IConditionNode
{
    public bool Evaluate(object candidate, RepositoryModel model) =>
        children.All(child => child.Evaluate(candidate, model));
}
