using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Selectors;

public sealed class SwitchSelector(
    string containingTypePattern = "*",
    string containingMethodPattern = "*",
    string projectPattern = "*",
    bool? hasDefaultOrDiscardArm = null) : ITargetSelector
{
    public string Kind => "switch";

    public IEnumerable<object> SelectCandidates(RepositoryModel model) =>
        model.Switches
            .Where(s => GlobMatcher.IsMatch(s.ContainingType, containingTypePattern))
            .Where(s => GlobMatcher.IsMatch(s.ContainingMethod, containingMethodPattern))
            .Where(s => GlobMatcher.IsMatch(s.ProjectName, projectPattern))
            .Where(s => hasDefaultOrDiscardArm is null || s.HasDefaultOrDiscardArm == hasDefaultOrDiscardArm);
}
