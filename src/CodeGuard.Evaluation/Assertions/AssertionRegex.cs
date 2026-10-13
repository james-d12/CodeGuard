using System.Text.RegularExpressions;

namespace CodeGuard.Evaluation.Assertions;

/// <summary>
/// The single entry point for matching a rule-author-supplied regex. Every match runs with
/// <see cref="MatchTimeout"/> (like <see cref="GlobMatcher"/>), so a catastrophically backtracking
/// pattern fails that rule with a <see cref="RegexAssertionTimeoutException"/> - surfaced by
/// <c>RuleEvaluator</c> as a <c>RuleEvaluationError</c> - instead of hanging evaluation forever.
/// </summary>
internal static class AssertionRegex
{
    /// <summary>
    /// Generous compared to <see cref="GlobMatcher"/>'s 100ms because <c>must_match_content</c> and
    /// <c>must_not_match_content</c> match whole files, not short names.
    /// </summary>
    public static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    public static bool IsMatch(string input, string pattern, string assertionKind)
    {
        try
        {
            return Regex.IsMatch(input, pattern, RegexOptions.None, MatchTimeout);
        }
        catch (RegexMatchTimeoutException ex)
        {
            throw new RegexAssertionTimeoutException(assertionKind, pattern, MatchTimeout, ex);
        }
    }
}
