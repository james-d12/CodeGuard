using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustHaveModifierAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    [Fact]
    public void Evaluate_Passes_WhenTypeHasSealedModifier()
    {
        var type = TestModels.Type("Contoso.Domain.Order") with { Modifiers = TypeModifiers.Sealed };
        var outcome = new MustHaveModifierAssertion("sealed").Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenTypeLacksModifier()
    {
        var type = TestModels.Type("Contoso.Domain.Order");
        var outcome = new MustHaveModifierAssertion("sealed").Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("Candidate must have modifier 'sealed'.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Passes_WhenTypeIsRecord()
    {
        var type = TestModels.Type("Contoso.Domain.OrderPlaced", kind: TypeKind.Record);
        var outcome = new MustHaveModifierAssertion("record").Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Passes_WhenPropertyIsRequired()
    {
        var property = new PropertyModel(
            "Id", "System.String", Accessibility.Public, HasGetter: true, HasSetter: true, SetterAccessibility: Accessibility.Public,
            IsRequired: true, IsInit: false, IsStatic: false, Attributes: [],
            DeclaringType: "Contoso.Domain.Order", ProjectName: "Contoso.Domain", FilePath: "Order.cs", Line: 1, Column: 1);
        var outcome = new MustHaveModifierAssertion("required").Evaluate(property, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustHaveModifierAssertion("sealed").Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("Modifier 'sealed' is not applicable to this candidate.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedModifierName()
    {
        var type = TestModels.Type("Contoso.Domain.Order");
        var outcome = new MustHaveModifierAssertion("virtual").Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
    }
}
