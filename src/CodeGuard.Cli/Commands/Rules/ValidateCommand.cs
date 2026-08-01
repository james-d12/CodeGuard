using System.CommandLine;
using CodeGuard.Cli.Support;
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

        var formatOption = new Option<string>("--format")
        {
            Description = "Output format: console or json.",
            DefaultValueFactory = _ => "console"
        };
        formatOption.AcceptOnlyFromAmong("console", "json");

        var command = new Command(
            "validate",
            "Validate a set of rule YAML files for structural correctness (schema conformance, known " +
            "selector/assertion/analyzer kinds, no duplicate rule ids) without evaluating them against a repository " +
            "(that's what the top-level `validate` command does). Use --rules-source to point directly at a folder; " +
            "otherwise validates whatever this repo is configured to use.");
        command.Add(pathOption);
        command.Add(configOption);
        command.Add(rulesSourceOption);
        command.Add(branchOption);
        command.Add(verbosityOption);
        command.Add(formatOption);

        command.SetAction((parseResult, _) =>
        {
            using var loggerFactory = CliLoggerFactory.Create(CliLoggerFactory.ParseVerbosity(parseResult.GetValue(verbosityOption)!));
            var logger = loggerFactory.CreateLogger(typeof(ValidateCommand));

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

            var report = context.ValidateRules();
            logger.LogInformation("Rule set validation: {PassCount} passed, {FailCount} failed", report.Rules.Count, report.Issues.Count);

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
