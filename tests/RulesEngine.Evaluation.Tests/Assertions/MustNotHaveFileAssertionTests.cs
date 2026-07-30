using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustNotHaveFileAssertionTests
{
    private static RepositoryModel BuildModel(params FileModel[] files) => new(".", [], files, [], [], [], [], [], [], []);

    [Fact]
    public void Evaluate_Passes_WhenNoMatchingFileExists()
    {
        var model = BuildModel(new FileModel("/repo/README.md", "README.md", ".md"));
        var outcome = new MustNotHaveFileAssertion("*.user").Evaluate(model, model);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenMatchingFileExists()
    {
        var model = BuildModel(new FileModel("/repo/App.csproj.user", "App.csproj.user", ".user"));
        var outcome = new MustNotHaveFileAssertion("*.user").Evaluate(model, model);
        Assert.False(outcome.Passed);
        Assert.Equal("Repository must not have a file matching '*.user' (found 'App.csproj.user').", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var model = BuildModel();

        var outcome = new MustNotHaveFileAssertion("*.user").Evaluate(42, model);

        Assert.False(outcome.Passed);
        Assert.Equal("'must_not_have_file' can only be evaluated against the repository.", outcome.Message);
    }
}
