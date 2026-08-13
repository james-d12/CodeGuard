using System.CommandLine;
using CodeGuard.Cli.Support;

namespace CodeGuard.Cli.Commands.Rules;

public static class TestCommand
{
    public static Command Build()
    {
        var pathOption = CommonOptions.CreatePathOption();
        var configOption = CommonOptions.CreateConfigOption();
        var rulesSourceOption = CommonOptions.CreateRulesSourceOption();
        var branchOption = CommonOptions.CreateBranchOption();

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
        command.Add(formatOption);
        command.Add(ruleOption);
        command.Add(colorOption);
        command.Add(noColorOption);

        command.SetAction((parseResult, _) =>
        {
            var context = CliRepositoryContext.Resolve(
                parseResult.GetValue(pathOption),
                parseResult.GetValue(configOption),
                parseResult.GetValue(rulesSourceOption),
                parseResult.GetValue(branchOption));

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
