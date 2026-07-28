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
    }
}
