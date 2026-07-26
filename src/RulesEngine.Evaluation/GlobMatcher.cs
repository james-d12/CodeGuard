using System.Text.RegularExpressions;

namespace RulesEngine.Evaluation;

internal static class GlobMatcher
{
    public static bool IsMatch(string value, string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(value, regexPattern);
    }
}
