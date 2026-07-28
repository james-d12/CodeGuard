using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

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
    }
}
