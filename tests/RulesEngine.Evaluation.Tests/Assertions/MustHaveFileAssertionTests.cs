using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustHaveFileAssertionTests
{
    private static RepositoryModel BuildModel(params FileModel[] files) => new(".", [], files);

    [Fact]
    public void Evaluate_Passes_WhenMatchingFileExists()
    {
        var model = BuildModel(new FileModel("/repo/.editorconfig", ".editorconfig", ""));
        var outcome = new MustHaveFileAssertion(".editorconfig").Evaluate(model, model);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenNoMatchingFileExists()
    {
        var model = BuildModel(new FileModel("/repo/README.md", "README.md", ".md"));
        var outcome = new MustHaveFileAssertion(".editorconfig").Evaluate(model, model);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var model = BuildModel();
        var outcome = new MustHaveFileAssertion(".editorconfig").Evaluate("not-a-repository", model);
        Assert.False(outcome.Passed);
    }
}
