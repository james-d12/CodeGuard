using RulesEngine.Configuration.Parsing;
using RulesEngine.Configuration.Validation;
using RulesEngine.RuleModel.Rules;

namespace RulesEngine.Configuration.Loading;

public sealed class RuleFileLoader(
    SelectorParserRegistry selectorParsers,
    AssertionParserRegistry assertionParsers,
    ConditionParserRegistry conditionParsers,
    RuleSchemaValidator schemaValidator)
{
    private static readonly string[] RuleFileExtensions = [".yml", ".yaml"];

    public static RuleFileLoader CreateDefault()
    {
        var assertionParsers = DefaultParsers.CreateAssertionRegistry();
        return new(
            DefaultParsers.CreateSelectorRegistry(),
            assertionParsers,
            DefaultParsers.CreateConditionRegistry(assertionParsers),
            RuleSchemaValidator.CreateDefault());
    }

    public IReadOnlyList<RuleDefinition> LoadFromDirectory(string directoryPath) =>
        LoadFromDirectories([directoryPath]);

    public IReadOnlyList<RuleDefinition> LoadFromDirectories(IEnumerable<string> directoryPaths) =>
        LoadFromDirectoriesWithSource(directoryPaths).Select(entry => entry.Rule).ToList();

    public IReadOnlyList<(RuleDefinition Rule, string SourceFile)> LoadFromDirectoriesWithSource(
        IEnumerable<string> directoryPaths)
    {
        var rules = new List<(RuleDefinition Rule, string SourceFile)>();
        var sourceFileById = new Dictionary<string, string>();

        foreach (var directoryPath in directoryPaths)
        {
            if (!Directory.Exists(directoryPath))
            {
                continue;
            }

            var files = Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
                .Where(file => RuleFileExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                .OrderBy(file => file, StringComparer.Ordinal);

            foreach (var file in files)
            {
                var rule = LoadFromFile(file);
                if (sourceFileById.TryGetValue(rule.Id, out var existingFile))
                {
                    throw new RuleLoadException(
                        $"Duplicate rule id '{rule.Id}' found in '{file}' (already defined in '{existingFile}').");
                }

                sourceFileById[rule.Id] = file;
                rules.Add((rule, file));
            }
        }

        return rules;
    }

    public RuleDefinition LoadFromFile(string filePath)
    {
        var yamlText = File.ReadAllText(filePath);
        var document = YamlDocumentReader.ReadDocument(yamlText)
            ?? throw new RuleLoadException($"Rule file '{filePath}' is empty.");

        schemaValidator.Validate(document, filePath);

        return RuleDocumentParser.Parse(document.AsObject(), selectorParsers, assertionParsers, conditionParsers);
    }
}
