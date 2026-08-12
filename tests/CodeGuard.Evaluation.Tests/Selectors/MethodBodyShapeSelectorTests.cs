using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Selectors;

namespace CodeGuard.Evaluation.Tests.Selectors;

public class MethodBodyShapeSelectorTests
{
    private static MethodBodyShapeModel Shape(
        int statementCount = 1,
        bool isSingleBaseCallDelegation = false,
        string containingType = "Contoso.Domain.Order",
        string containingMethod = "Save",
        string projectName = "Contoso.Domain") =>
        new(containingMethod, containingType, projectName, statementCount, isSingleBaseCallDelegation, "Order.cs", 1);

    [Fact]
    public void SelectCandidates_FiltersByIsSingleBaseCallDelegation()
    {
        var model = TestModels.RepositoryWithFacts(methodBodyShapes:
        [
            Shape(isSingleBaseCallDelegation: true),
            Shape(isSingleBaseCallDelegation: false)
        ]);

        var candidates = new MethodBodyShapeSelector(isSingleBaseCallDelegation: true)
            .SelectCandidates(model).Cast<MethodBodyShapeModel>().ToList();

        var match = Assert.Single(candidates);
        Assert.True(match.IsSingleBaseCallDelegation);
    }

    [Fact]
    public void SelectCandidates_FiltersByStatementCountRange()
    {
        var model = TestModels.RepositoryWithFacts(methodBodyShapes: [Shape(statementCount: 0), Shape(statementCount: 5)]);

        var candidates = new MethodBodyShapeSelector(minStatementCount: 1)
            .SelectCandidates(model).Cast<MethodBodyShapeModel>().ToList();

        var match = Assert.Single(candidates);
        Assert.Equal(5, match.StatementCount);
    }

    [Fact]
    public void SelectCandidates_ReturnsEmpty_WhenRepositoryHasNoMethodBodyShapes()
    {
        var model = TestModels.RepositoryWithFacts();

        var candidates = new MethodBodyShapeSelector().SelectCandidates(model).ToList();

        Assert.Empty(candidates);
    }
}
