using CodeGuard.Analysis.AnalysisModel;

namespace CodeGuard.RuleModel.Conditions;

public interface IConditionNode
{
    bool Evaluate(object candidate, RepositoryModel model);
}
