using RulesEngine.Configuration.Loading;
using RulesEngine.Configuration.Parsing;
using RulesEngine.Configuration.Validation;
using RulesEngine.RuleModel.Rules;

namespace RulesEngine.Configuration.Tests;

public class RuleFileLoaderTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("rulesengine-tests-").FullName;

    private static RuleFileLoader CreateLoader()
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

    [Fact]
    public void LoadFromFile_ValidRule_ParsesAllFields()
    {
        var file = WriteRuleFile("valid.yml", """
            id: DDD-ENTITY-001
            name: Domain entities must inherit from Entity
            description: All domain entities must inherit from the approved base class.
            standard: DDD-001
            severity: error
            enforcement:
              classification: deterministic
            tags: [ddd, domain]
            remediation: Inherit from Contoso.Domain.Entity<TId>.
            illustrative: true
            target:
              kind: class
              namespace: "Contoso.Domain.Entities"
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<TId>"
            """);

        var rule = CreateLoader().LoadFromFile(file);

        Assert.Equal("DDD-ENTITY-001", rule.Id);
        Assert.Equal("Domain entities must inherit from Entity", rule.Name);
        Assert.Equal("DDD-001", rule.Standard);
        Assert.Equal(Severity.Error, rule.Severity);
        Assert.Equal(EnforcementClassification.Deterministic, rule.Enforcement.Classification);
        Assert.Equal(["ddd", "domain"], rule.Tags);
        Assert.True(rule.Illustrative);
        Assert.Single(rule.Assertions!);
    }

    [Fact]
    public void LoadFromFile_WithWhenBlock_ParsesConditionTree()
    {
        var file = WriteRuleFile("with-when.yml", """
            id: DDD-ENTITY-004
            name: Some conditional rule
            target:
              kind: class
              namespace: "Contoso.Domain.Entities"
            when:
              not:
                must_inherit_from:
                  type: "Contoso.Domain.Aggregate"
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<TId>"
            """);

        var rule = CreateLoader().LoadFromFile(file);

        Assert.NotNull(rule.When);
    }

    [Fact]
    public void LoadFromFile_WithMustExist_ParsesNestedSelector()
    {
        var file = WriteRuleFile("with-must-exist.yml", """
            id: DDD-ENTITY-005
            name: Some cardinality rule
            target:
              kind: class
              namespace: "Contoso.Domain.Exceptions"
            assertions:
              - must_exist:
                  selector:
                    kind: constructor
                    declaring_type: "${FullName}"
                    parameter_types: []
            """);

        var rule = CreateLoader().LoadFromFile(file);

        var assertion = Assert.Single(rule.Assertions!);
        Assert.Equal("must_exist", assertion.Kind);
    }

    [Fact]
    public void LoadFromFile_WithAnalyzer_ParsesAnalyzer()
    {
        var file = WriteRuleFile("with-analyzer.yml", """
            id: DDD-ENTITY-006
            name: Some analyzer-backed rule
            analyzer:
              kind: exhaustive-switch
            """);

        var rule = CreateLoader().LoadFromFile(file);

        Assert.NotNull(rule.Analyzer);
        Assert.Equal("exhaustive-switch", rule.Analyzer!.Name);
        Assert.Null(rule.Target);
        Assert.Null(rule.Assertions);
    }

    [Fact]
    public void LoadFromFile_WithUnknownAnalyzer_ThrowsRuleParsingException()
    {
        var file = WriteRuleFile("with-unknown-analyzer.yml", """
            id: DDD-ENTITY-007
            name: Some analyzer-backed rule
            analyzer:
              kind: does-not-exist
            """);

        var exception = Assert.Throws<RuleParsingException>(() => CreateLoader().LoadFromFile(file));
        Assert.Contains("does-not-exist", exception.Message);
    }

    [Fact]
    public void LoadFromFile_WithAnalyzerAndTarget_ThrowsSchemaValidationException()
    {
        // Schema-level oneOf(target+assertions, analyzer) rejects this before the parser's own
        // mutual-exclusivity check would ever run.
        var file = WriteRuleFile("with-analyzer-and-target.yml", """
            id: DDD-ENTITY-008
            name: Some rule
            analyzer:
              kind: exhaustive-switch
            target:
              kind: class
              namespace: "Contoso.Domain.Entities"
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<TId>"
            """);

        Assert.Throws<RuleSchemaValidationException>(() => CreateLoader().LoadFromFile(file));
    }

    [Fact]
    public void LoadFromFile_SchemaInvalidRule_ThrowsWithClearError()
    {
        var file = WriteRuleFile("invalid.yml", """
            id: DDD-ENTITY-001
            severity: not-a-real-severity
            target:
              kind: class
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<TId>"
            """);

        var exception = Assert.Throws<RuleSchemaValidationException>(() => CreateLoader().LoadFromFile(file));

        Assert.Contains(file, exception.DocumentSource);
        Assert.NotEmpty(exception.Errors);
    }

    [Fact]
    public void LoadFromFile_UnknownAssertionKind_ThrowsRuleParsingException()
    {
        var file = WriteRuleFile("unknown-assertion.yml", """
            id: DDD-ENTITY-002
            name: Some rule
            target:
              kind: class
              namespace: "Contoso.Domain.Entities"
            assertions:
              - must_do_something_unsupported:
                  value: "x"
            """);

        var exception = Assert.Throws<RuleParsingException>(() => CreateLoader().LoadFromFile(file));
        Assert.Contains("must_do_something_unsupported", exception.Message);
    }

    [Fact]
    public void LoadFromFile_UnknownSelectorKind_ThrowsRuleParsingException()
    {
        var file = WriteRuleFile("unknown-selector.yml", """
            id: DDD-ENTITY-003
            name: Some rule
            target:
              kind: not_a_real_kind
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<TId>"
            """);

        var exception = Assert.Throws<RuleParsingException>(() => CreateLoader().LoadFromFile(file));
        Assert.Contains("not_a_real_kind", exception.Message);
    }

    [Fact]
    public void LoadFromDirectory_DuplicateRuleId_ThrowsRuleLoadException()
    {
        WriteRuleFile("a.yml", RuleYaml("DDD-ENTITY-001"));
        WriteRuleFile("b.yml", RuleYaml("DDD-ENTITY-001"));

        var exception = Assert.Throws<RuleLoadException>(() => CreateLoader().LoadFromDirectory(_directory));
        Assert.Contains("DDD-ENTITY-001", exception.Message);
    }

    [Fact]
    public void LoadFromDirectory_MissingDirectory_ReturnsEmpty()
    {
        var rules = CreateLoader().LoadFromDirectory(Path.Combine(_directory, "does-not-exist"));
        Assert.Empty(rules);
    }

    [Fact]
    public void LoadFromDirectory_LoadsAllRuleFilesRecursively()
    {
        WriteRuleFile("a.yml", RuleYaml("DDD-ENTITY-001"));
        Directory.CreateDirectory(Path.Combine(_directory, "nested"));
        WriteRuleFile(Path.Combine("nested", "b.yml"), RuleYaml("DDD-ENTITY-002"));

        var rules = CreateLoader().LoadFromDirectory(_directory);

        Assert.Equal(2, rules.Count);
        Assert.Contains(rules, r => r.Id == "DDD-ENTITY-001");
        Assert.Contains(rules, r => r.Id == "DDD-ENTITY-002");
    }

    [Fact]
    public void LoadFromDirectoriesWithSource_TracksSourceFilePerRule()
    {
        var fileA = WriteRuleFile("a.yml", RuleYaml("DDD-ENTITY-001"));
        Directory.CreateDirectory(Path.Combine(_directory, "nested"));
        var fileB = WriteRuleFile(Path.Combine("nested", "b.yml"), RuleYaml("DDD-ENTITY-002"));

        var entries = CreateLoader().LoadFromDirectoriesWithSource([_directory]);

        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Rule.Id == "DDD-ENTITY-001" && e.SourceFile == fileA);
        Assert.Contains(entries, e => e.Rule.Id == "DDD-ENTITY-002" && e.SourceFile == fileB);
    }

    private static string RuleYaml(string id) => $"""
        id: {id}
        name: Some rule
        target:
          kind: class
          namespace: "Contoso.Domain.Entities"
        assertions:
          - must_inherit_from:
              type: "Contoso.Domain.Entity<TId>"
        """;

    private string WriteRuleFile(string relativePath, string yaml)
    {
        var path = Path.Combine(_directory, relativePath);
        File.WriteAllText(path, yaml);
        return path;
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
