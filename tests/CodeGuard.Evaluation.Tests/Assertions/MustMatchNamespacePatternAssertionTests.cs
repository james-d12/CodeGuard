using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

public class MustMatchNamespacePatternAssertionTests
{
    [Fact]
    public void Evaluate_Passes_WhenNamespaceMatchesRegex()
    {
        var type = TestModels.Type("Contoso.Domain.Events.OrderPlaced");

        var outcome = new MustMatchNamespacePatternAssertion(@"\.Events$").Evaluate(type, TestModels.Repository());

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenNamespaceDoesNotMatchRegex()
    {
        var type = TestModels.Type("Contoso.Domain.Commands.PlaceOrder");

        var outcome = new MustMatchNamespacePatternAssertion(@"\.Events$").Evaluate(type, TestModels.Repository());

        Assert.False(outcome.Passed);
        Assert.Equal("'Contoso.Domain.Commands' must match namespace pattern '\\.Events$'.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustMatchNamespacePatternAssertion(@"\.Events$").Evaluate(TestModels.Project("Contoso.Domain"), TestModels.Repository());

        Assert.False(outcome.Passed);
        Assert.Equal("'must_match_namespace_pattern' can only be evaluated against types.", outcome.Message);
    }
}
