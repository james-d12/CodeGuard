using System.CommandLine;
using Microsoft.Build.Locator;
using RulesEngine.Cli.Commands;

if (!MSBuildLocator.IsRegistered)
{
    MSBuildLocator.RegisterDefaults();
}

var rootCommand = new RootCommand("Deterministic engineering rules and analysis engine");

rootCommand.Subcommands.Add(ValidateCommand.Build());
rootCommand.Subcommands.Add(CheckRulesCommand.Build());
rootCommand.Subcommands.Add(ListRulesCommand.Build());
rootCommand.Subcommands.Add(ExplainRuleCommand.Build());
rootCommand.Subcommands.Add(ListStandardsCommand.Build());
rootCommand.Subcommands.Add(SetupCommand.Build());

return await rootCommand.Parse(args).InvokeAsync();
