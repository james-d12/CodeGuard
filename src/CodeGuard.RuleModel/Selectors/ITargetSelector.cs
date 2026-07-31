using CodeGuard.Analysis.AnalysisModel;

namespace CodeGuard.RuleModel.Selectors;

public interface ITargetSelector
{
    string Kind { get; }

    IEnumerable<object> SelectCandidates(RepositoryModel model);
}
