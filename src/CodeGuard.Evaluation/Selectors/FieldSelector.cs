using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Selectors;

public sealed class FieldSelector(
    string declaringTypePattern = "*",
    bool? isReadonly = null,
    bool? isStatic = null) : ITargetSelector
{
    public string Kind => "field";

    public IEnumerable<object> SelectCandidates(RepositoryModel model) =>
        model.Solutions
            .SelectMany(solution => solution.Projects)
            .SelectMany(project => project.Types)
            .SelectMany(type => type.Fields)
            .Where(field => GlobMatcher.IsMatch(field.DeclaringType, declaringTypePattern))
            .Where(field => isReadonly is null || field.Modifiers.HasFlag(FieldModifiers.Readonly) == isReadonly)
            .Where(field => isStatic is null || field.Modifiers.HasFlag(FieldModifiers.Static) == isStatic);
}
