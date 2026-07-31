using CodeGuard.Analysis.AnalysisModel;

namespace CodeGuard.RuleModel.Conditions;

public sealed class OrCondition(IReadOnlyList<IConditionNode> children) : IConditionNode
{
    public bool Evaluate(object candidate, RepositoryModel model) =>
        children.Any(child => child.Evaluate(candidate, model));
}
