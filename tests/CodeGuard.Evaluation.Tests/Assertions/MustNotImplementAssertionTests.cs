using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

public class MustNotImplementAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    [Fact]
    public void Evaluate_Passes_WhenInterfaceNotImplemented()
    {
        var type = TestModels.Type("Contoso.Domain.Order", interfaces: ["Contoso.Domain.IAggregateRoot"]);
        var outcome = new MustNotImplementAssertion("Contoso.Domain.IDisposableEntity").Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenInterfaceImplemented()
    {
        var type = TestModels.Type("Contoso.Domain.Order", interfaces: ["Contoso.Domain.IDisposableEntity"]);
        var outcome = new MustNotImplementAssertion("Contoso.Domain.IDisposableEntity").Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'Contoso.Domain.Order' must not implement 'Contoso.Domain.IDisposableEntity'.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustNotImplementAssertion("Contoso.Domain.IDisposableEntity").Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'must_not_implement' can only be evaluated against types.", outcome.Message);
    }
}
