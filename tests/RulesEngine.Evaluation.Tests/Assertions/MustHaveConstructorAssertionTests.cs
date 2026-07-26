using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustHaveConstructorAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], []);

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
    }

    [Fact]
    public void Evaluate_Fails_WhenNoConstructorsAtAll()
    {
        var type = TestModels.Type("Contoso.Domain.Order");
        var outcome = new MustHaveConstructorAssertion([Accessibility.Private]).Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
    }
}
