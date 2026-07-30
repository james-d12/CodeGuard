using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustNotHaveAttributeAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    private static AttributeModel Obsolete() => new(
        "System.ObsoleteAttribute", [], new Dictionary<string, string>());

    [Fact]
    public void Evaluate_Passes_WhenAttributeMissing()
    {
        var type = TestModels.Type("Contoso.Domain.Order");
        var outcome = new MustNotHaveAttributeAssertion("System.ObsoleteAttribute", null).Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenAttributeExists()
    {
        var type = TestModels.Type("Contoso.Domain.Legacy") with { Attributes = [Obsolete()] };
        var outcome = new MustNotHaveAttributeAssertion("System.ObsoleteAttribute", null).Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustNotHaveAttributeAssertion("System.ObsoleteAttribute", null).Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Passes_WhenAttributePresent_ButTypeNameDoesNotMatch_EvenWithNoArgumentConstraint()
    {
        var type = TestModels.Type("Contoso.Domain.Legacy") with { Attributes = [Obsolete()] };
        var outcome = new MustNotHaveAttributeAssertion("System.SerializableAttribute", null).Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }
}
