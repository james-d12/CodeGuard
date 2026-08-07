using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeGuard.Cli.Support;
using CodeGuard.RuleModel.Rules;
using Microsoft.Extensions.Logging;

namespace CodeGuard.Cli.Commands.Rules;

public static class ListCommand
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
        var verbosityOption = CommonOptions.CreateVerbosityOption();

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

        var command = new Command("list", "List rules discovered from the configured rule directories");
        command.Add(pathOption);
        command.Add(configOption);
        command.Add(rulesSourceOption);
        command.Add(branchOption);
        command.Add(verbosityOption);
        command.Add(formatOption);
        command.Add(tagOption);
        command.Add(enabledOnlyOption);

        command.SetAction((parseResult, cancellationToken) =>
        {
            using var loggerFactory = CliLoggerFactory.Create(CliLoggerFactory.ParseVerbosity(parseResult.GetValue(verbosityOption)!));
            var logger = loggerFactory.CreateLogger(typeof(ListCommand));

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
            logger.LogDebug(
                "Listed {Count} rule(s) after filtering (tags={TagCount}, enabledOnly={EnabledOnly})",
                ruleList.Count, tags.Length, parseResult.GetValue(enabledOnlyOption));

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

        const string idHeader = "ID";
        const string severityHeader = "SEVERITY";
        const string enforcementHeader = "ENFORCEMENT";
        const string enabledHeader = "ENABLED";
        const string tagsHeader = "TAGS";
        const int columnGap = 2;

        var rows = rules
            .Select(rule => (
                Id: rule.Id,
                Severity: rule.Severity.ToString(),
                Enforcement: rule.Enforcement.Classification.ToString(),
                Enabled: rule.Enabled.ToString(),
                Tags: string.Join(",", rule.Tags)))
            .ToList();

        var idWidth = Math.Max(idHeader.Length, rows.Max(r => r.Id.Length)) + columnGap;
        var severityWidth = Math.Max(severityHeader.Length, rows.Max(r => r.Severity.Length)) + columnGap;
        var enforcementWidth = Math.Max(enforcementHeader.Length, rows.Max(r => r.Enforcement.Length)) + columnGap;
        var enabledWidth = Math.Max(enabledHeader.Length, rows.Max(r => r.Enabled.Length)) + columnGap;

        Console.WriteLine(
            $"{idHeader.PadRight(idWidth)}{severityHeader.PadRight(severityWidth)}{enforcementHeader.PadRight(enforcementWidth)}{enabledHeader.PadRight(enabledWidth)}{tagsHeader}");
        Console.WriteLine(new string('-', idWidth + severityWidth + enforcementWidth + enabledWidth + tagsHeader.Length));

        foreach (var row in rows)
        {
            Console.WriteLine(
                $"{row.Id.PadRight(idWidth)}{row.Severity.PadRight(severityWidth)}{row.Enforcement.PadRight(enforcementWidth)}{row.Enabled.PadRight(enabledWidth)}{row.Tags}");
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
