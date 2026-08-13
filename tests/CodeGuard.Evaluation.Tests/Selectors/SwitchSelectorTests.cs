using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Selectors;

namespace CodeGuard.Evaluation.Tests.Selectors;

public class SwitchSelectorTests
{
    private static SwitchModel Switch(
        string containingMethod = "Handle",
        string containingType = "Contoso.Domain.Order",
        string projectName = "Contoso.Domain",
        bool hasDefaultOrDiscardArm = true) =>
        new(containingMethod, containingType, projectName, ["A", "B"], hasDefaultOrDiscardArm, "Order.cs", 1);

    [Fact]
    public void SelectCandidates_FiltersByHasDefaultOrDiscardArm()
    {
        var model = TestModels.RepositoryWithFacts(switches:
        [
            Switch(hasDefaultOrDiscardArm: true),
            Switch(hasDefaultOrDiscardArm: false)
        ]);

        var candidates = new SwitchSelector(hasDefaultOrDiscardArm: false).SelectCandidates(model).Cast<SwitchModel>().ToList();

        var match = Assert.Single(candidates);
        Assert.False(match.HasDefaultOrDiscardArm);
    }

    [Fact]
    public void SelectCandidates_FiltersByContainingType()
    {
        var model = TestModels.RepositoryWithFacts(switches:
        [
            Switch(containingType: "Contoso.Domain.Order"),
            Switch(containingType: "Contoso.Application.Handler")
        ]);

        var candidates = new SwitchSelector(containingTypePattern: "Contoso.Domain.*").SelectCandidates(model).ToList();

        Assert.Single(candidates);
    }

    [Fact]
    public void SelectCandidates_MatchesAllArms_WhenFilterIsNull()
    {
        var model = TestModels.RepositoryWithFacts(switches: [Switch(hasDefaultOrDiscardArm: true), Switch(hasDefaultOrDiscardArm: false)]);

        var candidates = new SwitchSelector().SelectCandidates(model).ToList();

        Assert.Equal(2, candidates.Count);
    }

    [Fact]
    public void SelectCandidates_ReturnsEmpty_WhenRepositoryHasNoSwitches()
    {
        var model = TestModels.RepositoryWithFacts();

        var candidates = new SwitchSelector().SelectCandidates(model).ToList();

        Assert.Empty(candidates);
    }
}
