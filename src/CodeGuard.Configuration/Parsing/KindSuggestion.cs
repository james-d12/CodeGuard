namespace CodeGuard.Configuration.Parsing;

/// <summary>
/// Finds the registered kind closest to an unrecognised one, so "unknown kind 'must_inherits_from'"
/// can point at <c>must_inherit_from</c>. Mistyped and near-miss kinds are the most common way a
/// generated rule fails to parse, and the registry already knows every valid answer.
/// </summary>
internal static class KindSuggestion
{
    /// <summary>Appends " Did you mean 'x'?" when a close enough match exists, else returns "".</summary>
    public static string For(string unknownKind, IEnumerable<string> knownKinds)
    {
        var best = knownKinds
            .Select(kind => (Kind: kind, Distance: Distance(unknownKind, kind)))
            .OrderBy(candidate => candidate.Distance)
            .ThenBy(candidate => candidate.Kind, StringComparer.Ordinal)
            .FirstOrDefault();

        if (best.Kind is null)
        {
            return "";
        }

        // Scale the tolerance with length so short kinds don't match everything, but keep a floor so
        // a single typo in a short kind is still caught.
        var tolerance = Math.Max(2, unknownKind.Length / 3);
        return best.Distance <= tolerance ? $" Did you mean '{best.Kind}'?" : "";
    }

    private static int Distance(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];

        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var substitution = previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1);
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), substitution);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}
