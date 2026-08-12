using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Selectors;

public sealed class MethodBodyShapeSelector(
    int? minStatementCount = null,
    int? maxStatementCount = null,
    bool? isSingleBaseCallDelegation = null,
    string containingTypePattern = "*",
    string containingMethodPattern = "*",
    string projectPattern = "*") : ITargetSelector
{
    public string Kind => "method_body_shape";

    public IEnumerable<object> SelectCandidates(RepositoryModel model) =>
        model.MethodBodyShapes
            .Where(m => minStatementCount is null || m.StatementCount >= minStatementCount)
            .Where(m => maxStatementCount is null || m.StatementCount <= maxStatementCount)
            .Where(m => isSingleBaseCallDelegation is null || m.IsSingleBaseCallDelegation == isSingleBaseCallDelegation)
            .Where(m => GlobMatcher.IsMatch(m.ContainingType, containingTypePattern))
            .Where(m => GlobMatcher.IsMatch(m.ContainingMethod, containingMethodPattern))
            .Where(m => GlobMatcher.IsMatch(m.ProjectName, projectPattern));
}
