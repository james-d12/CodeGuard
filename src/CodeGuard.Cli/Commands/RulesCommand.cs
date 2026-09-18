using System.CommandLine;

namespace CodeGuard.Cli.Commands;

public static class RulesCommand
{
    public static Command Build()
    {
        var command = new Command("rules", "Inspect, validate, and author rule YAML files");
        command.Subcommands.Add(Rules.ValidateCommand.Build());
        command.Subcommands.Add(Rules.ListCommand.Build());
        command.Subcommands.Add(Rules.ExplainCommand.Build());
        command.Subcommands.Add(Rules.TestCommand.Build());
        command.Subcommands.Add(Rules.DiscoverCommand.Build());
        command.Subcommands.Add(Rules.AnalyzeCommand.Build());
        return command;
    }
}
