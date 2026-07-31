using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Selectors;

public sealed class RepositorySelector : ITargetSelector
{
    public string Kind => "repository";

    public IEnumerable<object> SelectCandidates(RepositoryModel model)
    {
        yield return model;
    }
}
