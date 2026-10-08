namespace CodeGuard.Evaluation.Assertions;

/// <summary>
/// A rule's regex took longer than <see cref="AssertionRegex.MatchTimeout"/> to match - almost always
/// catastrophic backtracking in the pattern. Deliberately an exception rather than a violation: the
/// assertion couldn't decide pass/fail, so reporting either would be a false result.
/// </summary>
public sealed class RegexAssertionTimeoutException(string assertionKind, string pattern, TimeSpan timeout, Exception innerException)
    : TimeoutException(
        $"'{assertionKind}' regex '{pattern}' exceeded the {timeout.TotalMilliseconds}ms match timeout; " +
        "the pattern likely backtracks catastrophically (e.g. nested quantifiers like '(a+)+').",
        innerException)
{
    public string AssertionKind { get; } = assertionKind;

    public string Pattern { get; } = pattern;
}
