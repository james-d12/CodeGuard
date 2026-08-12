using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Selectors;

public sealed class DiagnosticSelector(
    string idPattern = "*",
    string projectPattern = "*") : ITargetSelector
{
    public string Kind => "diagnostic";

    public IEnumerable<object> SelectCandidates(RepositoryModel model) =>
        model.Diagnostics
            .Where(d => GlobMatcher.IsMatch(d.Id, idPattern))
            .Where(d => GlobMatcher.IsMatch(d.ProjectName, projectPattern));
}
