using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Selectors;

namespace RulesEngine.Evaluation.Tests.Selectors;

public class InheritsFromSelectorTests
{
    [Fact]
    public void SelectCandidates_ReturnsOnlyTypesWithMatchingBaseType()
    {
        var aggregateRoot = TestModels.Type("Contoso.Domain.Order", baseType: "Contoso.Domain.Entity<TId>");
        var unrelated = TestModels.Type("Contoso.Domain.ValueObject", baseType: null);

        var model = TestModels.Repository(TestModels.Project("Contoso.Domain", types: [aggregateRoot, unrelated]));

        var candidates = new InheritsFromSelector("Contoso.Domain.Entity<TId>")
            .SelectCandidates(model)
            .Cast<TypeModel>()
            .ToList();

        var match = Assert.Single(candidates);
        Assert.Equal("Contoso.Domain.Order", match.FullName);
    }

    [Fact]
    public void SelectCandidates_ReturnsEmpty_WhenRepositoryHasNoProjects()
    {
        var model = TestModels.Repository();

        var candidates = new InheritsFromSelector("*").SelectCandidates(model).ToList();

        Assert.Empty(candidates);
    }
}
