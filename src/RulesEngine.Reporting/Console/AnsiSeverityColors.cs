using RulesEngine.RuleModel.Rules;

namespace RulesEngine.Reporting.Console;

internal static class AnsiSeverityColors
{
    private const string Reset = "[0m";

    public static string Colorize(Severity severity, string text) => $"{Code(severity)}{text}{Reset}";

    private static string Code(Severity severity) => severity switch
    {
        Severity.Info => "[36m",
        Severity.Warning => "[33m",
        Severity.Error => "[31m",
        Severity.Critical => "[1;95m",
        _ => Reset
    };
}
