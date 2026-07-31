using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

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
        Assert.Equal("'Contoso.Domain.Order' must be declared in a file named 'Order.cs' (found 'SomethingElse.cs').", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustMatchFilenameAssertion().Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'must_match_filename' can only be evaluated against types.", outcome.Message);
    }
}
