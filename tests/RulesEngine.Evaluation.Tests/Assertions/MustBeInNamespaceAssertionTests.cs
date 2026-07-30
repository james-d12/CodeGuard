using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustBeInNamespaceAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    [Fact]
    public void Evaluate_Passes_WhenNamespaceMatches()
    {
        var type = TestModels.Type("Contoso.Domain.Events.OrderPlaced");
        var outcome = new MustBeInNamespaceAssertion("*.Domain.Events").Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenNamespaceDoesNotMatch()
    {
        var type = TestModels.Type("Contoso.Application.Events.OrderPlaced");
        var outcome = new MustBeInNamespaceAssertion("*.Domain.Events").Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'Contoso.Application.Events.OrderPlaced' must be in a namespace matching '*.Domain.Events'.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustBeInNamespaceAssertion("*").Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'must_be_in_namespace' can only be evaluated against types.", outcome.Message);
    }
}
