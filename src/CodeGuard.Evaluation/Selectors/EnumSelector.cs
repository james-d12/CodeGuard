using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Selectors;

public sealed class EnumSelector(string namespacePattern = "*") : ITargetSelector
{
    public string Kind => "enum";

    public IEnumerable<object> SelectCandidates(RepositoryModel model) =>
        model.Solutions
            .SelectMany(solution => solution.Projects)
            .SelectMany(project => project.Types)
            .Where(type => type.Kind == TypeKind.Enum && GlobMatcher.IsMatch(type.Namespace, namespacePattern));
}
