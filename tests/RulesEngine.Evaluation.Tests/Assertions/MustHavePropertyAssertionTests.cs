using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustHavePropertyAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], []);

    private static PropertyModel Property(string name) =>
        new(name, "System.String", Accessibility.Public, HasGetter: true, HasSetter: false, SetterAccessibility: null);

    [Fact]
    public void Evaluate_Passes_WhenMatchingPropertyExists()
    {
        var type = TestModels.Type("Contoso.Domain.Order", properties: [Property("Id")]);
        var outcome = new MustHavePropertyAssertion("Id").Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenNoMatchingPropertyExists()
    {
        var type = TestModels.Type("Contoso.Domain.Order", properties: [Property("Name")]);
        var outcome = new MustHavePropertyAssertion("Id").Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
    }
}
