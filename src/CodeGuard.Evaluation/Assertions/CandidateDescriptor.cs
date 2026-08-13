using CodeGuard.Analysis.AnalysisModel;

namespace CodeGuard.Evaluation.Assertions;

/// <summary>
/// Best-effort human-readable label for a candidate object, shared by
/// <see cref="MustAllMatchAssertion"/>/<see cref="MustAnyMatchAssertion"/>/
/// <see cref="MustNoneMatchAssertion"/> to identify which nested-selector match a failure message
/// is about. Falls back to <see cref="object.ToString"/> for candidate kinds without an obvious
/// name rather than failing outright - unlike e.g. <see cref="MustMatchNameAssertion"/>, a
/// quantifier assertion's own outcome doesn't depend on producing a name, so a generic fallback
/// is preferable to refusing to evaluate.
/// </summary>
internal static class CandidateDescriptor
{
    public static string Describe(object candidate) => candidate switch
    {
        TypeModel type => type.FullName,
        ProjectModel project => project.Name,
        MethodModel method => $"{method.DeclaringType}.{method.Name}",
        PropertyModel property => $"{property.DeclaringType}.{property.Name}",
        FieldModel field => $"{field.DeclaringType}.{field.Name}",
        ConstructorModel constructor => $"{constructor.DeclaringType}..ctor",
        FileModel file => file.RelativePath,
        _ => candidate.ToString() ?? candidate.GetType().Name
    };
}
