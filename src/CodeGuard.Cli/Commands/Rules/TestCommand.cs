using System.CommandLine;
using CodeGuard.Cli.Support;
using Microsoft.Extensions.Logging;
using CodeGuard.Configuration.Testing;

namespace CodeGuard.Cli.Commands.Rules;

public static class TestCommand
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

        var ruleOption = new Option<string[]>("--rule")
        {
            Description = "Restrict test execution to these rule IDs (repeatable). Default: every rule with embedded tests."
        };

        var colorOption = new Option<bool>("--color")
        {
            Description = "Force ANSI color in console output, even when redirected."
        };

        var noColorOption = new Option<bool>("--no-color")
        {
            Description = "Disable ANSI color in console output, even in an interactive terminal."
        };

        var command = new Command(
            "test",
            "Run a rule set's embedded `tests:` cases against a virtual analysis model, using the same " +
            "evaluator as `codeguard validate` (no repository, no disk I/O). See docs/RULES_TEST_DESIGN.md.");
        command.Add(pathOption);
        command.Add(configOption);
        command.Add(rulesSourceOption);
        command.Add(branchOption);
        command.Add(verbosityOption);
        command.Add(formatOption);
        command.Add(ruleOption);
        command.Add(colorOption);
        command.Add(noColorOption);

        command.SetAction((parseResult, _) =>
        {
            using var loggerFactory = CliLoggerFactory.Create(CliLoggerFactory.ParseVerbosity(parseResult.GetValue(verbosityOption)!));
            var logger = loggerFactory.CreateLogger(typeof(TestCommand));

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

            var rules = context.LoadRules().Where(r => r.Tests.Count > 0);

            var selectedRuleIds = parseResult.GetValue(ruleOption) ?? [];
            if (selectedRuleIds.Length > 0)
            {
                var selectedSet = new HashSet<string>(selectedRuleIds, StringComparer.Ordinal);
                rules = rules.Where(r => selectedSet.Contains(r.Id));
            }

            var results = rules.SelectMany(RuleTestRunner.Run).ToList();

            logger.LogInformation(
                "Rule tests: {Total} case(s), {Passed} passed, {Failed} failed, {Errored} errored",
                results.Count,
                results.Count(r => r.Outcome == TestOutcome.Passed),
                results.Count(r => r.Outcome == TestOutcome.Failed),
                results.Count(r => r.Outcome == TestOutcome.Errored));

            if (parseResult.GetValue(formatOption) == "json")
            {
                RuleTestReportWriter.WriteJson(results, Console.Out);
            }
            else
            {
                var useColor = ColorSupport.ShouldUseColor(
                    parseResult.GetValue(colorOption),
                    parseResult.GetValue(noColorOption),
                    writingToFile: false,
                    consoleOutputRedirected: Console.IsOutputRedirected,
                    noColorEnvVar: Environment.GetEnvironmentVariable("NO_COLOR"));

                RuleTestReportWriter.WriteConsole(results, Console.Out, useColor);
            }

            var passed = results.All(r => r.Outcome == TestOutcome.Passed);
            return Task.FromResult(passed ? 0 : 1);
        });

        return command;
    }
}
