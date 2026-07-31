using System.CommandLine;

namespace CodeGuard.Cli.Commands;

public static class RulesCommand
{
    public static Command Build()
    {
        var command = new Command("rules", "Inspect, validate, and author rule YAML files");
        command.Subcommands.Add(Rules.CheckCommand.Build());
        command.Subcommands.Add(Rules.ListCommand.Build());
        command.Subcommands.Add(Rules.ExplainCommand.Build());
        command.Subcommands.Add(Rules.CreateCommand.Build());
        return command;
    }
}
