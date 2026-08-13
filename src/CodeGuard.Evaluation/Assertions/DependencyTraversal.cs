using CodeGuard.Analysis.AnalysisModel;

namespace CodeGuard.Evaluation.Assertions;

/// <summary>
/// Shared type-reference traversal used by the `must_not_depend_on`/`must_depend_on`/
/// `must_only_depend_on` assertion family. Walks every type name a <see cref="TypeModel"/>
/// references: base type, interfaces, type-level attributes, and the return/parameter/property/
/// field types (plus their attributes) of its members. This does not consume a precomputed
/// dependency graph - CodeGuard.Analysis's `DependencyEdge` type exists but is never populated
/// (see docs/REFACTORING.md's "Analysis Session" proposal for that, a separate, un-started
/// initiative) - so each assertion using this helper re-walks the raw model directly.
/// </summary>
internal static class DependencyTraversal
{
    public static IEnumerable<string> ReferencedTypeNames(TypeModel type)
    {
        if (type.BaseType is not null)
        {
            yield return type.BaseType;
        }

        foreach (var name in type.Interfaces)
        {
            yield return name;
        }

        foreach (var name in AttributeTypeNames(type.Attributes))
        {
            yield return name;
        }

        foreach (var method in type.Methods)
        {
            yield return method.ReturnType;

            foreach (var parameter in method.Parameters)
            {
                yield return parameter.Type;
            }

            foreach (var name in AttributeTypeNames(method.Attributes))
            {
                yield return name;
            }
        }

        foreach (var property in type.Properties)
        {
            yield return property.Type;

            foreach (var name in AttributeTypeNames(property.Attributes))
            {
                yield return name;
            }
        }

        foreach (var field in type.Fields)
        {
            yield return field.Type;

            foreach (var name in AttributeTypeNames(field.Attributes))
            {
                yield return name;
            }
        }

        foreach (var constructor in type.Constructors)
        {
            foreach (var parameter in constructor.Parameters)
            {
                yield return parameter.Type;
            }

            foreach (var name in AttributeTypeNames(constructor.Attributes))
            {
                yield return name;
            }
        }
    }

    public static IEnumerable<string> ReferencedTypeNames(ProjectModel project) =>
        project.Types.SelectMany(ReferencedTypeNames);

    private static IEnumerable<string> AttributeTypeNames(IReadOnlyList<AttributeModel> attributes) =>
        attributes.Select(a => a.TypeName);
}
