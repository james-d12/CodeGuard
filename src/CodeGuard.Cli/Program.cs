using System.CommandLine;
using Microsoft.Build.Locator;
using CodeGuard.Cli.Commands;

if (!MSBuildLocator.IsRegistered)
{
    try
    {
        MSBuildLocator.RegisterDefaults();
    }
    catch (InvalidOperationException)
    {
        await Console.Error.WriteLineAsync(
            "codeguard could not find a .NET SDK/MSBuild install on this machine. " +
            "Install the .NET SDK from https://dotnet.microsoft.com/download and try again.");
        return 1;
    }
}

var rootCommand = new RootCommand("Deterministic engineering rules and analysis engine");

rootCommand.Subcommands.Add(ValidateCommand.Build());
rootCommand.Subcommands.Add(CheckRulesCommand.Build());
rootCommand.Subcommands.Add(ListRulesCommand.Build());
rootCommand.Subcommands.Add(ExplainRuleCommand.Build());
rootCommand.Subcommands.Add(SetupCommand.Build());

return await rootCommand.Parse(args).InvokeAsync();
