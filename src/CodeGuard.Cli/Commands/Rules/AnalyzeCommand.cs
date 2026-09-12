using System.CommandLine;
using CodeGuard.Cli.Support;
using CodeGuard.Configuration.Analysis;
using CodeGuard.Configuration.Capabilities;
using Microsoft.Extensions.Logging;

namespace CodeGuard.Cli.Commands.Rules;

/// <summary>
/// Analyses a rule set for mechanically detectable problems - invalid rules, duplicate ids, missing
/// or one-sided tests, disabled/illustrative rules, unreachable assertions and exact-duplicate rules.
/// See docs/HIGH_LEVEL_AI_ASSISTING.md §14. This is a superset of `rules validate`'s checks (it
/// reuses the same load/validate pass) plus additional, non-fatal findings for a human or an AI
/// agent to review - unlike `rules validate`, a non-empty report doesn't necessarily mean the rule
/// set is broken (e.g. illustrative rules are expected in this repo's own `examples/rules/`).
/// </summary>
public static class AnalyzeCommand
{
    public static Command Build()
    {
        var pathOption = CommonOptions.CreatePathOption();
        var configOption = CommonOptions.CreateConfigOption();
        var rulesSourceOption = CommonOptions.CreateRulesSourceOption();
        var branchOption = CommonOptions.CreateBranchOption();
        var verbosityOption = CommonOptions.CreateVerbosityOption();

        var formatOption = new Option<string>("--format")
        {
            Description = "Output format: console or json.",
            DefaultValueFactory = _ => "console"
        };
        formatOption.AcceptOnlyFromAmong("console", "json");

        var command = new Command(
            "analyze",
            "Analyze a rule set for mechanically detectable problems beyond structural validity: " +
            "missing/one-sided tests, disabled or illustrative rules, assertions that can never apply " +
            "to their target, and exact-duplicate rules. Does not evaluate rules against a repository.");
        command.Add(pathOption);
        command.Add(configOption);
        command.Add(rulesSourceOption);
        command.Add(branchOption);
        command.Add(verbosityOption);
        command.Add(formatOption);

        command.SetAction((parseResult, _) =>
        {
            using var loggerFactory = CliLoggerFactory.Create(CliLoggerFactory.ParseVerbosity(parseResult.GetValue(verbosityOption)!));
            var logger = loggerFactory.CreateLogger(typeof(AnalyzeCommand));

            var context = CliRepositoryContext.Resolve(
                parseResult.GetValue(pathOption),
                parseResult.GetValue(configOption),
                parseResult.GetValue(rulesSourceOption),
                parseResult.GetValue(branchOption),
                loggerFactory: loggerFactory);

            if (!context.TryRequireRulesConfigured(Console.Error))
            {
                return Task.FromResult(1);
            }

            var validation = context.ValidateRules();
            var report = RuleSetAnalyzer.Analyze(validation, CapabilityCatalog.Create());

            logger.LogInformation(
                "Rule analysis: {RuleCount} rule(s), {InvalidCount} invalid, {DuplicateCount} duplicate id(s), " +
                "{MissingTestsCount} without tests, {OneSidedCount} one-sided, {UnreachableCount} unreachable " +
                "assertion(s), {ExactDuplicateCount} exact-duplicate group(s)",
                report.RuleCount, report.InvalidRules.Count, report.DuplicateIds.Count, report.RulesWithoutTests.Count,
                report.OneSidedTestRules.Count, report.UnreachableRules.Count, report.ExactDuplicateRules.Count);

            if (parseResult.GetValue(formatOption) == "json")
            {
                RuleAnalysisReportWriter.WriteJson(report, Console.Out);
            }
            else
            {
                RuleAnalysisReportWriter.WriteConsole(report, Console.Out);
            }

            return Task.FromResult(report.HasFindings ? 1 : 0);
        });

        return command;
    }
}
