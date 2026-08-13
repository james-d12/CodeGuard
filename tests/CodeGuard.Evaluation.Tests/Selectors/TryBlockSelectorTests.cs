using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Selectors;

namespace CodeGuard.Evaluation.Tests.Selectors;

public class TryBlockSelectorTests
{
    private static TryBlockModel TryBlock(
        int catchClauseCount = 1,
        string containingType = "Contoso.Domain.Order",
        string containingMethod = "Save",
        string projectName = "Contoso.Domain") =>
        new(containingMethod, containingType, projectName, catchClauseCount, ["System.Exception"], "Order.cs", 1);

    [Fact]
    public void SelectCandidates_FiltersByMinCatchClauseCount()
    {
        var model = TestModels.RepositoryWithFacts(tryBlocks: [TryBlock(catchClauseCount: 1), TryBlock(catchClauseCount: 3)]);

        var candidates = new TryBlockSelector(minCatchClauseCount: 2).SelectCandidates(model).Cast<TryBlockModel>().ToList();

        var match = Assert.Single(candidates);
        Assert.Equal(3, match.CatchClauseCount);
    }

    [Fact]
    public void SelectCandidates_FiltersByMaxCatchClauseCount()
    {
        var model = TestModels.RepositoryWithFacts(tryBlocks: [TryBlock(catchClauseCount: 1), TryBlock(catchClauseCount: 3)]);

        var candidates = new TryBlockSelector(maxCatchClauseCount: 1).SelectCandidates(model).Cast<TryBlockModel>().ToList();

        var match = Assert.Single(candidates);
        Assert.Equal(1, match.CatchClauseCount);
    }

    [Fact]
    public void SelectCandidates_CombinesMinAndMax_AsAnInclusiveRange()
    {
        var model = TestModels.RepositoryWithFacts(tryBlocks:
        [
            TryBlock(catchClauseCount: 1),
            TryBlock(catchClauseCount: 2),
            TryBlock(catchClauseCount: 3)
        ]);

        var candidates = new TryBlockSelector(minCatchClauseCount: 2, maxCatchClauseCount: 2)
            .SelectCandidates(model).Cast<TryBlockModel>().ToList();

        var match = Assert.Single(candidates);
        Assert.Equal(2, match.CatchClauseCount);
    }

    [Fact]
    public void SelectCandidates_ReturnsEmpty_WhenRepositoryHasNoTryBlocks()
    {
        var model = TestModels.RepositoryWithFacts();

        var candidates = new TryBlockSelector().SelectCandidates(model).ToList();

        Assert.Empty(candidates);
    }
}
