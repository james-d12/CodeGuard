using System.CommandLine;
using CodeGuard.Cli.Support;
using CodeGuard.Configuration.Analysis;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Configuration.Sources;
using CodeGuard.Configuration.Versioning;
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
                "Recompute and write metadata.source.fingerprint (for rules whose linked documentation " +
                "has drifted or was never fingerprinted) and versionFingerprint (for every rule whose " +
                "enforceable body has drifted or was never fingerprinted) alike. Off by default - this " +
                "is the only `rules validate` mode that writes to rule files, and only edits the " +
                "relevant fingerprint value in place (comments/key order/formatting elsewhere are " +
                "untouched). Rules with a broken source link (missing file/section, or an ambiguous " +
                "section) have nothing to fingerprint and are left for a human to fix regardless of " +
                "this flag; if you changed a rule's enforceable behavior, consider bumping `version` " +
                "before running this, since it only updates the fingerprint, never `version`.",
            DefaultValueFactory = _ => false
        };

        var command = new Command(
            "validate",
            "Validate a set of rule YAML files for structural correctness (schema conformance, known " +
            "selector/assertion/analyzer kinds, no duplicate rule ids) without evaluating them against a repository " +
            "(that's what the top-level `validate` command does). Also checks two independent fingerprint " +
            "mechanisms: a rule's optional metadata.source.file link to its documentation drifting, being " +
            "broken, or never fingerprinted (metadata.source.* - warns only, never fails the exit code); " +
            "and every rule's own enforceable body (target/assertions/when/analyzer) drifting from its " +
            "recorded versionFingerprint, checked unconditionally for every rule with no opt-in (fails " +
            "the exit code - see docs/RULE_VERSIONING_PLAN.md). These are the only cases this command " +
            "reads files outside the configured rules directory (source) or re-derives content from " +
            "rules already loaded (version). Also reports (never fails the exit code) rule-set-level " +
            "findings - missing/one-sided tests, disabled/illustrative rules, unreachable assertions, " +
            "and exact-duplicate rules - the same checks the former standalone `rules analyze` command " +
            "used to report separately. Use --rules-source to point directly at a folder; otherwise " +
            "validates whatever this repo is configured to use.");
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
            var versionReport = RuleVersionChecker.Check(report.Rules);
            if (parseResult.GetValue(updateFingerprintsOption))
            {
                sourceReport = UpdateSourceFingerprints(sourceReport, Console.Out);
                versionReport = UpdateVersionFingerprints(versionReport, Console.Out);
            }

            var analysisReport = RuleSetAnalyzer.Analyze(report, CapabilityCatalog.Create());
            logger.LogInformation(
                "Rule analysis: {MissingTestsCount} without tests, {OneSidedCount} one-sided, " +
                "{UnreachableCount} unreachable assertion(s), {ExactDuplicateCount} exact-duplicate group(s)",
                analysisReport.RulesWithoutTests.Count, analysisReport.OneSidedTestRules.Count,
                analysisReport.UnreachableRules.Count, analysisReport.ExactDuplicateRules.Count);

            if (parseResult.GetValue(formatOption) == "json")
            {
                RuleValidationReportWriter.WriteJson(report, sourceReport, versionReport, analysisReport, Console.Out);
            }
            else
            {
                RuleValidationReportWriter.WriteConsole(report, sourceReport, versionReport, analysisReport, Console.Out);
            }

            // Source-check findings never affect this - see docs/done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md
            // ("No Automatic Decisions"). Rule-analysis findings never affect this either, same reasoning.
            // Version-check findings do - see docs/RULE_VERSIONING_PLAN.md.
            return Task.FromResult(report.IsValid && versionReport.IsValid ? 0 : 1);
        });

        return command;
    }

    /// <summary>
    /// Writes a recomputed fingerprint for every <see cref="RuleSourceIssueKind.ContentChanged"/>/
    /// <see cref="RuleSourceIssueKind.FingerprintMissing"/> issue, prints what changed, and returns the
    /// report with those issues removed so whatever gets printed afterward reflects post-update
    /// reality rather than repeating warnings that were just resolved.
    /// </summary>
    private static RuleSourceCheckReport UpdateSourceFingerprints(RuleSourceCheckReport sourceReport, TextWriter writer)
    {
        var remaining = new List<RuleSourceIssue>();
        var updated = 0;

        foreach (var issue in sourceReport.Issues)
        {
            if (issue.Kind is RuleSourceIssueKind.ContentChanged or RuleSourceIssueKind.FingerprintMissing
                && issue.ComputedFingerprint is { } fingerprint)
            {
                RuleSourceFingerprintWriter.WriteFingerprint(issue.SourceFile, fingerprint);
                writer.WriteLine($"Updated source fingerprint: {issue.RuleId} (was {issue.PreviousFingerprint ?? "missing"}, now {fingerprint})");
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

    /// <summary>Same idea as <see cref="UpdateSourceFingerprints"/>, for <see cref="RuleVersionIssue"/>s.</summary>
    private static RuleVersionCheckReport UpdateVersionFingerprints(RuleVersionCheckReport versionReport, TextWriter writer)
    {
        var updated = 0;

        foreach (var issue in versionReport.Issues)
        {
            RuleVersionFingerprintWriter.WriteFingerprint(issue.SourceFile, issue.ComputedFingerprint);
            writer.WriteLine(
                $"Updated version fingerprint: {issue.RuleId} (was {issue.RecordedFingerprint ?? "missing"}, now {issue.ComputedFingerprint})");
            updated++;
        }

        if (updated > 0)
        {
            writer.WriteLine();
        }

        return new RuleVersionCheckReport([]);
    }
}
