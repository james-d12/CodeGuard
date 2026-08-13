using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Selectors;

namespace CodeGuard.Evaluation.Tests.Selectors;

public class TypeSelectorTests
{
    [Fact]
    public void SelectCandidates_MatchesAnyKind_WhenPatternMatches()
    {
        var domainClass = TestModels.Type("Contoso.Domain.Entities.Order");
        var domainInterface = TestModels.Type("Contoso.Domain.Entities.IOrder", TypeKind.Interface);
        var other = TestModels.Type("Contoso.Application.Commands.PlaceOrder");

        var model = TestModels.Repository(TestModels.Project("Contoso.Domain", types: [domainClass, domainInterface, other]));

        var candidates = new TypeSelector("Contoso.Domain.*").SelectCandidates(model).Cast<TypeModel>().ToList();

        Assert.Equal(2, candidates.Count);
        Assert.Contains(candidates, t => t.FullName == "Contoso.Domain.Entities.Order");
        Assert.Contains(candidates, t => t.FullName == "Contoso.Domain.Entities.IOrder");
    }

    [Fact]
    public void SelectCandidates_MatchesEverything_WithDefaultWildcardPattern()
    {
        var model = TestModels.Repository(TestModels.Project("Contoso.Domain",
            types: [TestModels.Type("Contoso.Domain.Entities.Order"), TestModels.Type("Contoso.Application.PlaceOrder")]));

        var candidates = new TypeSelector().SelectCandidates(model).ToList();

        Assert.Equal(2, candidates.Count);
    }

    [Fact]
    public void SelectCandidates_ReturnsEmpty_WhenRepositoryHasNoProjects()
    {
        var model = TestModels.Repository();

        var candidates = new TypeSelector().SelectCandidates(model).ToList();

        Assert.Empty(candidates);
    }

    [Fact]
    public void SelectCandidates_FiltersByName()
    {
        var matching = TestModels.Type("Contoso.Application.Commands.PlaceOrderHandler");
        var other = TestModels.Type("Contoso.Application.Commands.PlaceOrder");
        var model = TestModels.Repository(TestModels.Project("Contoso.Application", types: [matching, other]));

        var candidates = new TypeSelector(namePattern: "*Handler").SelectCandidates(model).Cast<TypeModel>().ToList();

        var type = Assert.Single(candidates);
        Assert.Equal("Contoso.Application.Commands.PlaceOrderHandler", type.FullName);
    }

    [Fact]
    public void SelectCandidates_CombinesNamespaceAndNamePatterns_RequiringBothToMatch()
    {
        var matching = TestModels.Type("Contoso.Application.Commands.PlaceOrderHandler");
        var wrongNamespace = TestModels.Type("Contoso.Domain.Commands.PlaceOrderHandler");
        var wrongName = TestModels.Type("Contoso.Application.Commands.PlaceOrder");
        var model = TestModels.Repository(TestModels.Project("Contoso.Application", types: [matching, wrongNamespace, wrongName]));

        var candidates = new TypeSelector(namespacePattern: "Contoso.Application.*", namePattern: "*Handler").SelectCandidates(model).Cast<TypeModel>().ToList();

        var type = Assert.Single(candidates);
        Assert.Equal("Contoso.Application.Commands.PlaceOrderHandler", type.FullName);
    }
}
