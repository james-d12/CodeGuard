using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Core.Evaluation;
using CodeGuard.Core.Results;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Analyzers;
using CodeGuard.RuleModel.Assertions;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Core.Tests.Evaluation;

public class RuleEvaluatorTests
{
    private const string EntityBaseType = "Contoso.Domain.Entity<TId>";

    [Fact]
    public void Evaluate_ReturnsPassed_WhenAllCandidatesSatisfyAssertions()
    {
        var model = BuildModel(CreateEntityType("Order", EntityBaseType));
        var rules = new[] { CreateEntityInheritsRule() };

        var result = new RuleEvaluator().Evaluate(rules, model);

        Assert.Equal(ValidationStatus.Passed, result.Status);
        Assert.Equal(1, result.RulesEvaluated);
        Assert.Equal(1, result.RulesPassed);
        Assert.Equal(0, result.RulesFailed);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void Evaluate_ReturnsFailed_AndProducesViolation_WhenACandidateFailsAnAssertion()
    {
        var model = BuildModel(CreateEntityType("LegacyThing", baseType: null));
        var rules = new[] { CreateEntityInheritsRule() };

        var result = new RuleEvaluator().Evaluate(rules, model);

        Assert.Equal(ValidationStatus.Failed, result.Status);
        Assert.Equal(0, result.RulesPassed);
        Assert.Equal(1, result.RulesFailed);
        var violation = Assert.Single(result.Violations);
        Assert.Equal("DDD-ENTITY-001", violation.RuleId);
        Assert.Equal(Severity.Error, violation.Severity);
        Assert.Equal("LegacyThing.cs", violation.File);
    }

    [Fact]
    public void Evaluate_SkipsDisabledRules()
    {
        var model = BuildModel(CreateEntityType("LegacyThing", baseType: null));
        var disabledRule = new RuleDefinition
        {
            Id = "DDD-ENTITY-001",
            Name = "Domain entities must inherit from Entity",
            Enabled = false,
            Target = new ClassInNamespaceSelector("Contoso.Domain.Entities"),
            Assertions = [new MustInheritFromAssertion(EntityBaseType)]
        };

        var result = new RuleEvaluator().Evaluate([disabledRule], model);

        Assert.Equal(0, result.RulesEvaluated);
        Assert.Empty(result.Violations);
    }

    private static RuleDefinition CreateEntityInheritsRule() => new()
    {
        Id = "DDD-ENTITY-001",
        Name = "Domain entities must inherit from Entity",
        Severity = Severity.Error,
        Enforcement = new EnforcementMetadata { Classification = EnforcementClassification.Deterministic },
        Remediation = $"Inherit from {EntityBaseType}.",
        Illustrative = true,
        Target = new ClassInNamespaceSelector("Contoso.Domain.Entities"),
        Assertions = [new MustInheritFromAssertion(EntityBaseType)]
    };

    private static TypeModel CreateEntityType(string name, string? baseType) => new(
        Name: name,
        FullName: $"Contoso.Domain.Entities.{name}",
        Namespace: "Contoso.Domain.Entities",
        Kind: TypeKind.Class,
        BaseType: baseType,
        Interfaces: [],
        Accessibility: Accessibility.Public,
        Modifiers: TypeModifiers.None,
        Attributes: [],
        Methods: [],
        Properties: [],
        Constructors: [],
        Fields: [],
        ProjectName: "Contoso.Domain",
        FilePath: $"{name}.cs",
        Line: 1,
        Column: 1);

    private static RepositoryModel BuildModel(params TypeModel[] types)
    {
        var project = new ProjectModel(
            "Contoso.Domain", "Contoso.Domain.csproj", "net10.0", "Microsoft.NET.Sdk",
            [], [], new Dictionary<string, string>(), types);
        var solution = new SolutionModel("Contoso.sln", [project]);
        return new RepositoryModel(".", [solution], [], [], [], [], [], [], [], []);
    }

    private sealed class AlwaysFailsAssertion : IAssertion
    {
        public string Kind => "always_fails";

        public AssertionOutcome Evaluate(object candidate, RepositoryModel model) =>
            AssertionOutcome.Failure("always fails");
    }

    [Fact]
    public void Evaluate_ExtractsLocation_ForFileCandidate()
    {
        var file = new FileModel("/repo/.editorconfig", ".editorconfig", "");
        var model = new RepositoryModel("/repo", [], [file], [], [], [], [], [], [], []);
        var rule = new RuleDefinition
        {
            Id = "FILE-001",
            Name = "File rule",
            Target = new FileSelector("*.editorconfig"),
            Assertions = [new AlwaysFailsAssertion()]
        };

        var result = new RuleEvaluator().Evaluate([rule], model);

        var violation = Assert.Single(result.Violations);
        Assert.Equal("/repo/.editorconfig", violation.File);
        Assert.Equal(".editorconfig", violation.Symbol);
    }

    [Fact]
    public void Evaluate_ExtractsLocation_ForRepositoryCandidate()
    {
        var model = new RepositoryModel("/repo", [], [], [], [], [], [], [], [], []);
        var rule = new RuleDefinition
        {
            Id = "REPO-001",
            Name = "Repository rule",
            Target = new RepositorySelector(),
            Assertions = [new AlwaysFailsAssertion()]
        };

        var result = new RuleEvaluator().Evaluate([rule], model);

        var violation = Assert.Single(result.Violations);
        Assert.Equal("/repo", violation.File);
    }

    [Fact]
    public void Evaluate_ExtractsLocation_ForMethodCandidate()
    {
        var method = new MethodModel(
            "Save", "System.Void", [], Accessibility.Public, MethodModifiers.None,
            [], "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 5, 3);
        var type = CreateEntityType("Order", EntityBaseType) with { Methods = [method] };
        var model = BuildModel(type);
        var rule = new RuleDefinition
        {
            Id = "METHOD-001",
            Name = "Method rule",
            Target = new MethodSelector(),
            Assertions = [new AlwaysFailsAssertion()]
        };

        var result = new RuleEvaluator().Evaluate([rule], model);

        var violation = Assert.Single(result.Violations);
        Assert.Equal("Order.cs", violation.File);
        Assert.Equal(5, violation.Line);
        Assert.Equal("Contoso.Domain.Order.Save", violation.Symbol);
    }

    [Fact]
    public void Evaluate_ExtractsLocation_ForFieldCandidate()
    {
        var field = new FieldModel(
            "_id", "System.Guid", Accessibility.Private, FieldModifiers.Readonly,
            [], "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 7, 5);
        var type = CreateEntityType("Order", EntityBaseType) with { Fields = [field] };
        var model = BuildModel(type);
        var rule = new RuleDefinition
        {
            Id = "FIELD-001",
            Name = "Field rule",
            Target = new FieldSelector(),
            Assertions = [new AlwaysFailsAssertion()]
        };

        var result = new RuleEvaluator().Evaluate([rule], model);

        var violation = Assert.Single(result.Violations);
        Assert.Equal("Order.cs", violation.File);
        Assert.Equal(7, violation.Line);
    }

    private sealed class StubAnalyzer(string name, params AnalyzerViolation[] violations) : ICustomAnalyzer
    {
        public string Name => name;
        public IEnumerable<AnalyzerViolation> Analyze(RepositoryModel model) => violations;
    }

    [Fact]
    public void Evaluate_RunsAnalyzerBranch_WhenRuleHasAnalyzer()
    {
        var model = new RepositoryModel(".", [], [], [], [], [], [], [], [], []);
        var analyzer = new StubAnalyzer("stub-analyzer", new AnalyzerViolation("bad shape", "Foo.cs", 3, 1));
        var rule = new RuleDefinition { Id = "ANALYZER-001", Name = "Analyzer rule", Analyzer = analyzer };

        var result = new RuleEvaluator().Evaluate([rule], model);

        Assert.Equal(ValidationStatus.Failed, result.Status);
        var violation = Assert.Single(result.Violations);
        Assert.Equal("Foo.cs", violation.File);
        Assert.Equal(3, violation.Line);
    }

    [Fact]
    public void Evaluate_AnalyzerRule_PassesWhenNoViolationsProduced()
    {
        var model = new RepositoryModel(".", [], [], [], [], [], [], [], [], []);
        var analyzer = new StubAnalyzer("stub-analyzer");
        var rule = new RuleDefinition { Id = "ANALYZER-002", Name = "Analyzer rule", Analyzer = analyzer };

        var result = new RuleEvaluator().Evaluate([rule], model);

        Assert.Equal(ValidationStatus.Passed, result.Status);
        Assert.Equal(1, result.RulesPassed);
    }
}
