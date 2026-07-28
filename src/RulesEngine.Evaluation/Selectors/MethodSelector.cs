using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Evaluation.Selectors;

public sealed class MethodSelector(
    string namespacePattern = "*",
    string projectPattern = "*",
    string declaringTypePattern = "*",
    string namePattern = "*",
    Accessibility? accessibility = null,
    bool? isAsync = null,
    bool? isStatic = null) : ITargetSelector
{
    public string Kind => "method";

    public IEnumerable<object> SelectCandidates(RepositoryModel model) =>
        model.Solutions
            .SelectMany(solution => solution.Projects)
            .SelectMany(project => project.Types)
            .Where(type => GlobMatcher.IsMatch(type.Namespace, namespacePattern))
            .SelectMany(type => type.Methods)
            .Where(method => GlobMatcher.IsMatch(method.ProjectName, projectPattern))
            .Where(method => GlobMatcher.IsMatch(method.DeclaringType, declaringTypePattern))
            .Where(method => GlobMatcher.IsMatch(method.Name, namePattern))
            .Where(method => accessibility is null || method.Accessibility == accessibility)
            .Where(method => isAsync is null || method.Modifiers.HasFlag(MethodModifiers.Async) == isAsync)
            .Where(method => isStatic is null || method.Modifiers.HasFlag(MethodModifiers.Static) == isStatic)
            .Cast<object>();
}
