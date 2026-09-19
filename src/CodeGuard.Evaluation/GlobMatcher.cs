using System.Text;
using System.Text.RegularExpressions;

namespace CodeGuard.Evaluation;

/// <summary>
/// Glob matching used throughout the engine for name/path patterns (namespaces, base types,
/// project/package names, file and directory paths, ...). <c>*</c> matches within one path segment
/// (never crosses <c>/</c>); <c>**</c>, used as a whole segment (i.e. bounded by <c>/</c> or the
/// start/end of the pattern), matches zero or more full path segments; <c>?</c> matches exactly one
/// character (also never <c>/</c>). Everything else matches literally.
///
/// None of the non-path callers (namespace/base-type/project/package/method/attribute patterns)
/// ever pass a value containing <c>/</c>, so for them <c>*</c> and <c>**</c> behave identically to
/// the old blanket "match anything" <c>*</c> - this segment-aware behavior only changes matching for
/// the file/directory selectors and assertions, which are the only callers whose values are
/// <c>/</c>-delimited relative paths.
/// </summary>
internal static class GlobMatcher
{
    public static bool IsMatch(string value, string pattern) =>
        Regex.IsMatch(value, ToRegexPattern(pattern), options: RegexOptions.None, matchTimeout: TimeSpan.FromMilliseconds(100));

    private static string ToRegexPattern(string pattern)
    {
        var regex = new StringBuilder("^");
        var i = 0;

        while (i < pattern.Length)
        {
            if (IsDoubleStarSegment(pattern, i, out var consumed, out var expansion))
            {
                regex.Append(expansion);
                i += consumed;
                continue;
            }

            switch (pattern[i])
            {
                case '*':
                    regex.Append("[^/]*");
                    i++;
                    break;
                case '?':
                    regex.Append("[^/]");
                    i++;
                    break;
                default:
                    regex.Append(Regex.Escape(pattern[i].ToString()));
                    i++;
                    break;
            }
        }

        regex.Append('$');
        return regex.ToString();
    }

    /// <summary>
    /// Recognises <c>**</c> as a whole path segment at position <paramref name="i"/> - i.e. followed
    /// by <c>/</c> (matching zero or more segments, then that slash) or standing at the end of the
    /// pattern (matching zero or more segments, with no trailing slash required). A bare <c>**</c>
    /// not bounded this way (e.g. embedded in a literal like <c>foo**bar</c>) is deliberately not
    /// treated specially - it falls through to two ordinary single-segment <c>*</c> wildcards.
    /// </summary>
    private static bool IsDoubleStarSegment(string pattern, int i, out int consumed, out string expansion)
    {
        if (pattern[i] != '*' || i + 1 >= pattern.Length || pattern[i + 1] != '*')
        {
            consumed = 0;
            expansion = "";
            return false;
        }

        if (i + 2 < pattern.Length && pattern[i + 2] == '/')
        {
            consumed = 3;
            expansion = "(?:.*/)?";
            return true;
        }

        if (i + 2 == pattern.Length)
        {
            consumed = 2;
            expansion = ".*";
            return true;
        }

        consumed = 0;
        expansion = "";
        return false;
    }
}
