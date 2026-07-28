using System.CommandLine;
using System.Text.Json;
using RulesEngine.Cli.Support;

namespace RulesEngine.Cli.Commands;

public static class ListStandardsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static Command Build()
    {
        var pathOption = CommonOptions.CreatePathOption();
        var configOption = CommonOptions.CreateConfigOption();
        var rulesSourceOption = CommonOptions.CreateRulesSourceOption();
        var branchOption = CommonOptions.CreateBranchOption();

        var formatOption = new Option<string>("--format")
        {
            Description = "Output format: table or json.",
            DefaultValueFactory = _ => "table"
        };
        formatOption.AcceptOnlyFromAmong("table", "json");

        var command = new Command("list-standards", "List standards referenced by the configured rules");
        command.Add(pathOption);
        command.Add(configOption);
        command.Add(rulesSourceOption);
        command.Add(branchOption);
        command.Add(formatOption);

        command.SetAction((parseResult, cancellationToken) =>
        {
            var context = CliRepositoryContext.Resolve(
                parseResult.GetValue(pathOption),
                parseResult.GetValue(configOption),
                parseResult.GetValue(rulesSourceOption),
                parseResult.GetValue(branchOption));

            var standards = context.LoadRules()
                .Where(r => r.Standard is not null)
                .GroupBy(r => r.Standard!)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new StandardSummary(g.Key, g.Count(), g.Select(r => r.Id).OrderBy(id => id, StringComparer.Ordinal).ToList()))
                .ToList();

            if (parseResult.GetValue(formatOption) == "json")
            {
                Console.WriteLine(JsonSerializer.Serialize(standards, JsonOptions));
            }
            else
            {
                WriteTable(standards);
            }

            return Task.FromResult(0);
        });

        return command;
    }

    private static void WriteTable(IReadOnlyList<StandardSummary> standards)
    {
        if (standards.Count == 0)
        {
            Console.WriteLine("No standards found.");
            return;
        }

        Console.WriteLine($"{"STANDARD",-20}{"RULE COUNT",-12}RULE IDS");
        foreach (var standard in standards)
        {
            Console.WriteLine($"{standard.Standard,-20}{standard.RuleCount,-12}{string.Join(", ", standard.RuleIds)}");
        }
    }

    private sealed record StandardSummary(string Standard, int RuleCount, IReadOnlyList<string> RuleIds);
}
