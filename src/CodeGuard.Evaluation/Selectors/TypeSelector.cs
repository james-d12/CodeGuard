using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Selectors;

public sealed class TypeSelector(string namespacePattern = "*") : ITargetSelector
{
    public string Kind => "type";

    public IEnumerable<object> SelectCandidates(RepositoryModel model) =>
        model.Solutions
            .SelectMany(solution => solution.Projects)
            .SelectMany(project => project.Types)
            .Where(type => GlobMatcher.IsMatch(type.Namespace, namespacePattern));
}
