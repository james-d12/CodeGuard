using System.CommandLine;
using CodeGuard.Cli.Support;
using CodeGuard.Configuration.Parsing;
using CodeGuard.Configuration.Writing;
using Microsoft.Extensions.Logging;
using CodeGuard.Configuration.Capabilities;

namespace CodeGuard.Cli.Commands.Rules;

/// <summary>
/// Interactively scaffolds a new rule YAML file. Deliberately doesn't hardcode per-selector/
/// per-assertion parameter shapes (there are 21 target selector kinds and 45 assertion kinds in
/// <see cref="DefaultParsers"/>, each with different parameter names) - instead drives the prompts
/// off the registries' <see cref="CapabilityDescriptor"/>s, so new kinds and their parameters are
/// picked up automatically. Only authors the `target`+`assertions` rule shape, not the
/// `analyzer`-referencing shape.
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
        var verbosityOption = CommonOptions.CreateVerbosityOption();

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
        command.Add(verbosityOption);
        command.Add(idOption);
        command.Add(nameOption);
        command.Add(descriptionOption);
        command.Add(severityOption);
        command.Add(tagOption);

        command.SetAction((parseResult, _) =>
        {
            using var loggerFactory = CliLoggerFactory.Create(CliLoggerFactory.ParseVerbosity(parseResult.GetValue(verbosityOption)!));
            var logger = loggerFactory.CreateLogger(typeof(CreateCommand));

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

            try
            {
                return Task.FromResult(RunInteractive(parseResult, context, logger, idOption, nameOption, descriptionOption, severityOption, tagOption));
            }
            catch (EndOfInputException)
            {
                Console.Error.WriteLine("Unexpected end of input while prompting - stopping.");
                return Task.FromResult(1);
            }
        });

        return command;
    }

    private static int RunInteractive(
        ParseResult parseResult,
        CliRepositoryContext context,
        ILogger logger,
        Option<string?> idOption,
        Option<string?> nameOption,
        Option<string?> descriptionOption,
        Option<string?> severityOption,
        Option<string[]> tagOption)
    {
        var id = PromptRequired(parseResult.GetValue(idOption), "Rule ID (e.g. DDD-ENTITY-003): ");
        var name = PromptRequired(parseResult.GetValue(nameOption), "Rule name: ");
        var description = PromptOptional(parseResult.GetValue(descriptionOption), "Description (optional, blank to skip): ");
        var severity = PromptSeverity(parseResult.GetValue(severityOption), "Severity - info/warning/error/critical (blank = warning): ");
        var tags = PromptTags(parseResult.GetValue(tagOption) ?? [], "Tags, comma-separated (optional, blank to skip): ");

        var selectorRegistry = DefaultParsers.CreateSelectorRegistry();
        var assertionRegistry = DefaultParsers.CreateAssertionRegistry(selectorRegistry);

        Console.WriteLine();
        Console.WriteLine("--- Target selector ---");
        var target = PromptTargetSelector(selectorRegistry.Descriptors);

        Console.WriteLine();
        Console.WriteLine("--- Assertions ---");
        var assertions = new List<object>();
        do
        {
            var assertionDescriptor = PromptKind("Assertion kind", assertionRegistry.Descriptors);
            var assertionParameters = PromptParameters(assertionDescriptor);
            assertions.Add(new Dictionary<string, object> { [assertionDescriptor.Kind] = assertionParameters });
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
            return 1;
        }

        File.WriteAllText(filePath, RuleYamlWriter.Serialize(document));
        logger.LogInformation("Created rule file {FilePath} (id={RuleId})", filePath, id);

        var report = context.ValidateRules();
        if (!report.IsValid)
        {
            logger.LogWarning("Created rule {RuleId} at {FilePath} failed validation: {IssueCount} issue(s)", id, filePath, report.Issues.Count);
            Console.WriteLine();
            Console.WriteLine($"Wrote {filePath}, but it did not pass validation:");
            RuleValidationReportWriter.WriteConsole(report, Console.Out);
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine($"Created rule '{id}' at {filePath}.");
        Console.WriteLine($"Run 'codeguard rules explain {id}' to review it.");
        return 0;
    }

    /// <summary>
    /// Thrown when a prompt hits end-of-input (<see cref="Console.ReadLine"/> returns <c>null</c>)
    /// instead of looping forever re-issuing the same prompt - e.g. piped/scripted input that runs
    /// out before every required answer is given.
    /// </summary>
    private sealed class EndOfInputException : Exception;

    private static string ReadLineOrThrow() => Console.ReadLine() ?? throw new EndOfInputException();

    private static string PromptRequired(string? suppliedValue, string prompt)
    {
        if (!string.IsNullOrWhiteSpace(suppliedValue))
        {
            return suppliedValue.Trim();
        }

        while (true)
        {
            Console.Write(prompt);
            var input = ReadLineOrThrow().Trim();
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
        var input = ReadLineOrThrow().Trim();
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
            var input = ReadLineOrThrow().Trim();
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
        return SplitCommaList(ReadLineOrThrow());
    }

    private static string[] SplitCommaList(string? input) =>
        string.IsNullOrWhiteSpace(input)
            ? []
            : input.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static bool PromptYesNo(string prompt, bool defaultYes)
    {
        Console.Write($"{prompt} ({(defaultYes ? "Y/n" : "y/N")}): ");
        var input = ReadLineOrThrow().Trim();
        if (string.IsNullOrEmpty(input))
        {
            return defaultYes;
        }

        return input.Equals("y", StringComparison.OrdinalIgnoreCase) || input.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private static CapabilityDescriptor PromptKind(string label, IReadOnlyList<CapabilityDescriptor> descriptors)
    {
        var byKind = descriptors.ToDictionary(d => d.Kind, StringComparer.Ordinal);
        while (true)
        {
            Console.WriteLine($"Known kinds: {string.Join(", ", byKind.Keys.Order(StringComparer.Ordinal))}");
            Console.Write($"{label}: ");
            var input = ReadLineOrThrow().Trim();
            if (byKind.TryGetValue(input, out var descriptor))
            {
                return descriptor;
            }

            Console.WriteLine($"'{input}' is not a known kind.{KindSuggestionHint(input, byKind.Keys)}");
        }
    }

    private static string KindSuggestionHint(string? input, IEnumerable<string> knownKinds) =>
        string.IsNullOrEmpty(input)
            ? ""
            : knownKinds.FirstOrDefault(k => k.Contains(input, StringComparison.OrdinalIgnoreCase)) is { } near
                ? $" Did you mean '{near}'?"
                : "";

    /// <summary>
    /// Prompts for the chosen kind's declared parameters by name, then allows any extras. Required
    /// parameters are re-prompted until answered, since omitting one makes the rule fail to parse.
    /// </summary>
    private static Dictionary<string, object> PromptParameters(CapabilityDescriptor descriptor)
    {
        Console.WriteLine($"  {descriptor.Summary}");
        var parameters = new Dictionary<string, object>();

        foreach (var parameter in descriptor.Parameters)
        {
            var hint = parameter.Required ? "required" : parameter.Default is { } d ? $"optional, default {d}" : "optional";
            if (parameter.AllowedValues is { Count: > 0 } allowed)
            {
                hint += $"; one of {string.Join(", ", allowed)}";
            }

            while (true)
            {
                Console.Write($"  {parameter.Name} ({hint}): ");
                var value = ReadLineOrThrow().Trim();
                if (value.Length > 0)
                {
                    parameters[parameter.Name] = ParseValue(value);
                    break;
                }

                if (!parameter.Required)
                {
                    break;
                }

                Console.WriteLine($"  '{parameter.Name}' is required.");
            }
        }

        // Nested selector/assertion parameters can't be prompted for meaningfully, and a kind may
        // gain a parameter this build doesn't know, so the free-form loop stays available.
        while (true)
        {
            Console.Write("Additional parameter name (blank to finish): ");
            var name = ReadLineOrThrow().Trim();
            if (string.IsNullOrEmpty(name))
            {
                return parameters;
            }

            Console.Write($"Value for '{name}': ");
            parameters[name] = ParseValue(ReadLineOrThrow().Trim());
        }
    }

    private static object ParseValue(string value) =>
        value.Contains(',')
            ? value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : value;

    private static Dictionary<string, object> PromptTargetSelector(IReadOnlyList<CapabilityDescriptor> descriptors)
    {
        var descriptor = PromptKind("Target selector kind", descriptors);
        var parameters = PromptParameters(descriptor);

        var target = new Dictionary<string, object> { ["kind"] = descriptor.Kind };
        foreach (var (key, value) in parameters)
        {
            target[key] = value;
        }

        return target;
    }
}
