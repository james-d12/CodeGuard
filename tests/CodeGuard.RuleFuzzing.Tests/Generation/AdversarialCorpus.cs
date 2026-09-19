namespace CodeGuard.RuleFuzzing.Tests.Generation;

/// <summary>
/// Fixed pools of adversarial parameter values, mixed into generation alongside benign ones (see
/// <see cref="RuleFuzzOptions.AdversarialValueWeight"/>).
///
/// Deliberately excluded: catastrophic-backtracking regex shapes (e.g. "(a+)+$") as live parameter
/// values. Assertion-level regex parameters (`must_match_content`, `must_match_argument`,
/// `must_match_namespace_pattern`) are evaluated via <c>Regex.IsMatch(value, pattern)</c> with NO match
/// timeout (unlike <c>CodeGuard.Evaluation.GlobMatcher</c>, which explicitly passes a 100ms
/// `matchTimeout`) - see e.g. `src/CodeGuard.Evaluation/Assertions/MustMatchContentAssertion.cs`.
/// Feeding a genuinely catastrophic pattern through the real, non-cancellable evaluation pipeline in a
/// tight fuzz loop risks hanging the whole test run rather than failing it cleanly, since .NET's
/// `Regex.IsMatch` without a timeout cannot be interrupted from outside once backtracking blows up.
/// This is a real gap worth a follow-up engine fix (give those call sites the same timeout treatment
/// as `GlobMatcher`), tracked as a known finding rather than exercised here.
/// </summary>
internal static class AdversarialCorpus
{
    public static readonly string[] Globs =
    [
        "**", "**/**", "*", "?", "", "/", "//", "a//b", "/a/b/",
        new string('?', 50),
        "***", "?*?*?*?*?*",
        "a.(b)+[c]\\d", // regex metacharacters that are NOT glob metacharacters - must be Regex.Escape'd
        "Entity<*>", "**/Properties/launchSettings.json"
    ];

    /// <summary>Syntactically valid, cheap-to-evaluate regexes - see the type-level doc for why no
    /// catastrophic-backtracking shapes are included here.</summary>
    public static readonly string[] Regexes =
    [
        "", ".*", "^$", "^Contoso\\..*$", "a|b|c", "[A-Za-z0-9_]+",
        "^" + new string('a', 500) + "$", "\\d{1,10}", "(foo|bar)?baz"
    ];

    public static readonly string[] Strings =
    [
        "", new string('x', 10_000), "\"quoted\"", "line1\nline2", "back\\slash",
        "unicode-é中文", "  leading-trailing-space  ", "null", "true", "123"
    ];

    public static readonly int[] Ints = [0, -1, int.MinValue, int.MaxValue, 1_000_000, -1_000_000];

    public static readonly string[] StringListEdgeArrays = ["single"]; // empty array handled separately (see AssertionGenerator)
}
