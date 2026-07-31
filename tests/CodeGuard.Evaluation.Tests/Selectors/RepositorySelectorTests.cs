using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Selectors;

namespace CodeGuard.Evaluation.Tests.Selectors;

public class RepositorySelectorTests
{
    [Fact]
    public void SelectCandidates_ReturnsTheRepositoryModelItselfAsSingleCandidate()
    {
        var model = new RepositoryModel("/repo", [], [], [], [], [], [], [], [], []);

        var candidates = new RepositorySelector().SelectCandidates(model).ToList();

        var candidate = Assert.Single(candidates);
        Assert.Same(model, candidate);
    }
}
