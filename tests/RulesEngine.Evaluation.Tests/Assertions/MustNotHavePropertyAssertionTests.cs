using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustNotHavePropertyAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    private static PropertyModel Property(string name) => new(
        name, "System.String", Accessibility.Public, HasGetter: true, HasSetter: false, SetterAccessibility: null,
        IsRequired: false, IsInit: false, IsStatic: false, Attributes: [],
        DeclaringType: "Contoso.Domain.Order", ProjectName: "Contoso.Domain", FilePath: "Order.cs", Line: 1, Column: 1);

    [Fact]
    public void Evaluate_Passes_WhenNoMatchingPropertyExists()
    {
        var type = TestModels.Type("Contoso.Domain.Order");
        var outcome = new MustNotHavePropertyAssertion("Password").Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenMatchingPropertyExists()
    {
        var type = TestModels.Type("Contoso.Domain.Order", properties: [Property("Password")]);
        var outcome = new MustNotHavePropertyAssertion("Password").Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
    }
}
