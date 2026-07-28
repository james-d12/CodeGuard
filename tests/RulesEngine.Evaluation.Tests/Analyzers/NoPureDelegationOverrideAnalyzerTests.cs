using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Analyzers;

namespace RulesEngine.Evaluation.Tests.Analyzers;

public class NoPureDelegationOverrideAnalyzerTests
{
    private static MethodBodyShapeModel Shape(string containingType, bool isSingleBaseCallDelegation) => new(
        ContainingMethod: "Add",
        ContainingType: containingType,
        ProjectName: "Contoso.Persistence",
        StatementCount: 1,
        IsSingleBaseCallDelegation: isSingleBaseCallDelegation,
        FilePath: "OrderRepository.cs",
        Line: 12);

    [Fact]
    public void Analyze_Flags_PureDelegationOverrideOnMatchingBaseType()
    {
        var type = TestModels.Type("Contoso.Persistence.OrderRepository", baseType: "Contoso.Persistence.DomainEntityRepository<Order>");
        var project = TestModels.Project("Contoso.Persistence", types: [type]);
        var model = TestModels.RepositoryWithFacts(
            projects: [project],
            methodBodyShapes: [Shape("Contoso.Persistence.OrderRepository", isSingleBaseCallDelegation: true)]);
        var analyzer = new NoPureDelegationOverrideAnalyzer("*DomainEntityRepository*");

        var violations = analyzer.Analyze(model).ToList();

        var violation = Assert.Single(violations);
        Assert.Equal("OrderRepository.cs", violation.FilePath);
        Assert.Contains("Contoso.Persistence.OrderRepository.Add", violation.Message);
    }

    [Fact]
    public void Analyze_DoesNotFlag_NonDelegationOverride()
    {
        var type = TestModels.Type("Contoso.Persistence.OrderRepository", baseType: "Contoso.Persistence.DomainEntityRepository<Order>");
        var project = TestModels.Project("Contoso.Persistence", types: [type]);
        var model = TestModels.RepositoryWithFacts(
            projects: [project],
            methodBodyShapes: [Shape("Contoso.Persistence.OrderRepository", isSingleBaseCallDelegation: false)]);
        var analyzer = new NoPureDelegationOverrideAnalyzer("*DomainEntityRepository*");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_DoesNotFlag_DelegationOverrideOnNonMatchingBaseType()
    {
        var type = TestModels.Type("Contoso.Persistence.OrderCache", baseType: "System.Object");
        var project = TestModels.Project("Contoso.Persistence", types: [type]);
        var model = TestModels.RepositoryWithFacts(
            projects: [project],
            methodBodyShapes: [Shape("Contoso.Persistence.OrderCache", isSingleBaseCallDelegation: true)]);
        var analyzer = new NoPureDelegationOverrideAnalyzer("*DomainEntityRepository*");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Name_IsNoPureDelegationOverride()
    {
        var analyzer = new NoPureDelegationOverrideAnalyzer("*");

        Assert.Equal("no-pure-delegation-override", analyzer.Name);
    }
}
