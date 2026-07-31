using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Selectors;

namespace CodeGuard.Evaluation.Tests.Selectors;

public class ImplementsSelectorTests
{
    [Fact]
    public void SelectCandidates_ReturnsOnlyTypesImplementingMatchingInterface()
    {
        var handler = TestModels.Type(
            "Contoso.Application.PlaceOrderHandler",
            interfaces: ["Contoso.Application.ICommandHandler<Contoso.Application.PlaceOrderCommand>"]);
        var unrelated = TestModels.Type("Contoso.Domain.Order");

        var model = TestModels.Repository(TestModels.Project("Contoso.Application", types: [handler, unrelated]));

        var candidates = new ImplementsSelector("Contoso.Application.ICommandHandler<*>")
            .SelectCandidates(model)
            .Cast<TypeModel>()
            .ToList();

        var match = Assert.Single(candidates);
        Assert.Equal("Contoso.Application.PlaceOrderHandler", match.FullName);
    }

    [Fact]
    public void SelectCandidates_ReturnsEmpty_WhenRepositoryHasNoProjects()
    {
        var model = TestModels.Repository();

        var candidates = new ImplementsSelector("*").SelectCandidates(model).ToList();

        Assert.Empty(candidates);
    }
}
