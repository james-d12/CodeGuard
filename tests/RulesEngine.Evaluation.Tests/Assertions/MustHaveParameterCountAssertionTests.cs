using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustHaveParameterCountAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    private static MethodModel MethodWithParameters(int count) => new(
        "Create", "System.Void",
        Enumerable.Range(0, count).Select(i => new ParameterModel($"p{i}", "string", [], false)).ToList(),
        Accessibility.Public, MethodModifiers.None, [], "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 1, 1);

    [Fact]
    public void Evaluate_Passes_WhenWithinRange()
    {
        var outcome = new MustHaveParameterCountAssertion(1, 3).Evaluate(MethodWithParameters(2), EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenBelowMinimum()
    {
        var outcome = new MustHaveParameterCountAssertion(2, null).Evaluate(MethodWithParameters(1), EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenAboveMaximum()
    {
        var outcome = new MustHaveParameterCountAssertion(null, 1).Evaluate(MethodWithParameters(2), EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustHaveParameterCountAssertion(null, null).Evaluate(TestModels.Type("Contoso.Domain.Order"), EmptyModel);
        Assert.False(outcome.Passed);
    }
}
