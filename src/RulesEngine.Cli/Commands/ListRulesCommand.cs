using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using RulesEngine.Cli.Support;
using RulesEngine.RuleModel.Rules;

namespace RulesEngine.Cli.Commands;

public static class ListRulesCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
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

        var tagOption = new Option<string[]>("--tag")
        {
            Description = "Only include rules with at least one of these tags (repeatable)."
        };

        var enabledOnlyOption = new Option<bool>("--enabled-only")
        {
            Description = "Only include enabled rules."
        };

        var command = new Command("list-rules", "List rules discovered from the configured rule directories");
        command.Add(pathOption);
        command.Add(configOption);
        command.Add(rulesSourceOption);
        command.Add(branchOption);
        command.Add(formatOption);
        command.Add(tagOption);
        command.Add(enabledOnlyOption);

        command.SetAction((parseResult, cancellationToken) =>
        {
            var context = CliRepositoryContext.Resolve(
                parseResult.GetValue(pathOption),
                parseResult.GetValue(configOption),
                parseResult.GetValue(rulesSourceOption),
                parseResult.GetValue(branchOption));

            var rules = context.LoadRules().AsEnumerable();

            var tags = parseResult.GetValue(tagOption) ?? [];
            if (tags.Length > 0)
            {
                var tagSet = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
                rules = rules.Where(r => r.Tags.Any(tagSet.Contains));
            }

            if (parseResult.GetValue(enabledOnlyOption))
            {
                rules = rules.Where(r => r.Enabled);
            }

            var ruleList = rules.OrderBy(r => r.Id, StringComparer.Ordinal).ToList();

            if (parseResult.GetValue(formatOption) == "json")
            {
                var summaries = ruleList.Select(RuleSummary.From);
                Console.WriteLine(JsonSerializer.Serialize(summaries, JsonOptions));
            }
            else
            {
                WriteTable(ruleList);
            }

            return Task.FromResult(0);
        });

        return command;
    }

    private static void WriteTable(IReadOnlyList<RuleDefinition> rules)
    {
        if (rules.Count == 0)
        {
            Console.WriteLine("No rules found.");
            return;
        }

        Console.WriteLine($"{"ID",-24}{"SEVERITY",-10}{"ENFORCEMENT",-24}{"ENABLED",-9}TAGS");
        foreach (var rule in rules)
        {
            var tags = string.Join(",", rule.Tags);
            Console.WriteLine(
                $"{rule.Id,-24}{rule.Severity,-10}{rule.Enforcement.Classification,-24}{rule.Enabled,-9}{tags}");
        }
    }

    private sealed record RuleSummary(
        string Id,
        string Name,
        Severity Severity,
        EnforcementClassification Enforcement,
        IReadOnlyList<string> Tags,
        bool Enabled,
        bool Illustrative)
    {
        public static RuleSummary From(RuleDefinition rule) => new(
            rule.Id, rule.Name, rule.Severity, rule.Enforcement.Classification,
            rule.Tags, rule.Enabled, rule.Illustrative);
    }
}
