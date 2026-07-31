using CodeGuard.Core.Results;

namespace CodeGuard.Reporting.Console;

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
            $"Rules evaluated: {result.RulesEvaluated}, passed: {result.RulesPassed}, failed: {result.RulesFailed}, errored: {result.RulesErrored}");
        await writer.WriteLineAsync($"Status: {result.Status}");

        if (result.EvaluationErrors.Count > 0)
        {
            await writer.WriteLineAsync();
            await writer.WriteLineAsync("Rules that could not be evaluated:");
            foreach (var error in result.EvaluationErrors.OrderBy(e => e.RuleId, StringComparer.Ordinal))
            {
                await writer.WriteLineAsync($"  {error.RuleId}: {error.ExceptionType}: {error.Message}");
            }
        }
    }
}
