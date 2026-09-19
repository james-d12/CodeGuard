using System.CommandLine;
using CodeGuard.Cli.Support;
using CodeGuard.Configuration.Sources;
using Microsoft.Extensions.Logging;

namespace CodeGuard.Cli.Commands.Rules;

public static class ValidateCommand
{
    public static Command Build()
    {
        var pathOption = CommonOptions.CreatePathOption();
        var configOption = CommonOptions.CreateConfigOption();
        var rulesSourceOption = CommonOptions.CreateRulesSourceOption();
        var branchOption = CommonOptions.CreateBranchOption();
        var verbosityOption = CommonOptions.CreateVerbosityOption();

        var formatOption = CommonOptions.CreateFormatOption("Output format: console or json.", "console", "console", "json");

        var updateFingerprintsOption = new Option<bool>("--update-fingerprints")
        {
            Description =
                "Recompute and write metadata.source.fingerprint for rules whose linked documentation " +
                "has drifted or was never fingerprinted. Off by default - this is the only `rules " +
                "validate` mode that writes to rule files, and only edits the `fingerprint` value in " +
                "place (comments/key order/formatting elsewhere are untouched). Rules with a broken " +
                "link (missing file/section, or an ambiguous section) have nothing to fingerprint and " +
                "are left for a human to fix regardless of this flag.",
            DefaultValueFactory = _ => false
        };

        var command = new Command(
            "validate",
            "Validate a set of rule YAML files for structural correctness (schema conformance, known " +
            "selector/assertion/analyzer kinds, no duplicate rule ids) without evaluating them against a repository " +
            "(that's what the top-level `validate` command does). Also warns (never fails the exit code) when a " +
            "rule's metadata.source.file link to its documentation has drifted, is broken, or hasn't been " +
            "fingerprinted yet - the only place this command reads files outside the configured rules directory, " +
            "and only for rules that opt in via metadata.source.file. Use --rules-source to point directly at a " +
            "folder; otherwise validates whatever this repo is configured to use.");
        command.Add(pathOption);
        command.Add(configOption);
        command.Add(rulesSourceOption);
        command.Add(branchOption);
        command.Add(verbosityOption);
        command.Add(formatOption);
        command.Add(updateFingerprintsOption);

        command.SetAction((parseResult, _) =>
        {
            using var loggerFactory = CliLoggerFactory.Create(CliLoggerFactory.ParseVerbosity(parseResult.GetValue(verbosityOption)!));
            var logger = loggerFactory.CreateLogger(typeof(ValidateCommand));

            if (!CliRepositoryContext.TryResolve(
                    parseResult.GetValue(pathOption),
                    parseResult.GetValue(configOption),
                    out var context,
                    out var resolveError,
                    parseResult.GetValue(rulesSourceOption),
                    parseResult.GetValue(branchOption),
                    loggerFactory: loggerFactory))
            {
                Console.Error.WriteLine($"codeguard: {resolveError}");
                return Task.FromResult(1);
            }

            if (!context.TryRequireRulesConfigured(Console.Error))
            {
                return Task.FromResult(1);
            }

            var report = context.ValidateRules();
            logger.LogInformation("Rule set validation: {PassCount} passed, {FailCount} failed", report.Rules.Count, report.Issues.Count);

            var sourceReport = RuleSourceChecker.Check(report.Rules, context.RepoRoot);
            if (parseResult.GetValue(updateFingerprintsOption))
            {
                sourceReport = UpdateFingerprints(sourceReport, Console.Out);
            }

            if (parseResult.GetValue(formatOption) == "json")
            {
                RuleValidationReportWriter.WriteJson(report, sourceReport, Console.Out);
            }
            else
            {
                RuleValidationReportWriter.WriteConsole(report, sourceReport, Console.Out);
            }

            // Source-check findings never affect this - see docs/done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md
            // ("No Automatic Decisions"). Only the pre-existing schema/structural checks do.
            return Task.FromResult(report.IsValid ? 0 : 1);
        });

        return command;
    }

    /// <summary>
    /// Writes a recomputed fingerprint for every <see cref="RuleSourceIssueKind.ContentChanged"/>/
    /// <see cref="RuleSourceIssueKind.FingerprintMissing"/> issue, prints what changed, and returns the
    /// report with those issues removed so whatever gets printed afterward reflects post-update
    /// reality rather than repeating warnings that were just resolved.
    /// </summary>
    private static RuleSourceCheckReport UpdateFingerprints(RuleSourceCheckReport sourceReport, TextWriter writer)
    {
        var remaining = new List<RuleSourceIssue>();
        var updated = 0;

        foreach (var issue in sourceReport.Issues)
        {
            if (issue.Kind is RuleSourceIssueKind.ContentChanged or RuleSourceIssueKind.FingerprintMissing
                && issue.ComputedFingerprint is { } fingerprint)
            {
                RuleSourceFingerprintWriter.WriteFingerprint(issue.SourceFile, fingerprint);
                writer.WriteLine($"Updated fingerprint: {issue.RuleId} (was {issue.PreviousFingerprint ?? "missing"}, now {fingerprint})");
                updated++;
            }
            else
            {
                remaining.Add(issue);
            }
        }

        if (updated > 0)
        {
            writer.WriteLine();
        }

        return new RuleSourceCheckReport(remaining);
    }
}
