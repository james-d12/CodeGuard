using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CodeGuard.Cli.Support;
using CodeGuard.Configuration.Loading;
using CodeGuard.RuleModel.Rules;
using Microsoft.Extensions.Logging;

namespace CodeGuard.Cli.Commands.Rules;

public static class ExplainCommand
{
    public static Command Build()
    {
        var pathOption = CommonOptions.CreatePathOption();
        var configOption = CommonOptions.CreateConfigOption();
        var rulesSourceOption = CommonOptions.CreateRulesSourceOption();
        var branchOption = CommonOptions.CreateBranchOption();
        var verbosityOption = CommonOptions.CreateVerbosityOption();
        var ruleIdArgument = new Argument<string>("ruleId")
        {
            Description = "The rule ID to explain, e.g. DDD-ENTITY-001."
        };

        var formatOption = new Option<string>("--format")
        {
            Description = "Output format: console or json.",
            DefaultValueFactory = _ => "console"
        };
        formatOption.AcceptOnlyFromAmong("console", "json");

        var command = new Command("explain", "Print full metadata and source YAML for a single rule");
        command.Add(pathOption);
        command.Add(configOption);
        command.Add(rulesSourceOption);
        command.Add(branchOption);
        command.Add(verbosityOption);
        command.Add(ruleIdArgument);
        command.Add(formatOption);

        command.SetAction((parseResult, _) =>
        {
            using var loggerFactory = CliLoggerFactory.Create(CliLoggerFactory.ParseVerbosity(parseResult.GetValue(verbosityOption)!));
            var logger = loggerFactory.CreateLogger(typeof(ExplainCommand));

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

            var ruleId = parseResult.GetValue(ruleIdArgument)!;
            var entries = context.LoadRulesWithSource();
            var entry = entries.FirstOrDefault(e => e.Rule.Id == ruleId);

            if (entry.Rule is null)
            {
                logger.LogWarning("Rule {RuleId} not found under {RulesPaths}", ruleId, string.Join(", ", context.Layout.RulesPaths));
                Console.Error.WriteLine($"Rule '{ruleId}' was not found under {string.Join(", ", context.Layout.RulesPaths)}.");
                return Task.FromResult(1);
            }

            if (parseResult.GetValue(formatOption) == "json")
            {
                PrintJson(entry.Rule, entry.SourceFile);
            }
            else
            {
                PrintSummary(entry.Rule);
                Console.WriteLine();
                Console.WriteLine($"Source: {entry.SourceFile}");
                Console.WriteLine();
                Console.WriteLine("--- Raw YAML ---");
                Console.WriteLine(File.ReadAllText(entry.SourceFile));
            }

            return Task.FromResult(0);
        });

        return command;
    }


    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Emits the parsed rule's metadata plus its source document. The document is included verbatim
    /// (converted YAML to JSON) rather than reconstructed from the parsed rule: IAssertion exposes
    /// only a Kind, so a selector's or assertion's parameter *values* cannot be recovered from the
    /// model at all - see docs/HIGH_LEVEL_AI_ASSISTING.md section 13.
    /// </summary>
    private static void PrintJson(RuleDefinition rule, string sourceFile)
    {
        var payload = new JsonObject
        {
            ["id"] = rule.Id,
            ["name"] = rule.Name,
            ["description"] = rule.Description?.Trim(),
            ["severity"] = rule.Severity.ToString().ToLowerInvariant(),
            ["enforcement"] = new JsonObject
            {
                ["classification"] = ToSnakeCase(rule.Enforcement.Classification.ToString())
            },
            ["tags"] = new JsonArray(rule.Tags.Select(t => (JsonNode)t!).ToArray()),
            ["remediation"] = rule.Remediation?.Trim(),
            ["documentation"] = new JsonArray(rule.Documentation.Select(d => (JsonNode)d!).ToArray()),
            ["enabled"] = rule.Enabled,
            ["illustrative"] = rule.Illustrative,
            // "declarative" is the target+assertions form. Spelled without a '+' so the value doesn't
            // come back unicode-escaped by the default JSON encoder.
            ["shape"] = rule.Analyzer is not null ? "analyzer" : "declarative",
            ["testCount"] = rule.Tests.Count,
            ["sourceFile"] = sourceFile,
            ["document"] = RuleFileLoader.ReadDocument(sourceFile)
        };

        Console.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static string ToSnakeCase(string value) =>
        string.Concat(value.Select((c, i) => char.IsUpper(c) && i > 0 ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));

    private static void PrintSummary(RuleDefinition rule)
    {
        Console.WriteLine($"Id:            {rule.Id}");
        Console.WriteLine($"Name:          {rule.Name}");
        if (rule.Description is not null)
        {
            Console.WriteLine($"Description:   {rule.Description.Trim()}");
        }
        Console.WriteLine($"Severity:      {rule.Severity}");
        Console.WriteLine($"Enforcement:   {rule.Enforcement.Classification}");
        Console.WriteLine($"Tags:          {(rule.Tags.Count == 0 ? "-" : string.Join(", ", rule.Tags))}");
        Console.WriteLine($"Enabled:       {rule.Enabled}");
        Console.WriteLine($"Illustrative:  {rule.Illustrative}");
        if (rule.Analyzer is not null)
        {
            Console.WriteLine($"Analyzer:      {rule.Analyzer.Name}");
        }
        else
        {
            Console.WriteLine($"Target kind:   {rule.Target!.Kind}");
            Console.WriteLine($"Assertions:    {string.Join(", ", rule.Assertions!.Select(a => a.Kind))}");
        }
        if (rule.Remediation is not null)
        {
            Console.WriteLine($"Remediation:   {rule.Remediation.Trim()}");
        }
        if (rule.Documentation.Count > 0)
        {
            Console.WriteLine($"Documentation: {string.Join(", ", rule.Documentation)}");
        }
    }
}
