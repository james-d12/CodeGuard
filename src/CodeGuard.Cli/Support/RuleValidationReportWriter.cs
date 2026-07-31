using System.Text.Json;
using CodeGuard.Configuration.Validation;

namespace CodeGuard.Cli.Support;

/// <summary>Renders a <see cref="RuleSetValidationReport"/>, shared by `check-rules` and `validate`'s pre-flight gate.</summary>
public static class RuleValidationReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void WriteConsole(RuleSetValidationReport report, TextWriter writer)
    {
        var total = report.Rules.Count + report.Issues.Count;
        writer.WriteLine($"Checked {total} rule file{(total == 1 ? "" : "s")}: {report.Rules.Count} passed, {report.Issues.Count} failed.");

        if (report.Issues.Count == 0)
        {
            return;
        }

        writer.WriteLine();
        foreach (var issue in report.Issues.OrderBy(i => i.SourceFile, StringComparer.Ordinal))
        {
            writer.WriteLine(issue.SourceFile);
            foreach (var error in issue.Errors)
            {
                writer.WriteLine($"  - {error}");
            }
        }
    }

    public static void WriteJson(RuleSetValidationReport report, TextWriter writer)
    {
        var summary = new RuleValidationSummary(
            report.Rules.Count + report.Issues.Count,
            report.Rules.Count,
            report.IsValid,
            report.Issues.OrderBy(i => i.SourceFile, StringComparer.Ordinal).ToList());

        writer.WriteLine(JsonSerializer.Serialize(summary, JsonOptions));
    }

    private sealed record RuleValidationSummary(
        int FilesChecked,
        int FilesPassed,
        bool IsValid,
        IReadOnlyList<RuleFileIssue> Issues);
}
