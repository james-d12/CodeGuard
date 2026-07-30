using RulesEngine.Evaluation.Analyzers;

namespace RulesEngine.Evaluation.Tests.Analyzers;

public class CompanionTypeCardinalityAnalyzerTests
{
    private const string MarkerInterface = "Contoso.Domain.IAggregateRoot";
    private const string CompanionSuffix = "PartitionKeyBuilder";

    [Fact]
    public void Analyze_DoesNotFlag_MarkerTypeWithExactlyOneCompanion()
    {
        var order = TestModels.Type("Contoso.Domain.Order", interfaces: [MarkerInterface]);
        var companion = TestModels.Type("Contoso.Persistence.OrderPartitionKeyBuilder");
        var project = TestModels.Project("Contoso.Domain", types: [order, companion]);
        var model = TestModels.RepositoryWithFacts(projects: [project]);
        var analyzer = new CompanionTypeCardinalityAnalyzer(MarkerInterface, CompanionSuffix);

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_Flags_MarkerTypeWithNoCompanion()
    {
        var order = TestModels.Type("Contoso.Domain.Order", interfaces: [MarkerInterface]);
        var project = TestModels.Project("Contoso.Domain", types: [order]);
        var model = TestModels.RepositoryWithFacts(projects: [project]);
        var analyzer = new CompanionTypeCardinalityAnalyzer(MarkerInterface, CompanionSuffix);

        var violations = analyzer.Analyze(model).ToList();

        var violation = Assert.Single(violations);
        Assert.Contains("Contoso.Domain.Order", violation.Message);
        Assert.Contains("0 type(s)", violation.Message);
    }

    [Fact]
    public void Analyze_Flags_MarkerTypeWithMultipleCompanions()
    {
        var order = TestModels.Type("Contoso.Domain.Order", interfaces: [MarkerInterface]);
        var companion1 = TestModels.Type("Contoso.Persistence.OrderPartitionKeyBuilder", projectName: "Contoso.Persistence");
        var companion2 = TestModels.Type("Contoso.Reporting.OrderPartitionKeyBuilder", projectName: "Contoso.Reporting");
        var project = TestModels.Project("Contoso.Domain", types: [order, companion1, companion2]);
        var model = TestModels.RepositoryWithFacts(projects: [project]);
        var analyzer = new CompanionTypeCardinalityAnalyzer(MarkerInterface, CompanionSuffix);

        var violations = analyzer.Analyze(model).ToList();

        var violation = Assert.Single(violations);
        Assert.Contains("2 type(s)", violation.Message);
    }

    [Fact]
    public void Analyze_DoesNotFlag_TypeNotImplementingMarkerInterface()
    {
        var helper = TestModels.Type("Contoso.Domain.OrderHelper");
        var project = TestModels.Project("Contoso.Domain", types: [helper]);
        var model = TestModels.RepositoryWithFacts(projects: [project]);
        var analyzer = new CompanionTypeCardinalityAnalyzer(MarkerInterface, CompanionSuffix);

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_SupportsDifferentSuffixConventions()
    {
        var command = TestModels.Type("Contoso.Application.PlaceOrderCommand", interfaces: ["Contoso.Application.ICommand"]);
        var validator = TestModels.Type("Contoso.Application.PlaceOrderCommandValidator");
        var project = TestModels.Project("Contoso.Application", types: [command, validator]);
        var model = TestModels.RepositoryWithFacts(projects: [project]);
        var analyzer = new CompanionTypeCardinalityAnalyzer("Contoso.Application.ICommand", "Validator");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Name_IsCompanionTypeCardinality()
    {
        var analyzer = new CompanionTypeCardinalityAnalyzer(MarkerInterface, CompanionSuffix);

        Assert.Equal("companion-type-cardinality", analyzer.Name);
    }
}
