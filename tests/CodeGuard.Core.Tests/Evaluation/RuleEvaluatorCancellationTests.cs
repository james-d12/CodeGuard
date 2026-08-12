using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Core.Evaluation;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Assertions;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Core.Tests.Evaluation;

/// <summary>
/// Covers the CancellationToken support added to RuleEvaluator.Evaluate/EvaluateRule so that
/// `codeguard validate` can actually be interrupted by Ctrl+C during rule evaluation - see
/// RuleEvaluator.cs for the corresponding ParallelOptions.CancellationToken and per-candidate
/// ThrowIfCancellationRequested() wiring.
/// </summary>
public class RuleEvaluatorCancellationTests
{
    private const string EntityBaseType = "Contoso.Domain.Entity<TId>";

    [Fact]
    public void Evaluate_Throws_WhenTokenIsAlreadyCanceled()
    {
        var model = BuildModel(entityCount: 5);
        var rules = new[] { CreateEntityInheritsRule("DDD-ENTITY-001") };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            new RuleEvaluator().Evaluate(rules, model, cancellationToken: cts.Token));
    }

    [Fact]
    public void Evaluate_StopsPartwayThrough_WhenTokenIsCanceledMidCandidateLoop()
    {
        // A single rule with many candidates so the per-candidate ThrowIfCancellationRequested()
        // check (not just Parallel.For's between-rule check) is what actually has to fire.
        var model = BuildModel(entityCount: 1000);
        using var cts = new CancellationTokenSource();
        var assertion = new CancelAfterNCandidatesAssertion(cts, cancelAfter: 10);
        var rule = new RuleDefinition
        {
            Id = "DDD-ENTITY-001",
            Name = "Domain entities must inherit from Entity",
            Target = new ClassInNamespaceSelector("Contoso.Domain.Entities"),
            Assertions = [assertion]
        };

        Assert.Throws<OperationCanceledException>(() =>
            new RuleEvaluator().Evaluate([rule], model, maxDegreeOfParallelism: 1, cancellationToken: cts.Token));

        // Proves the loop actually stopped early rather than running to completion first.
        Assert.True(assertion.CallCount < 1000);
    }

    [Fact]
    public void EvaluateRule_Throws_WhenTokenIsAlreadyCanceled()
    {
        var model = BuildModel(entityCount: 5);
        var rule = CreateEntityInheritsRule("DDD-ENTITY-001");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            new RuleEvaluator().EvaluateRule(rule, model, cts.Token));
    }

    [Fact]
    public void Evaluate_ProducesIdenticalResult_WhenTokenIsNeverCanceled()
    {
        // Regression check: adding the optional CancellationToken parameter must not change
        // behavior on the (default, non-canceled) happy path.
        var model = BuildModel(entityCount: 10);
        var rules = new[] { CreateEntityInheritsRule("DDD-ENTITY-001") };

        var withoutToken = new RuleEvaluator().Evaluate(rules, model);
        var withDefaultToken = new RuleEvaluator().Evaluate(rules, model, cancellationToken: CancellationToken.None);

        Assert.Equal(withoutToken.Status, withDefaultToken.Status);
        Assert.Equal(withoutToken.Violations.Count, withDefaultToken.Violations.Count);
    }

    private sealed class CancelAfterNCandidatesAssertion(CancellationTokenSource cts, int cancelAfter) : IAssertion
    {
        public int CallCount { get; private set; }

        public string Kind => "cancel_after_n";

        public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
        {
            CallCount++;
            if (CallCount == cancelAfter)
            {
                cts.Cancel();
            }

            return AssertionOutcome.Success();
        }
    }

    private static RuleDefinition CreateEntityInheritsRule(string id) => new()
    {
        Id = id,
        Name = id,
        Target = new ClassInNamespaceSelector("Contoso.Domain.Entities"),
        Assertions = [new MustInheritFromAssertion(EntityBaseType)]
    };

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
