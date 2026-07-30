using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustNotHaveModifierAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    [Fact]
    public void Evaluate_Passes_WhenTypeLacksModifier()
    {
        var type = TestModels.Type("Contoso.Domain.Order");
        var outcome = new MustNotHaveModifierAssertion("sealed").Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenTypeHasModifier()
    {
        var type = TestModels.Type("Contoso.Domain.Order") with { Modifiers = TypeModifiers.Sealed };
        var outcome = new MustNotHaveModifierAssertion("sealed").Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustNotHaveModifierAssertion("sealed").Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
    }
}
