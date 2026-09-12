using System.Text.Json;
using CodeGuard.Configuration.Analysis;

namespace CodeGuard.Cli.Support;

/// <summary>Renders a <see cref="RuleAnalysisReport"/> for `rules analyze`.</summary>
public static class RuleAnalysisReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void WriteConsole(RuleAnalysisReport report, TextWriter writer)
    {
        writer.WriteLine("CodeGuard Rule Analysis");
        writer.WriteLine();
        writer.WriteLine($"Rules:                    {report.RuleCount}");
        writer.WriteLine($"Invalid:                  {report.InvalidRules.Count}");
        writer.WriteLine($"Duplicate ids:            {report.DuplicateIds.Count}");
        writer.WriteLine($"Rules without tests:      {report.RulesWithoutTests.Count}");
        writer.WriteLine($"One-sided tests:          {report.OneSidedTestRules.Count}");
        writer.WriteLine($"Unreachable assertions:   {report.UnreachableRules.Count}");
        writer.WriteLine($"Exact-duplicate rules:    {report.ExactDuplicateRules.Count}");
        writer.WriteLine($"Disabled rules:           {report.DisabledRules.Count}");
        writer.WriteLine($"Illustrative rules:       {report.IllustrativeRules.Count}");

        WriteFileList(writer, "Invalid rules", report.InvalidRules.Select(i => i.SourceFile));
        WriteFileList(writer, "Duplicate ids", report.DuplicateIds.Select(i => i.SourceFile));
        WriteList(writer, "Rules without tests", report.RulesWithoutTests);
        WriteList(writer, "One-sided tests (missing a pass or fail case)", report.OneSidedTestRules);

        if (report.UnreachableRules.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("Unreachable assertions:");
            foreach (var issue in report.UnreachableRules)
            {
                writer.WriteLine($"  - {issue.RuleId}: '{issue.AssertionKind}' cannot apply to a '{issue.TargetKind}' target ({issue.SourceFile})");
            }
        }

        if (report.ExactDuplicateRules.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("Exact-duplicate rules:");
            foreach (var group in report.ExactDuplicateRules)
            {
                writer.WriteLine($"  - {string.Join(", ", group.RuleIds)}");
            }
        }
    }

    private static void WriteList(TextWriter writer, string title, IReadOnlyList<string> ruleIds)
    {
        if (ruleIds.Count == 0)
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine($"{title}:");
        foreach (var ruleId in ruleIds)
        {
            writer.WriteLine($"  - {ruleId}");
        }
    }

    private static void WriteFileList(TextWriter writer, string title, IEnumerable<string> sourceFiles)
    {
        var files = sourceFiles.ToList();
        if (files.Count == 0)
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine($"{title}:");
        foreach (var file in files)
        {
            writer.WriteLine($"  - {file}");
        }
    }

    public static void WriteJson(RuleAnalysisReport report, TextWriter writer) =>
        writer.WriteLine(JsonSerializer.Serialize(report, JsonOptions));
}
