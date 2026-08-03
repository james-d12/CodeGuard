using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

public sealed class MustHaveDirectoryAssertionTests
{
    [Fact]
    public void Evaluate_Passes_WhenDirectoryExistsInModel()
    {
        var model = new RepositoryModel("/repo", [], [], [], [], [], [], [], [], []) { Directories = ["src"] };

        var outcome = new MustHaveDirectoryAssertion("src").Evaluate(model, model);

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenDirectoryNotInModel()
    {
        var model = new RepositoryModel("/repo", [], [], [], [], [], [], [], [], []);

        var outcome = new MustHaveDirectoryAssertion("src").Evaluate(model, model);

        Assert.False(outcome.Passed);
        Assert.Equal("Repository must have a directory at 'src'.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var model = new RepositoryModel("/repo", [], [], [], [], [], [], [], [], []);

        var outcome = new MustHaveDirectoryAssertion("src").Evaluate(42, model);

        Assert.False(outcome.Passed);
        Assert.Equal("'must_have_directory' can only be evaluated against the repository.", outcome.Message);
    }
}
