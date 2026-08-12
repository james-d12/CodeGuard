using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Selectors;

public sealed class TryBlockSelector(
    int? minCatchClauseCount = null,
    int? maxCatchClauseCount = null,
    string containingTypePattern = "*",
    string containingMethodPattern = "*",
    string projectPattern = "*") : ITargetSelector
{
    public string Kind => "try_block";

    public IEnumerable<object> SelectCandidates(RepositoryModel model) =>
        model.TryBlocks
            .Where(t => minCatchClauseCount is null || t.CatchClauseCount >= minCatchClauseCount)
            .Where(t => maxCatchClauseCount is null || t.CatchClauseCount <= maxCatchClauseCount)
            .Where(t => GlobMatcher.IsMatch(t.ContainingType, containingTypePattern))
            .Where(t => GlobMatcher.IsMatch(t.ContainingMethod, containingMethodPattern))
            .Where(t => GlobMatcher.IsMatch(t.ProjectName, projectPattern));
}
