using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public sealed class MustHaveDirectoryAssertionTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("rulesengine-directory-tests-").FullName;

    [Fact]
    public void Evaluate_Passes_WhenDirectoryExists()
    {
        Directory.CreateDirectory(Path.Combine(_root, "src"));
        var model = new RepositoryModel(_root, [], [], [], [], [], [], [], [], []);

        var outcome = new MustHaveDirectoryAssertion("src").Evaluate(model, model);

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenDirectoryDoesNotExist()
    {
        var model = new RepositoryModel(_root, [], [], [], [], [], [], [], [], []);

        var outcome = new MustHaveDirectoryAssertion("src").Evaluate(model, model);

        Assert.False(outcome.Passed);
        Assert.Equal("Repository must have a directory at 'src'.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var model = new RepositoryModel(_root, [], [], [], [], [], [], [], [], []);

        var outcome = new MustHaveDirectoryAssertion("src").Evaluate(42, model);

        Assert.False(outcome.Passed);
        Assert.Equal("'must_have_directory' can only be evaluated against the repository.", outcome.Message);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
