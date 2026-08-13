using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

public class MustHaveFieldAssertionTests
{
    private static FieldModel Field(string name) =>
        new(name, "System.String", Accessibility.Private, FieldModifiers.None, [], "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 1, 1);

    [Fact]
    public void Evaluate_Passes_WhenAFieldMatches()
    {
        var type = TestModels.Type("Contoso.Domain.Order", fields: [Field("_id")]);

        var outcome = new MustHaveFieldAssertion("_id").Evaluate(type, TestModels.Repository());

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenNoFieldMatches()
    {
        var type = TestModels.Type("Contoso.Domain.Order", fields: [Field("_name")]);

        var outcome = new MustHaveFieldAssertion("_id").Evaluate(type, TestModels.Repository());

        Assert.False(outcome.Passed);
        Assert.Equal("'Contoso.Domain.Order' must have a field matching '_id'.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustHaveFieldAssertion("_id").Evaluate(TestModels.Project("Contoso.Domain"), TestModels.Repository());

        Assert.False(outcome.Passed);
        Assert.Equal("'must_have_field' can only be evaluated against types.", outcome.Message);
    }
}
