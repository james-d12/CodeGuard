using System.Text.Json;
using CodeGuard.Configuration.Sources;
using CodeGuard.Configuration.Validation;

namespace CodeGuard.Cli.Support;

/// <summary>Renders a <see cref="RuleSetValidationReport"/>, shared by `rules validate` and `validate`'s pre-flight gate.</summary>
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
                // Console output stays prose-first: the code is useful to a machine, the message to a
                // person, so lead with the message and append the path where there is one.
                writer.WriteLine($"  - {error}");
            }
        }
    }
    
    /// <summary>
    /// `rules validate` only - prints everything <see cref="WriteConsole(RuleSetValidationReport,TextWriter)"/>
    /// does, plus a "Source checks" section for any `metadata.source.file` drift found by
    /// <see cref="RuleSourceChecker"/>. See docs/done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md - these are
    /// always warnings, never reflected in <paramref name="report"/>'s own pass/fail counts or exit
    /// code, and the section is omitted entirely when there's nothing to report (the common case,
    /// since the feature is opt-in per rule).
    /// </summary>
    public static void WriteConsole(RuleSetValidationReport report, RuleSourceCheckReport sourceReport, TextWriter writer)
    {
        WriteConsole(report, writer);

        if (sourceReport.Issues.Count == 0)
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine("Source checks:");
        foreach (var issue in sourceReport.Issues.OrderBy(i => i.RuleId, StringComparer.Ordinal))
        {
            WriteSourceIssue(writer, issue);
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
    
    /// <summary>`rules validate` only - see the console overload's remarks.</summary>
    public static void WriteJson(RuleSetValidationReport report, RuleSourceCheckReport sourceReport, TextWriter writer)
    {
        var summary = new RuleValidationSummaryWithSources(
            report.Rules.Count + report.Issues.Count,
            report.Rules.Count,
            report.IsValid,
            report.Issues.OrderBy(i => i.SourceFile, StringComparer.Ordinal).ToList(),
            sourceReport.Issues
                .OrderBy(i => i.RuleId, StringComparer.Ordinal)
                .Select(ToSourceCheckEntry)
                .ToList());

        writer.WriteLine(JsonSerializer.Serialize(summary, JsonOptions));
    }

    private static void WriteSourceIssue(TextWriter writer, RuleSourceIssue issue)
    {
        var location = issue.Section is null ? issue.File : $"{issue.File} § {issue.Section}";

        switch (issue.Kind)
        {
            case RuleSourceIssueKind.FileMissing:
                writer.WriteLine($"  ✗ {issue.RuleId} - source document no longer exists ({issue.File})");
                return;
            case RuleSourceIssueKind.SectionNotFound:
                writer.WriteLine($"  ✗ {issue.RuleId} - source section \"{issue.Section}\" not found in {issue.File}");
                return;
            case RuleSourceIssueKind.SectionAmbiguous:
                writer.WriteLine(
                    $"  ✗ {issue.RuleId} - source section \"{issue.Section}\" matches multiple headings in " +
                    $"{issue.File} (lines {string.Join(", ", issue.AmbiguousHeadingLines)})");
                return;
            case RuleSourceIssueKind.ContentChanged:
                writer.WriteLine($"  ⚠ {issue.RuleId} - source content changed ({location})");
                if (issue.RecordedStatement is not null)
                {
                    writer.WriteLine($"      Recorded statement: {issue.RecordedStatement}");
                }

                writer.WriteLine($"      Current content:    {Summarize(issue.CurrentContent)}");
                writer.WriteLine($"      New fingerprint:    {issue.ComputedFingerprint}");
                return;
            case RuleSourceIssueKind.FingerprintMissing:
                writer.WriteLine($"  • {issue.RuleId} - fingerprint not yet captured ({location})");
                writer.WriteLine($"      New fingerprint:    {issue.ComputedFingerprint}");
                return;
        }
    }

    private static string Summarize(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return "(empty)";
        }

        var collapsed = string.Join(" ", content.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
        return collapsed.Length > 160 ? collapsed[..160] + "…" : collapsed;
    }

    private static SourceCheckEntry ToSourceCheckEntry(RuleSourceIssue issue) => new(
        issue.RuleId,
        JsonNamingPolicy.CamelCase.ConvertName(issue.Kind.ToString()),
        issue.File,
        issue.Section,
        issue.RecordedStatement,
        issue.CurrentContent,
        issue.ComputedFingerprint,
        issue.AmbiguousHeadingLines);

    private sealed record RuleValidationSummary(
        int FilesChecked,
        int FilesPassed,
        bool IsValid,
        IReadOnlyList<RuleFileIssue> Issues);

    private sealed record RuleValidationSummaryWithSources(
        int FilesChecked,
        int FilesPassed,
        bool IsValid,
        IReadOnlyList<RuleFileIssue> Issues,
        IReadOnlyList<SourceCheckEntry> SourceChecks);

    private sealed record SourceCheckEntry(
        string RuleId,
        string Kind,
        string File,
        string? Section,
        string? RecordedStatement,
        string? CurrentContent,
        string? ComputedFingerprint,
        IReadOnlyList<int> AmbiguousHeadingLines);
}
