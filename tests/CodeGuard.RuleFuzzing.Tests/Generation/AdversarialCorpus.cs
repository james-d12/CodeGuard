namespace CodeGuard.RuleFuzzing.Tests.Generation;

/// <summary>
/// Fixed pools of adversarial parameter values, mixed into generation alongside benign ones (see
/// <see cref="RuleFuzzOptions.AdversarialValueWeight"/>).
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

    /// <summary>
    /// Syntactically valid regexes, including catastrophic-backtracking shapes - every assertion-level
    /// regex runs with a match timeout (<c>AssertionRegex</c>), so a pattern that blows up surfaces as a
    /// tolerated <c>RegexAssertionTimeoutException</c> evaluation error rather than hanging the run.
    /// </summary>
    public static readonly string[] Regexes =
    [
        "", ".*", "^$", "^Contoso\\..*$", "a|b|c", "[A-Za-z0-9_]+",
        "^" + new string('a', 500) + "$", "\\d{1,10}", "(foo|bar)?baz",
        "^(a+)+$", "(x+x+)+y", "^(\\w+\\s?)*$", "(.*a){20}"
    ];

    public static readonly string[] Strings =
    [
        "", new string('x', 10_000), "\"quoted\"", "line1\nline2", "back\\slash",
        "unicode-é中文", "  leading-trailing-space  ", "null", "true", "123"
    ];

    public static readonly int[] Ints = [0, -1, int.MinValue, int.MaxValue, 1_000_000, -1_000_000];

    public static readonly string[] StringListEdgeArrays = ["single"]; // empty array handled separately (see AssertionGenerator)
}
