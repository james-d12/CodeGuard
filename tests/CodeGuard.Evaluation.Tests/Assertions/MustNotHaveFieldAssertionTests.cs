using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

public class MustNotHaveFieldAssertionTests
{
    private static FieldModel Field(string name) =>
        new(name, "System.String", Accessibility.Public, FieldModifiers.None, [], "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 1, 1);

    [Fact]
    public void Evaluate_Passes_WhenNoFieldMatches()
    {
        var type = TestModels.Type("Contoso.Domain.Order", fields: [Field("_id")]);

        var outcome = new MustNotHaveFieldAssertion("Public*").Evaluate(type, TestModels.Repository());

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenAFieldMatches()
    {
        var type = TestModels.Type("Contoso.Domain.Order", fields: [Field("PublicField")]);

        var outcome = new MustNotHaveFieldAssertion("Public*").Evaluate(type, TestModels.Repository());

        Assert.False(outcome.Passed);
        Assert.Equal("'Contoso.Domain.Order' must not have a field matching 'Public*' (found 'PublicField').", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustNotHaveFieldAssertion("Public*").Evaluate(TestModels.Project("Contoso.Domain"), TestModels.Repository());

        Assert.False(outcome.Passed);
        Assert.Equal("'must_not_have_field' can only be evaluated against types.", outcome.Message);
    }
}
