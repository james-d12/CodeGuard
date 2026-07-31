using System.CommandLine;
using CodeGuard.Cli.Support;

namespace CodeGuard.Cli.Commands;

public static class CheckRulesCommand
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

        var command = new Command(
            "check-rules",
            "Validate a set of rule YAML files for structural correctness (schema conformance, known " +
            "selector/assertion/analyzer kinds, no duplicate rule ids) without evaluating them against a repository. " +
            "Use --rules-source to point directly at a folder; otherwise checks whatever this repo is configured to use.");
        command.Add(pathOption);
        command.Add(configOption);
        command.Add(rulesSourceOption);
        command.Add(branchOption);
        command.Add(formatOption);

        command.SetAction((parseResult, _) =>
        {
            var context = CliRepositoryContext.Resolve(
                parseResult.GetValue(pathOption),
                parseResult.GetValue(configOption),
                parseResult.GetValue(rulesSourceOption),
                parseResult.GetValue(branchOption));

            var report = context.ValidateRules();

            if (parseResult.GetValue(formatOption) == "json")
            {
                RuleValidationReportWriter.WriteJson(report, Console.Out);
            }
            else
            {
                RuleValidationReportWriter.WriteConsole(report, Console.Out);
            }

            return Task.FromResult(report.IsValid ? 0 : 1);
        });

        return command;
    }
}
