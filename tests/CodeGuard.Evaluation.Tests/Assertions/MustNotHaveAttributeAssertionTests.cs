using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

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
        Assert.Equal("Candidate must not have attribute matching 'System.ObsoleteAttribute'.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustNotHaveAttributeAssertion("System.ObsoleteAttribute", null).Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'must_not_have_attribute' cannot be evaluated against this candidate type.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_WhenArgumentSpecified_AndAttributeMatchesArgument()
    {
        var attribute = new AttributeModel("System.ObsoleteAttribute", ["use something else"], new Dictionary<string, string>());
        var type = TestModels.Type("Contoso.Domain.Legacy") with { Attributes = [attribute] };
        var outcome = new MustNotHaveAttributeAssertion("System.ObsoleteAttribute", "use something else").Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("Candidate must not have attribute matching 'System.ObsoleteAttribute' with argument 'use something else'.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Passes_WhenAttributePresent_ButTypeNameDoesNotMatch_EvenWithNoArgumentConstraint()
    {
        var type = TestModels.Type("Contoso.Domain.Legacy") with { Attributes = [Obsolete()] };
        var outcome = new MustNotHaveAttributeAssertion("System.SerializableAttribute", null).Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }
}
