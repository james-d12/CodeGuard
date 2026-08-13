namespace CodeGuard.Cli.Support;

/// <summary>ANSI colors for `rules test` console output symbols (✓/✗/!). Mirrors
/// CodeGuard.Reporting's AnsiSeverityColors, which is internal to that project and keyed by
/// Severity rather than TestOutcome, so isn't reusable here.</summary>
internal static class TestOutcomeColors
{
    private const string Reset = "\x1b[0m";

    public static string Colorize(TestOutcome outcome, string text) => $"{Code(outcome)}{text}{Reset}";

    private static string Code(TestOutcome outcome) => outcome switch
    {
        TestOutcome.Passed => "\x1b[32m",
        TestOutcome.Failed => "\x1b[31m",
        TestOutcome.Errored => "\x1b[33m",
        _ => Reset
    };
}
