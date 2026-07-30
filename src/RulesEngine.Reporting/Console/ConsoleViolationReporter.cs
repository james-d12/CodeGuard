using RulesEngine.Core.Results;

namespace RulesEngine.Reporting.Console;

public sealed class ConsoleViolationReporter(bool useColor = false) : IViolationReporter
{
    public string Format => "console";

    public async Task WriteAsync(ValidationResult result, TextWriter writer, CancellationToken ct = default)
    {
        foreach (var violation in result.Violations.OrderByDescending(v => v.Severity))
        {
            var location = violation.File is null
                ? violation.Project ?? violation.Symbol ?? "<unknown>"
                : $"{violation.File}({violation.Line ?? 0},{violation.Column ?? 0})";

            var severityTag = $"[{violation.Severity}]";
            if (useColor)
            {
                severityTag = AnsiSeverityColors.Colorize(violation.Severity, severityTag);
            }

            await writer.WriteLineAsync($"{severityTag} {violation.RuleId}: {violation.Message} ({location})");

            if (violation.Remediation is not null)
            {
                await writer.WriteLineAsync($"    remediation: {violation.Remediation.Trim()}");
            }
        }

        await writer.WriteLineAsync();
        await writer.WriteLineAsync(
            $"Rules evaluated: {result.RulesEvaluated}, passed: {result.RulesPassed}, failed: {result.RulesFailed}");
        await writer.WriteLineAsync($"Status: {result.Status}");
    }
}
