using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustImplementAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], []);

    [Fact]
    public void Evaluate_Passes_WhenInterfaceMatches()
    {
        var type = TestModels.Type("Contoso.Domain.Order", interfaces: ["Contoso.Domain.IAggregateRoot"]);
        var outcome = new MustImplementAssertion("Contoso.Domain.IAggregateRoot").Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenInterfaceMissing()
    {
        var type = TestModels.Type("Contoso.Domain.Order");
        var outcome = new MustImplementAssertion("Contoso.Domain.IAggregateRoot").Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
    }
}
