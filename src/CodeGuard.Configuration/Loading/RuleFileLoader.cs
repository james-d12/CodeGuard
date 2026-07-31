using CodeGuard.Configuration.Parsing;
using CodeGuard.Configuration.Validation;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Configuration.Loading;

public sealed class RuleFileLoader(
    SelectorParserRegistry selectorParsers,
    AssertionParserRegistry assertionParsers,
    ConditionParserRegistry conditionParsers,
    AnalyzerParserRegistry analyzerParsers,
    RuleSchemaValidator schemaValidator)
{
    private static readonly string[] RuleFileExtensions = [".yml", ".yaml"];

    public static RuleFileLoader CreateDefault()
    {
        var selectorParsers = DefaultParsers.CreateSelectorRegistry();
        var assertionParsers = DefaultParsers.CreateAssertionRegistry(selectorParsers);
        return new(
            selectorParsers,
            assertionParsers,
            DefaultParsers.CreateConditionRegistry(assertionParsers),
            DefaultAnalyzers.CreateRegistry(),
            RuleSchemaValidator.CreateDefault());
    }

    public IReadOnlyList<RuleDefinition> LoadFromDirectory(string directoryPath) =>
        LoadFromDirectories([directoryPath]);

    public IReadOnlyList<RuleDefinition> LoadFromDirectories(IEnumerable<string> directoryPaths) =>
        LoadFromDirectoriesWithSource(directoryPaths).Select(entry => entry.Rule).ToList();

    public IReadOnlyList<(RuleDefinition Rule, string SourceFile)> LoadFromDirectoriesWithSource(
        IEnumerable<string> directoryPaths)
    {
        var report = ValidateDirectories(directoryPaths);
        if (report.Issues.Count > 0)
        {
            var issue = report.Issues[0];
            throw new RuleLoadException($"{issue.SourceFile}: {string.Join(' ', issue.Errors)}");
        }

        return report.Rules;
    }

    /// <summary>
    /// Validates every rule file under <paramref name="directoryPaths"/> and reports every problem
    /// found, rather than throwing on the first one - used by the CLI's `check-rules` command and by
    /// `validate`'s pre-flight rule-set gate. Missing directories are skipped silently, matching the
    /// existing repository-discovery behavior (`docs/done/RULE_VALIDATION_PLAN.md`).
    /// </summary>
    public RuleSetValidationReport ValidateDirectories(IEnumerable<string> directoryPaths)
    {
        var rules = new List<(RuleDefinition Rule, string SourceFile)>();
        var issues = new List<RuleFileIssue>();
        var sourceFileById = new Dictionary<string, string>();

        foreach (var file in DiscoverRuleFiles(directoryPaths))
        {
            if (!TryLoadFromFile(file, out var rule, out var errors))
            {
                issues.Add(new RuleFileIssue(file, errors));
                continue;
            }

            if (sourceFileById.TryGetValue(rule!.Id, out var existingFile))
            {
                issues.Add(new RuleFileIssue(
                    file,
                    [$"Duplicate rule id '{rule.Id}' (already defined in '{existingFile}')."]));
                continue;
            }

            sourceFileById[rule.Id] = file;
            rules.Add((rule, file));
        }

        return new RuleSetValidationReport(rules, issues);
    }

    public RuleDefinition LoadFromFile(string filePath)
    {
        var yamlText = File.ReadAllText(filePath);
        var document = YamlDocumentReader.ReadDocument(yamlText)
            ?? throw new RuleLoadException($"Rule file '{filePath}' is empty.");

        schemaValidator.Validate(document, filePath);

        return RuleDocumentParser.Parse(document.AsObject(), selectorParsers, assertionParsers, conditionParsers, analyzerParsers);
    }

    /// <summary>Non-throwing counterpart to <see cref="LoadFromFile"/>, used to build aggregate reports.</summary>
    public bool TryLoadFromFile(string filePath, out RuleDefinition? rule, out IReadOnlyList<string> errors)
    {
        try
        {
            rule = LoadFromFile(filePath);
            errors = [];
            return true;
        }
        catch (Exception ex) when (ex is RuleSchemaValidationException or RuleParsingException or RuleLoadException)
        {
            rule = null;
            errors = ex is RuleSchemaValidationException schemaEx ? schemaEx.Errors : [ex.Message];
            return false;
        }
    }

    private static IEnumerable<string> DiscoverRuleFiles(IEnumerable<string> directoryPaths)
    {
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
                yield return file;
            }
        }
    }
}
