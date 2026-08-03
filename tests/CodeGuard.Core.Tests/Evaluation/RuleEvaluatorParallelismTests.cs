using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Core.Evaluation;
using CodeGuard.Core.Results;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Assertions;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Core.Tests.Evaluation;

/// <summary>
/// RuleEvaluator.Evaluate parallelizes across rules internally (see RuleEvaluator.cs) - these tests
/// exist to catch regressions in the deterministic-fold-after-parallel-compute pattern it relies on,
/// not to test rule-evaluation semantics (that's RuleEvaluatorTests).
/// </summary>
public class RuleEvaluatorParallelismTests
{
    private const string EntityBaseType = "Contoso.Domain.Entity<TId>";

    [Fact]
    public void Evaluate_ProducesIdenticalOutput_RegardlessOfMaxDegreeOfParallelism()
    {
        var model = BuildModel(entityCount: 40);
        var rules = BuildRules(count: 50);

        var sequential = new RuleEvaluator().Evaluate(rules, model, maxDegreeOfParallelism: 1);
        var parallel = new RuleEvaluator().Evaluate(rules, model, maxDegreeOfParallelism: 8);

        AssertIdenticalResults(sequential, parallel);
    }

    [Fact]
    public void Evaluate_IsStableAcrossManyRepeatedRuns()
    {
        var model = BuildModel(entityCount: 40);
        var rules = BuildRules(count: 50);

        var baseline = new RuleEvaluator().Evaluate(rules, model);

        for (var i = 0; i < 50; i++)
        {
            var result = new RuleEvaluator().Evaluate(rules, model);
            AssertIdenticalResults(baseline, result);
        }
    }

    private static void AssertIdenticalResults(ValidationResult expected, ValidationResult actual)
    {
        Assert.Equal(expected.RulesEvaluated, actual.RulesEvaluated);
        Assert.Equal(expected.RulesPassed, actual.RulesPassed);
        Assert.Equal(expected.RulesFailed, actual.RulesFailed);
        Assert.Equal(expected.RulesErrored, actual.RulesErrored);
        Assert.Equal(expected.Status, actual.Status);

        Assert.Equal(
            expected.Violations.Select(v => (v.RuleId, v.Symbol, v.File, v.Message)).ToList(),
            actual.Violations.Select(v => (v.RuleId, v.Symbol, v.File, v.Message)).ToList());

        Assert.Equal(
            expected.EvaluationErrors.Select(e => (e.RuleId, e.ExceptionType, e.Message)).ToList(),
            actual.EvaluationErrors.Select(e => (e.RuleId, e.ExceptionType, e.Message)).ToList());
    }

    // Every 10th rule deterministically throws, so the mix of pass/fail/error results exercises all
    // three RuleOutcome branches of the fold-after-parallel-compute step.
    private static IReadOnlyList<RuleDefinition> BuildRules(int count) =>
        Enumerable.Range(0, count)
            .Select(i => i % 10 == 9 ? CreateThrowingRule($"THROW-{i:000}") : CreateEntityInheritsRule($"ENTITY-{i:000}"))
            .ToList();

    private static RuleDefinition CreateEntityInheritsRule(string id) => new()
    {
        Id = id,
        Name = id,
        Target = new ClassInNamespaceSelector("Contoso.Domain.Entities"),
        Assertions = [new MustInheritFromAssertion(EntityBaseType)]
    };

    private static RuleDefinition CreateThrowingRule(string id) => new()
    {
        Id = id,
        Name = id,
        Target = new ClassInNamespaceSelector("Contoso.Domain.Entities"),
        Assertions = [new AlwaysThrowsAssertion()]
    };

    private sealed class AlwaysThrowsAssertion : IAssertion
    {
        public string Kind => "always_throws";

        public AssertionOutcome Evaluate(object candidate, RepositoryModel model) =>
            throw new InvalidOperationException("deterministic failure for test");
    }

    private static RepositoryModel BuildModel(int entityCount)
    {
        var types = Enumerable.Range(0, entityCount)
            .Select(i => CreateEntityType($"Entity{i}", baseType: i % 2 == 0 ? EntityBaseType : null))
            .ToList();

        var project = new ProjectModel(
            "Contoso.Domain", "Contoso.Domain.csproj", "net10.0", "Microsoft.NET.Sdk",
            [], [], new Dictionary<string, string>(), types);
        var solution = new SolutionModel("Contoso.sln", [project]);
        return new RepositoryModel(".", [solution], [], [], [], [], [], [], [], []);
    }

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
}
