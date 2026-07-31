using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Selectors;

public sealed class PropertySelector(
    string namespacePattern = "*",
    string projectPattern = "*",
    string declaringTypePattern = "*",
    Accessibility? accessibility = null,
    bool? isStatic = null) : ITargetSelector
{
    public string Kind => "property";

    public IEnumerable<object> SelectCandidates(RepositoryModel model) =>
        model.Solutions
            .SelectMany(solution => solution.Projects)
            .SelectMany(project => project.Types)
            .Where(type => GlobMatcher.IsMatch(type.Namespace, namespacePattern))
            .SelectMany(type => type.Properties)
            .Where(property => GlobMatcher.IsMatch(property.ProjectName, projectPattern))
            .Where(property => GlobMatcher.IsMatch(property.DeclaringType, declaringTypePattern))
            .Where(property => accessibility is null || property.Accessibility == accessibility)
            .Where(property => isStatic is null || property.IsStatic == isStatic);
}
