using System.Text.RegularExpressions;
using CsCheck;
using Xunit.Abstractions;

namespace CodeGuard.Evaluation.Tests;

/// <summary>
/// Fuzzes <c>GlobMatcher.IsMatch</c> directly with adversarial glob patterns and values - it's
/// <c>internal</c>, visible here via <c>[InternalsVisibleTo("CodeGuard.Evaluation.Tests")]</c>, so this
/// is the only place able to call it without going through the full rule pipeline. The invariant: it
/// must never throw anything other than <see cref="RegexMatchTimeoutException"/> (the documented,
/// deliberate outcome of its 100ms match timeout on a pathological pattern - e.g. many adjacent
/// unbounded <c>*</c>s create the same kind of backtracking ambiguity as a catastrophic regex). Any
/// other exception (an unescaped-metacharacter regex syntax error, an index error, ...) would be a bug
/// in <c>GlobMatcher.ToRegexPattern</c>, which is supposed to make every input safe to compile.
/// </summary>
public sealed class GlobMatcherFuzzTests(ITestOutputHelper output)
{
    private static readonly string[] AdversarialPatterns =
    [
        "**", "**/**", "*", "?", "", "/", "//", "a//b", "/a/b/",
        new string('?', 50), new string('*', 50), "***", "?*?*?*?*?*",
        "a.(b)+[c]\\d", "Entity<*>", "**/Properties/launchSettings.json",
        new string('*', 30) + "x" // many unbounded stars followed by a non-matching literal
    ];

    private static readonly string[] AdversarialValues =
    [
        "", "/", "a/b/c", new string('a', 500), "a/" + new string('b', 200),
        "unicode-é中文", "back\\slash", "line1\nline2"
    ];

    private static readonly Gen<(string Value, string Pattern)> Inputs =
        Gen.OneOfConst(AdversarialValues).Select(Gen.OneOfConst(AdversarialPatterns), (v, p) => (v, p));

    [Fact]
    public void IsMatch_OnAdversarialInputs_NeverThrowsExceptRegexTimeout()
    {
        Inputs.Sample(input =>
        {
            try
            {
                GlobMatcher.IsMatch(input.Value, input.Pattern);
            }
            catch (RegexMatchTimeoutException)
            {
                // Documented, deliberate outcome of the 100ms match timeout on a pathological pattern.
            }
        }, writeLine: output.WriteLine, iter: 5000, threads: 1);
    }
}
