using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Core.Evaluation;
using RulesEngine.Core.Results;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.Evaluation.Selectors;
using RulesEngine.RuleModel.Rules;

namespace RulesEngine.Core.Tests.Evaluation;

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
        Standard = "DDD-001",
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
        return new RepositoryModel(".", [solution], []);
    }
}
