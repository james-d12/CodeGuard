using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustMatchFilenameAssertionTests
{
    private static readonly Analysis.AnalysisModel.RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    [Fact]
    public void Evaluate_Passes_WhenTypeNameMatchesFilename()
    {
        var type = TestModels.Type("Contoso.Domain.Order");
        var outcome = new MustMatchFilenameAssertion().Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenTypeNameDoesNotMatchFilename()
    {
        var type = TestModels.Type("Contoso.Domain.Order") with { FilePath = "SomethingElse.cs" };
        var outcome = new MustMatchFilenameAssertion().Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustMatchFilenameAssertion().Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
    }
}
