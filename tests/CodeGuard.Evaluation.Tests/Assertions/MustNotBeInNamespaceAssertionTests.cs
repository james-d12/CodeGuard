using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

public class MustNotBeInNamespaceAssertionTests
{
    [Fact]
    public void Evaluate_Passes_WhenNamespaceDoesNotMatch()
    {
        var type = TestModels.Type("Contoso.Domain.Order");

        var outcome = new MustNotBeInNamespaceAssertion("Contoso.Infrastructure*").Evaluate(type, TestModels.Repository());

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenNamespaceMatches()
    {
        var type = TestModels.Type("Contoso.Infrastructure.OrderRepository");

        var outcome = new MustNotBeInNamespaceAssertion("Contoso.Infrastructure*").Evaluate(type, TestModels.Repository());

        Assert.False(outcome.Passed);
        Assert.Equal(
            "'Contoso.Infrastructure.OrderRepository' must not be in a namespace matching 'Contoso.Infrastructure*'.",
            outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustNotBeInNamespaceAssertion("Contoso.Infrastructure*").Evaluate(TestModels.Project("Contoso.Domain"), TestModels.Repository());

        Assert.False(outcome.Passed);
        Assert.Equal("'must_not_be_in_namespace' can only be evaluated against types.", outcome.Message);
    }
}
