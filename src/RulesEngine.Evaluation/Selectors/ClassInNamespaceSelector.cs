using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Evaluation.Selectors;

public sealed class ClassInNamespaceSelector(string namespacePattern) : ITargetSelector
{
    public string Kind => "class";

    public IEnumerable<object> SelectCandidates(RepositoryModel model) =>
        model.Solutions
            .SelectMany(solution => solution.Projects)
            .SelectMany(project => project.Types)
            .Where(type => type.Kind == TypeKind.Class && GlobMatcher.IsMatch(type.Namespace, namespacePattern));
}
