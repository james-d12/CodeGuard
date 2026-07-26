using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustMatchNameAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], []);

    [Fact]
    public void Evaluate_Passes_WhenTypeNameMatchesRegex()
    {
        var type = TestModels.Type("Contoso.Domain.OrderPlacedEvent");
        var outcome = new MustMatchNameAssertion("^.+Event$").Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenTypeNameDoesNotMatchRegex()
    {
        var type = TestModels.Type("Contoso.Domain.Order");
        var outcome = new MustMatchNameAssertion("^.+Event$").Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustMatchNameAssertion("^.+$").Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_MatchesAgainstFileRelativePath()
    {
        var file = new FileModel("/repo/scripts/001_Init.sql", "scripts/001_Init.sql", ".sql");
        var outcome = new MustMatchNameAssertion(@"^scripts/\d{3}_.+\.sql$").Evaluate(file, EmptyModel);
        Assert.True(outcome.Passed);
    }
}
