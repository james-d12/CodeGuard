using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

public class MustHaveMethodAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    private static MethodModel Method(string name) =>
        new(name, "System.Void", [], Accessibility.Public, MethodModifiers.Static, [], "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 1, 1);

    [Fact]
    public void Evaluate_Passes_WhenMatchingMethodExists()
    {
        var type = TestModels.Type("Contoso.Domain.Order", methods: [Method("Create")]);
        var outcome = new MustHaveMethodAssertion("Create").Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenNoMatchingMethodExists()
    {
        var type = TestModels.Type("Contoso.Domain.Order", methods: [Method("Update")]);
        var outcome = new MustHaveMethodAssertion("Create").Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'Contoso.Domain.Order' must have a method matching 'Create'.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustHaveMethodAssertion("Create").Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'must_have_method' can only be evaluated against types.", outcome.Message);
    }
}
