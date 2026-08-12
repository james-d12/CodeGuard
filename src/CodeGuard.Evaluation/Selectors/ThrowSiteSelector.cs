using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Selectors;

public sealed class ThrowSiteSelector(
    string exceptionTypePattern = "*",
    bool? isFirstStatementInMethod = null,
    string containingTypePattern = "*",
    string containingMethodPattern = "*",
    string projectPattern = "*") : ITargetSelector
{
    public string Kind => "throw_site";

    public IEnumerable<object> SelectCandidates(RepositoryModel model) =>
        model.ThrowSites
            .Where(t => exceptionTypePattern == "*" || GlobMatcher.IsMatch(t.ExceptionTypeName ?? string.Empty, exceptionTypePattern))
            .Where(t => isFirstStatementInMethod is null || t.IsFirstStatementInMethod == isFirstStatementInMethod)
            .Where(t => GlobMatcher.IsMatch(t.ContainingType, containingTypePattern))
            .Where(t => GlobMatcher.IsMatch(t.ContainingMethod, containingMethodPattern))
            .Where(t => GlobMatcher.IsMatch(t.ProjectName, projectPattern));
}
