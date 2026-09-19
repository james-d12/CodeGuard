using System.Text.Json;
using CodeGuard.Configuration.Analysis;
using CodeGuard.Configuration.Sources;
using CodeGuard.Configuration.Validation;
using CodeGuard.Configuration.Versioning;

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
    /// <see cref="RuleSourceChecker"/> (always warnings, never reflected in <paramref name="report"/>'s
    /// own pass/fail counts or exit code - see docs/done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md), a
    /// "Version checks" section for any drift found by <see cref="RuleVersionChecker"/>, checked
    /// unconditionally for every rule with no opt-in (these ARE failures - see
    /// docs/RULE_VERSIONING_PLAN.md), and a "Rule analysis" section (rule-set-level findings from
    /// <see cref="RuleSetAnalyzer"/> - this used to be a separate `rules analyze` command; folded in
    /// here for the same reason source checks were: see docs/done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md).
    /// The source/version sections are omitted entirely when there's nothing to report; the analysis
    /// section is a dashboard shown whenever at least one rule parsed, always non-fatal.
    /// </summary>
    public static void WriteConsole(
        RuleSetValidationReport report, RuleSourceCheckReport sourceReport, RuleVersionCheckReport versionReport,
        RuleAnalysisReport analysisReport, TextWriter writer)
    {
        WriteConsole(report, writer);

        if (sourceReport.Issues.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("Source checks:");
            foreach (var issue in sourceReport.Issues.OrderBy(i => i.RuleId, StringComparer.Ordinal))
            {
                WriteSourceIssue(writer, issue);
            }
        }

        if (versionReport.Issues.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("Version checks:");
            foreach (var issue in versionReport.Issues.OrderBy(i => i.RuleId, StringComparer.Ordinal))
            {
                WriteVersionIssue(writer, issue);
            }
        }

        if (report.Rules.Count > 0)
        {
            WriteAnalysisSection(writer, analysisReport);
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
    public static void WriteJson(
        RuleSetValidationReport report, RuleSourceCheckReport sourceReport, RuleVersionCheckReport versionReport,
        RuleAnalysisReport analysisReport, TextWriter writer)
    {
        var summary = new RuleValidationSummaryWithSources(
            report.Rules.Count + report.Issues.Count,
            report.Rules.Count,
            report.IsValid && versionReport.IsValid,
            report.Issues.OrderBy(i => i.SourceFile, StringComparer.Ordinal).ToList(),
            sourceReport.Issues
                .OrderBy(i => i.RuleId, StringComparer.Ordinal)
                .Select(ToSourceCheckEntry)
                .ToList(),
            versionReport.Issues
                .OrderBy(i => i.RuleId, StringComparer.Ordinal)
                .Select(ToVersionCheckEntry)
                .ToList(),
            ToAnalysisSummary(analysisReport));

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

    private static void WriteVersionIssue(TextWriter writer, RuleVersionIssue issue)
    {
        switch (issue.Kind)
        {
            case RuleVersionIssueKind.FingerprintMissing:
                writer.WriteLine($"  ✗ {issue.RuleId} - no versionFingerprint captured yet");
                writer.WriteLine($"      New fingerprint: {issue.ComputedFingerprint}");
                return;
            case RuleVersionIssueKind.ContentChanged:
                writer.WriteLine($"  ✗ {issue.RuleId} - enforceable body changed since its recorded versionFingerprint");
                writer.WriteLine($"      Recorded fingerprint: {issue.RecordedFingerprint}");
                writer.WriteLine($"      New fingerprint:      {issue.ComputedFingerprint}");
                return;
        }
    }

    /// <summary>
    /// Unlike "Source checks:"/"Version checks:" (omitted when clean), this prints unconditionally
    /// whenever there's at least one parsed rule - it's a dashboard of counts every rule set has, not
    /// an opt-in or rare feature, matching what the standalone `rules analyze` command always printed.
    /// </summary>
    private static void WriteAnalysisSection(TextWriter writer, RuleAnalysisReport report)
    {
        writer.WriteLine();
        writer.WriteLine("Rule analysis:");
        writer.WriteLine($"  {"Rules:",-25}{report.RuleCount}");
        writer.WriteLine($"  {"Rules without tests:",-25}{report.RulesWithoutTests.Count}");
        writer.WriteLine($"  {"One-sided tests:",-25}{report.OneSidedTestRules.Count}");
        writer.WriteLine($"  {"Disabled rules:",-25}{report.DisabledRules.Count}");
        writer.WriteLine($"  {"Illustrative rules:",-25}{report.IllustrativeRules.Count}");
        writer.WriteLine($"  {"Missing provenance:",-25}{report.RulesMissingProvenance.Count}");
        writer.WriteLine($"  {"Unreachable assertions:",-25}{report.UnreachableRules.Count}");
        writer.WriteLine($"  {"Exact-duplicate rules:",-25}{report.ExactDuplicateRules.Count}");

        WriteAnalysisList(writer, "Rules without tests", report.RulesWithoutTests);
        WriteAnalysisList(writer, "One-sided tests (missing a pass or fail case)", report.OneSidedTestRules);
        WriteAnalysisList(writer, "Disabled rules", report.DisabledRules);
        WriteAnalysisList(writer, "Illustrative rules", report.IllustrativeRules);
        WriteAnalysisList(writer, "Missing provenance", report.RulesMissingProvenance);

        if (report.UnreachableRules.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("  Unreachable assertions:");
            foreach (var issue in report.UnreachableRules)
            {
                writer.WriteLine(
                    $"    - {issue.RuleId}: '{issue.AssertionKind}' cannot apply to a '{issue.TargetKind}' target ({issue.SourceFile})");
            }
        }

        if (report.ExactDuplicateRules.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("  Exact-duplicate rules:");
            foreach (var group in report.ExactDuplicateRules)
            {
                writer.WriteLine($"    - {string.Join(", ", group.RuleIds)}");
            }
        }
    }

    private static void WriteAnalysisList(TextWriter writer, string title, IReadOnlyList<string> ruleIds)
    {
        if (ruleIds.Count == 0)
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine($"  {title}:");
        foreach (var ruleId in ruleIds)
        {
            writer.WriteLine($"    - {ruleId}");
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

    private static VersionCheckEntry ToVersionCheckEntry(RuleVersionIssue issue) => new(
        issue.RuleId,
        JsonNamingPolicy.CamelCase.ConvertName(issue.Kind.ToString()),
        issue.RecordedFingerprint,
        issue.ComputedFingerprint);

    /// <summary>
    /// Deliberately drops <see cref="RuleAnalysisReport.InvalidRules"/>/<see cref="RuleAnalysisReport.DuplicateIds"/>
    /// - they're literally <c>report.Issues</c> split by <see cref="RuleErrorCodes.DuplicateRuleId"/>,
    /// i.e. the same entries already at the JSON's top-level <c>issues</c>. A consumer wanting just
    /// the duplicate-id ones can filter <c>issues</c> by that error code instead of seeing it twice.
    /// </summary>
    private static RuleAnalysisSummary ToAnalysisSummary(RuleAnalysisReport report) => new(
        report.RuleCount,
        report.RulesWithoutTests,
        report.OneSidedTestRules,
        report.DisabledRules,
        report.IllustrativeRules,
        report.RulesMissingProvenance,
        report.UnreachableRules,
        report.ExactDuplicateRules,
        report.HasFindings);

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
        IReadOnlyList<SourceCheckEntry> SourceChecks,
        IReadOnlyList<VersionCheckEntry> VersionChecks,
        RuleAnalysisSummary Analysis);

    private sealed record SourceCheckEntry(
        string RuleId,
        string Kind,
        string File,
        string? Section,
        string? RecordedStatement,
        string? CurrentContent,
        string? ComputedFingerprint,
        IReadOnlyList<int> AmbiguousHeadingLines);

    private sealed record VersionCheckEntry(string RuleId, string Kind, string? RecordedFingerprint, string ComputedFingerprint);

    /// <summary>
    /// `HasFindings` is purely informational here - see <see cref="RuleAnalysisReport.HasFindings"/>'s
    /// own doc comment. `rules validate`'s exit code never reads it; only schema/structural validity
    /// and version-fingerprint drift do (<see cref="ValidateCommand"/>).
    /// </summary>
    private sealed record RuleAnalysisSummary(
        int RuleCount,
        IReadOnlyList<string> RulesWithoutTests,
        IReadOnlyList<string> OneSidedTestRules,
        IReadOnlyList<string> DisabledRules,
        IReadOnlyList<string> IllustrativeRules,
        IReadOnlyList<string> RulesMissingProvenance,
        IReadOnlyList<UnreachableAssertionIssue> UnreachableRules,
        IReadOnlyList<ExactDuplicateGroup> ExactDuplicateRules,
        bool HasFindings);
}
