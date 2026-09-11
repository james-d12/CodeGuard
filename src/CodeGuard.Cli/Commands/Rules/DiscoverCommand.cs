using System.CommandLine;
using CodeGuard.Cli.Support;
using CodeGuard.Configuration.Capabilities;

namespace CodeGuard.Cli.Commands.Rules;

/// <summary>
/// Prints the engine's rule-authoring vocabulary. Unlike every other `rules` subcommand this reads no
/// rule files and needs no repository - the catalog comes from the parser registries, so it answers
/// "what can a rule say?" rather than "what do these rules say?".
/// </summary>
public static class DiscoverCommand
{
    public static Command Build()
    {
        var formatOption = new Option<string>("--format")
        {
            Description = "Output format: console, json, or markdown.",
            DefaultValueFactory = _ => "console"
        };
        formatOption.AcceptOnlyFromAmong("console", "json", "markdown");

        var sectionOption = new Option<string>("--section")
        {
            Description = "With --format markdown, which table to emit: selectors, assertions, or analyzers.",
            DefaultValueFactory = _ => "selectors"
        };
        sectionOption.AcceptOnlyFromAmong("selectors", "assertions", "analyzers");

        var command = new Command(
            "discover",
            "List every target selector, assertion, analyzer and condition the engine supports, with " +
            "their parameters. Reads no rule files - use this to find out what a rule can express " +
            "before writing one.");
        command.Add(formatOption);
        command.Add(sectionOption);

        command.SetAction((parseResult, _) =>
        {
            var catalog = CapabilityCatalog.Create();

            switch (parseResult.GetValue(formatOption))
            {
                case "json":
                    CapabilityReportWriter.WriteJson(catalog, Console.Out);
                    break;
                case "markdown":
                    CapabilityReportWriter.WriteMarkdown(catalog, Console.Out, parseResult.GetValue(sectionOption)!);
                    break;
                default:
                    CapabilityReportWriter.WriteConsole(catalog, Console.Out);
                    break;
            }

            return Task.FromResult(0);
        });

        return command;
    }
}
