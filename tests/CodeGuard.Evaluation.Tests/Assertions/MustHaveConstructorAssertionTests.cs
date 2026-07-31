using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

public class MustHaveConstructorAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    private static ConstructorModel Constructor(Accessibility accessibility) =>
        new(accessibility, [], [], "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 1, 1);

    [Fact]
    public void Evaluate_Passes_WhenConstructorMatchesAnAllowedAccessibility()
    {
        var type = TestModels.Type("Contoso.Domain.Order", constructors: [Constructor(Accessibility.Protected)]);
        var outcome = new MustHaveConstructorAssertion([Accessibility.Private, Accessibility.Protected])
            .Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenNoConstructorMatchesAllowedAccessibility()
    {
        var type = TestModels.Type("Contoso.Domain.Order", constructors: [Constructor(Accessibility.Public)]);
        var outcome = new MustHaveConstructorAssertion([Accessibility.Private, Accessibility.Protected])
            .Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'Contoso.Domain.Order' must have a constructor with accessibility Private or Protected.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_WhenNoConstructorsAtAll()
    {
        var type = TestModels.Type("Contoso.Domain.Order");
        var outcome = new MustHaveConstructorAssertion([Accessibility.Private]).Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenAllowedAccessibilitiesIsEmpty_EvenIfConstructorsExist()
    {
        var type = TestModels.Type("Contoso.Domain.Order", constructors: [Constructor(Accessibility.Public)]);
        var outcome = new MustHaveConstructorAssertion([]).Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustHaveConstructorAssertion([Accessibility.Public]).Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'must_have_constructor' can only be evaluated against types.", outcome.Message);
    }
}
