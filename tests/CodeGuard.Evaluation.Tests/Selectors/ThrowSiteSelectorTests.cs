using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Selectors;

namespace CodeGuard.Evaluation.Tests.Selectors;

public class ThrowSiteSelectorTests
{
    private static ThrowSiteModel ThrowSite(
        string? exceptionTypeName = "System.ArgumentException",
        bool isFirstStatementInMethod = true,
        string containingType = "Contoso.Domain.Order",
        string containingMethod = "Validate",
        string projectName = "Contoso.Domain") =>
        new(containingMethod, containingType, projectName, exceptionTypeName, isFirstStatementInMethod, "Order.cs", 1);

    [Fact]
    public void SelectCandidates_FiltersByExceptionType()
    {
        var model = TestModels.RepositoryWithFacts(throwSites:
        [
            ThrowSite(exceptionTypeName: "System.ArgumentException"),
            ThrowSite(exceptionTypeName: "System.InvalidOperationException")
        ]);

        var candidates = new ThrowSiteSelector(exceptionTypePattern: "System.Argument*").SelectCandidates(model).Cast<ThrowSiteModel>().ToList();

        var match = Assert.Single(candidates);
        Assert.Equal("System.ArgumentException", match.ExceptionTypeName);
    }

    [Fact]
    public void SelectCandidates_FiltersByIsFirstStatementInMethod()
    {
        var model = TestModels.RepositoryWithFacts(throwSites:
        [
            ThrowSite(isFirstStatementInMethod: true),
            ThrowSite(isFirstStatementInMethod: false)
        ]);

        var candidates = new ThrowSiteSelector(isFirstStatementInMethod: false).SelectCandidates(model).Cast<ThrowSiteModel>().ToList();

        var match = Assert.Single(candidates);
        Assert.False(match.IsFirstStatementInMethod);
    }

    [Fact]
    public void SelectCandidates_MatchesNullExceptionType_WhenPatternIsBareWildcard()
    {
        var model = TestModels.RepositoryWithFacts(throwSites: [ThrowSite(exceptionTypeName: null)]);

        var candidates = new ThrowSiteSelector().SelectCandidates(model).ToList();

        Assert.Single(candidates);
    }

    [Fact]
    public void SelectCandidates_ReturnsEmpty_WhenRepositoryHasNoThrowSites()
    {
        var model = TestModels.RepositoryWithFacts();

        var candidates = new ThrowSiteSelector().SelectCandidates(model).ToList();

        Assert.Empty(candidates);
    }
}
