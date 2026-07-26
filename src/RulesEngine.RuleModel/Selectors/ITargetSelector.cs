using RulesEngine.Analysis.AnalysisModel;

namespace RulesEngine.RuleModel.Selectors;

public interface ITargetSelector
{
    string Kind { get; }

    IEnumerable<object> SelectCandidates(RepositoryModel model);
}
