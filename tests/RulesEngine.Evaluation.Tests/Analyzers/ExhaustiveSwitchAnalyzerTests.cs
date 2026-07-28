using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Analyzers;

namespace RulesEngine.Evaluation.Tests.Analyzers;

public class ExhaustiveSwitchAnalyzerTests
{
    private static SwitchModel Switch(bool hasDefaultOrDiscardArm, string containingType = "Contoso.Domain.OrderEventMapper") => new(
        ContainingMethod: "Handle",
        ContainingType: containingType,
        ProjectName: "Contoso.Domain",
        ArmLabels: ["Created", "Shipped"],
        HasDefaultOrDiscardArm: hasDefaultOrDiscardArm,
        FilePath: "OrderEventMapper.cs",
        Line: 10);

    [Fact]
    public void Analyze_Flags_SwitchInScopeWithNoDefaultOrDiscardArm()
    {
        var model = TestModels.RepositoryWithFacts(switches: [Switch(hasDefaultOrDiscardArm: false)]);
        var analyzer = new ExhaustiveSwitchAnalyzer("*");

        var violations = analyzer.Analyze(model).ToList();

        var violation = Assert.Single(violations);
        Assert.Equal("OrderEventMapper.cs", violation.FilePath);
        Assert.Equal(10, violation.Line);
        Assert.Contains("Contoso.Domain.OrderEventMapper.Handle", violation.Message);
    }

    [Fact]
    public void Analyze_DoesNotFlag_SwitchWithDefaultOrDiscardArm()
    {
        var model = TestModels.RepositoryWithFacts(switches: [Switch(hasDefaultOrDiscardArm: true)]);
        var analyzer = new ExhaustiveSwitchAnalyzer("*");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_DoesNotFlag_SwitchOutsideConfiguredNamespace()
    {
        var model = TestModels.RepositoryWithFacts(
            switches: [Switch(hasDefaultOrDiscardArm: false, containingType: "Contoso.Application.OrderEventMapper")]);
        var analyzer = new ExhaustiveSwitchAnalyzer("Contoso.Domain.*");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Name_IsExhaustiveSwitch()
    {
        var analyzer = new ExhaustiveSwitchAnalyzer("*");

        Assert.Equal("exhaustive-switch", analyzer.Name);
    }
}
