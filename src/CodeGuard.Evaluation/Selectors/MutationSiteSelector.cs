using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Selectors;

public sealed class MutationSiteSelector(
    string targetMemberPattern = "*",
    string containingTypePattern = "*",
    string containingMethodPattern = "*",
    string projectPattern = "*") : ITargetSelector
{
    public string Kind => "mutation_site";

    public IEnumerable<object> SelectCandidates(RepositoryModel model) =>
        model.MutationSites
            .Where(m => GlobMatcher.IsMatch(m.TargetMemberName, targetMemberPattern))
            .Where(m => GlobMatcher.IsMatch(m.ContainingType, containingTypePattern))
            .Where(m => GlobMatcher.IsMatch(m.ContainingMethod, containingMethodPattern))
            .Where(m => GlobMatcher.IsMatch(m.ProjectName, projectPattern));
}
