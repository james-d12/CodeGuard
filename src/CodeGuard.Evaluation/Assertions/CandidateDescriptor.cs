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
        DirectoryModel directory => directory.RelativePath,
        _ => candidate.ToString() ?? candidate.GetType().Name
    };

    /// <summary>
    /// Upper bound on the number of entries in any Evidence (or evidence-like Actual) list, so a
    /// failure over thousands of matches stays a bounded payload for reporters and agents.
    /// </summary>
    public const int MaxEvidenceEntries = 50;

    /// <summary>
    /// Returns at most <paramref name="max"/> items. When there are more, the final entry is
    /// "...and N more", where N is the number of items omitted.
    /// </summary>
    public static List<string> CapEvidence(IEnumerable<string> items, int max = MaxEvidenceEntries)
    {
        var all = items.ToList();
        if (all.Count <= max)
        {
            return all;
        }

        var capped = all.Take(max).ToList();
        capped.Add($"...and {all.Count - max} more");
        return capped;
    }

    /// <summary>Joins names for an Actual value, or "(none)" when empty. Bounded like evidence.</summary>
    public static string ListOrNone(IEnumerable<string> items)
    {
        var list = CapEvidence(items);
        return list.Count == 0 ? "(none)" : string.Join(", ", list);
    }

    /// <summary>
    /// Aggregates nested outcome values for a quantifier: returns the value only when every nested
    /// outcome agrees on it (including agreeing that it is null), otherwise null.
    /// </summary>
    public static string? CommonValue(IEnumerable<string?> values)
    {
        var distinct = values.Distinct().ToList();
        return distinct.Count == 1 ? distinct[0] : null;
    }
}
