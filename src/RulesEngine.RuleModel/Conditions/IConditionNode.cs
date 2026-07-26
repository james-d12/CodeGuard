using RulesEngine.Analysis.AnalysisModel;

namespace RulesEngine.RuleModel.Conditions;

public interface IConditionNode
{
    bool Evaluate(object candidate, RepositoryModel model);
}
