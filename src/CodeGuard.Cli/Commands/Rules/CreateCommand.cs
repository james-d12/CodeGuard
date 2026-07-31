using System.CommandLine;
using CodeGuard.Cli.Support;
using CodeGuard.Configuration.Parsing;
using CodeGuard.Configuration.Writing;

namespace CodeGuard.Cli.Commands.Rules;

/// <summary>
/// Interactively scaffolds a new rule YAML file. Deliberately doesn't hardcode per-selector/
/// per-assertion parameter shapes (there are 14 target selector kinds and ~35 assertion kinds in
/// <see cref="DefaultParsers"/>, each with different parameter names) - instead drives a generic
/// kind-picker + key/value parameter loop off <see cref="SelectorParserRegistry.Kinds"/>/
/// <see cref="AssertionParserRegistry.Kinds"/>, so new kinds are picked up automatically. Only
/// authors the `target`+`assertions` rule shape, not the `analyzer`-referencing shape.
/// </summary>
public static class CreateCommand
{
    private static readonly string[] AllowedSeverities = ["info", "warning", "error", "critical"];

    public static Command Build()
    {
        var pathOption = CommonOptions.CreatePathOption();
        var configOption = CommonOptions.CreateConfigOption();
        var rulesSourceOption = CommonOptions.CreateRulesSourceOption();
        var branchOption = CommonOptions.CreateBranchOption();

        var idOption = new Option<string?>("--id")
        {
            Description = "Rule ID, e.g. DDD-ENTITY-003. Prompted for interactively if omitted."
        };
        var nameOption = new Option<string?>("--name")
        {
            Description = "Short human-readable rule name. Prompted for interactively if omitted."
        };
        var descriptionOption = new Option<string?>("--description")
        {
            Description = "Longer description of what the rule checks. Prompted for interactively if omitted."
        };
        var severityOption = new Option<string?>("--severity")
        {
            Description = "Rule severity: info, warning, error, or critical (default: warning). Prompted for interactively if omitted."
        };
        severityOption.AcceptOnlyFromAmong(AllowedSeverities);
        var tagOption = new Option<string[]>("--tag")
        {
            Description = "Tag to attach to the rule (repeatable). Prompted for interactively if omitted."
        };

        var command = new Command(
            "create",
            "Interactively scaffold a new rule YAML file: prompts for metadata, then a target " +
            "selector and one or more assertions, then validates the result before saving.");
        command.Add(pathOption);
        command.Add(configOption);
        command.Add(rulesSourceOption);
        command.Add(branchOption);
        command.Add(idOption);
        command.Add(nameOption);
        command.Add(descriptionOption);
        command.Add(severityOption);
        command.Add(tagOption);

        command.SetAction((parseResult, _) =>
        {
            var context = CliRepositoryContext.Resolve(
                parseResult.GetValue(pathOption),
                parseResult.GetValue(configOption),
                parseResult.GetValue(rulesSourceOption),
                parseResult.GetValue(branchOption));

            if (!context.TryRequireRulesConfigured(Console.Error))
            {
                return Task.FromResult(1);
            }

            var id = PromptRequired(parseResult.GetValue(idOption), "Rule ID (e.g. DDD-ENTITY-003): ");
            var name = PromptRequired(parseResult.GetValue(nameOption), "Rule name: ");
            var description = PromptOptional(parseResult.GetValue(descriptionOption), "Description (optional, blank to skip): ");
            var severity = PromptSeverity(parseResult.GetValue(severityOption), "Severity - info/warning/error/critical (blank = warning): ");
            var tags = PromptTags(parseResult.GetValue(tagOption) ?? [], "Tags, comma-separated (optional, blank to skip): ");

            var selectorRegistry = DefaultParsers.CreateSelectorRegistry();
            var assertionRegistry = DefaultParsers.CreateAssertionRegistry(selectorRegistry);

            Console.WriteLine();
            Console.WriteLine("--- Target selector ---");
            var target = PromptTargetSelector(selectorRegistry.Kinds);

            Console.WriteLine();
            Console.WriteLine("--- Assertions ---");
            var assertions = new List<object>();
            do
            {
                var assertionKind = PromptKind("Assertion kind", assertionRegistry.Kinds);
                var assertionParameters = PromptParameters();
                assertions.Add(new Dictionary<string, object> { [assertionKind] = assertionParameters });
            } while (PromptYesNo("Add another assertion?", defaultYes: false));

            var document = new Dictionary<string, object>
            {
                ["id"] = id,
                ["name"] = name
            };
            if (description is not null)
            {
                document["description"] = description;
            }
            if (severity is not null)
            {
                document["severity"] = severity;
            }
            if (tags.Length > 0)
            {
                document["tags"] = tags;
            }
            document["target"] = target;
            document["assertions"] = assertions;

            var rulesDirectory = context.Layout.RulesPaths[0];
            Directory.CreateDirectory(rulesDirectory);
            var filePath = Path.Combine(rulesDirectory, $"{id.ToLowerInvariant()}.yml");
            if (File.Exists(filePath))
            {
                Console.Error.WriteLine($"'{filePath}' already exists - refusing to overwrite.");
                return Task.FromResult(1);
            }

            File.WriteAllText(filePath, RuleYamlWriter.Serialize(document));

            var report = context.ValidateRules();
            if (!report.IsValid)
            {
                Console.WriteLine();
                Console.WriteLine($"Wrote {filePath}, but it did not pass validation:");
                RuleValidationReportWriter.WriteConsole(report, Console.Out);
                return Task.FromResult(1);
            }

            Console.WriteLine();
            Console.WriteLine($"Created rule '{id}' at {filePath}.");
            Console.WriteLine($"Run 'codeguard rules explain {id}' to review it.");
            return Task.FromResult(0);
        });

        return command;
    }

    private static string PromptRequired(string? suppliedValue, string prompt)
    {
        if (!string.IsNullOrWhiteSpace(suppliedValue))
        {
            return suppliedValue.Trim();
        }

        while (true)
        {
            Console.Write(prompt);
            var input = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            Console.WriteLine("A value is required.");
        }
    }

    private static string? PromptOptional(string? suppliedValue, string prompt)
    {
        if (suppliedValue is not null)
        {
            return string.IsNullOrWhiteSpace(suppliedValue) ? null : suppliedValue.Trim();
        }

        Console.Write(prompt);
        var input = Console.ReadLine()?.Trim();
        return string.IsNullOrWhiteSpace(input) ? null : input;
    }

    private static string? PromptSeverity(string? suppliedValue, string prompt)
    {
        if (suppliedValue is not null)
        {
            return suppliedValue;
        }

        while (true)
        {
            Console.Write(prompt);
            var input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            if (AllowedSeverities.Contains(input, StringComparer.OrdinalIgnoreCase))
            {
                return input.ToLowerInvariant();
            }

            Console.WriteLine($"Severity must be one of: {string.Join(", ", AllowedSeverities)} (or blank for the default).");
        }
    }

    private static string[] PromptTags(string[] suppliedTags, string prompt)
    {
        if (suppliedTags.Length > 0)
        {
            return suppliedTags;
        }

        Console.Write(prompt);
        return SplitCommaList(Console.ReadLine());
    }

    private static string[] SplitCommaList(string? input) =>
        string.IsNullOrWhiteSpace(input)
            ? []
            : input.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static bool PromptYesNo(string prompt, bool defaultYes)
    {
        Console.Write($"{prompt} ({(defaultYes ? "Y/n" : "y/N")}): ");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input))
        {
            return defaultYes;
        }

        return input.Equals("y", StringComparison.OrdinalIgnoreCase) || input.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private static string PromptKind(string label, IReadOnlyCollection<string> knownKinds)
    {
        var sorted = knownKinds.OrderBy(k => k, StringComparer.Ordinal).ToList();
        while (true)
        {
            Console.WriteLine($"Known kinds: {string.Join(", ", sorted)}");
            Console.Write($"{label}: ");
            var input = Console.ReadLine()?.Trim();
            if (input is not null && knownKinds.Contains(input))
            {
                return input;
            }

            Console.WriteLine($"'{input}' is not a known kind.");
        }
    }

    private static Dictionary<string, object> PromptParameters()
    {
        var parameters = new Dictionary<string, object>();
        while (true)
        {
            Console.Write("Parameter name (blank to finish): ");
            var name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return parameters;
            }

            Console.Write($"Value for '{name}': ");
            var value = Console.ReadLine()?.Trim() ?? "";
            parameters[name] = value.Contains(',')
                ? value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                : value;
        }
    }

    private static Dictionary<string, object> PromptTargetSelector(IReadOnlyCollection<string> knownKinds)
    {
        var kind = PromptKind("Target selector kind", knownKinds);
        var parameters = PromptParameters();

        var target = new Dictionary<string, object> { ["kind"] = kind };
        foreach (var (key, value) in parameters)
        {
            target[key] = value;
        }

        return target;
    }
}
