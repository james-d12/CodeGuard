using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustHaveAttributeAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    private static AttributeModel Obsolete(string reason) => new(
        "System.ObsoleteAttribute", [reason], new Dictionary<string, string>());

    [Fact]
    public void Evaluate_Passes_WhenAttributeExists()
    {
        var type = TestModels.Type("Contoso.Domain.Legacy") with { Attributes = [Obsolete("use something else")] };
        var outcome = new MustHaveAttributeAssertion("System.ObsoleteAttribute", null).Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Passes_WhenAttributeArgumentMatches()
    {
        var type = TestModels.Type("Contoso.Domain.Legacy") with { Attributes = [Obsolete("use something else")] };
        var outcome = new MustHaveAttributeAssertion("System.ObsoleteAttribute", "use something else").Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenAttributeMissing()
    {
        var type = TestModels.Type("Contoso.Domain.Order");
        var outcome = new MustHaveAttributeAssertion("System.ObsoleteAttribute", null).Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustHaveAttributeAssertion("System.ObsoleteAttribute", null).Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
    }
}
