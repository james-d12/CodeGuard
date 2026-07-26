using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Evaluation.Selectors;

public sealed class RepositorySelector : ITargetSelector
{
    public string Kind => "repository";

    public IEnumerable<object> SelectCandidates(RepositoryModel model)
    {
        yield return model;
    }
}
