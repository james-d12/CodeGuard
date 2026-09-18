using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

public sealed class MustNotHaveDirectoryAssertionTests
{
    [Fact]
    public void Evaluate_Passes_WhenNoMatchingDirectoryExists()
    {
        var model = new RepositoryModel("/repo", [], [], [], [], [], [], [], [], [])
        {
            Directories = [new DirectoryModel("src", "src", "src")]
        };

        var outcome = new MustNotHaveDirectoryAssertion("Dockerfile").Evaluate(model, model);

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenMatchingDirectoryExists()
    {
        var model = new RepositoryModel("/repo", [], [], [], [], [], [], [], [], [])
        {
            Directories = [new DirectoryModel("bin", "bin", "bin")]
        };

        var outcome = new MustNotHaveDirectoryAssertion("bin").Evaluate(model, model);

        Assert.False(outcome.Passed);
        Assert.Equal("Repository must not have a directory matching 'bin' (found 'bin').", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var model = new RepositoryModel("/repo", [], [], [], [], [], [], [], [], []);

        var outcome = new MustNotHaveDirectoryAssertion("bin").Evaluate(42, model);

        Assert.False(outcome.Passed);
        Assert.Equal("'must_not_have_directory' can only be evaluated against the repository.", outcome.Message);
    }
}
