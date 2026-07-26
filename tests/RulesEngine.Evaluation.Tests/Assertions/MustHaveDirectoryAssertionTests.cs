using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustHaveDirectoryAssertionTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("rulesengine-directory-tests-").FullName;

    [Fact]
    public void Evaluate_Passes_WhenDirectoryExists()
    {
        Directory.CreateDirectory(Path.Combine(_root, "src"));
        var model = new RepositoryModel(_root, [], []);

        var outcome = new MustHaveDirectoryAssertion("src").Evaluate(model, model);

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenDirectoryDoesNotExist()
    {
        var model = new RepositoryModel(_root, [], []);

        var outcome = new MustHaveDirectoryAssertion("src").Evaluate(model, model);

        Assert.False(outcome.Passed);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
