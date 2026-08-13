using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Selectors;

namespace CodeGuard.Evaluation.Tests.Selectors;

public class MutationSiteSelectorTests
{
    private static MutationSiteModel MutationSite(
        string targetMemberName = "_status",
        string containingType = "Contoso.Domain.Order",
        string containingMethod = "Cancel",
        string projectName = "Contoso.Domain") =>
        new(containingMethod, containingType, targetMemberName, projectName, "Order.cs", 1);

    [Fact]
    public void SelectCandidates_FiltersByTargetMember()
    {
        var model = TestModels.RepositoryWithFacts(mutationSites:
        [
            MutationSite(targetMemberName: "_status"),
            MutationSite(targetMemberName: "_total")
        ]);

        var candidates = new MutationSiteSelector(targetMemberPattern: "_status").SelectCandidates(model).Cast<MutationSiteModel>().ToList();

        var match = Assert.Single(candidates);
        Assert.Equal("_status", match.TargetMemberName);
    }

    [Fact]
    public void SelectCandidates_FiltersByContainingMethod()
    {
        var model = TestModels.RepositoryWithFacts(mutationSites:
        [
            MutationSite(containingMethod: "Cancel"),
            MutationSite(containingMethod: "Ship")
        ]);

        var candidates = new MutationSiteSelector(containingMethodPattern: "Ship").SelectCandidates(model).ToList();

        Assert.Single(candidates);
    }

    [Fact]
    public void SelectCandidates_ReturnsEmpty_WhenRepositoryHasNoMutationSites()
    {
        var model = TestModels.RepositoryWithFacts();

        var candidates = new MutationSiteSelector().SelectCandidates(model).ToList();

        Assert.Empty(candidates);
    }
}
