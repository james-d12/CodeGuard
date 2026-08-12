using System.Text.Json.Nodes;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Tests.Assertions;

public class MustHaveCountAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);
    private static readonly JsonObject Template = new() { ["kind"] = "stub" };

    private sealed class StubSelector(IEnumerable<object> candidates) : ITargetSelector
    {
        public string Kind => "stub";
        public IEnumerable<object> SelectCandidates(RepositoryModel model) => candidates;
    }

    private static MustHaveCountAssertion Assertion(IEnumerable<object> candidates, int? min = null, int? max = null, int? exactly = null) =>
        new(Template, _ => new StubSelector(candidates), min, max, exactly);

    [Fact]
    public void Evaluate_Passes_WhenCountMeetsMinimum()
    {
        var outcome = Assertion([new object(), new object()], min: 2).Evaluate(TestModels.Type("Contoso.Domain.Order"), EmptyModel);

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenCountBelowMinimum()
    {
        var outcome = Assertion([new object()], min: 2).Evaluate(TestModels.Type("Contoso.Domain.Order"), EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Equal("Expected at least 2 match(es) for a 'stub' selector, but found 1.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_WhenCountAboveMaximum()
    {
        var outcome = Assertion([new object(), new object(), new object()], max: 2)
            .Evaluate(TestModels.Type("Contoso.Domain.Order"), EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Equal("Expected at most 2 match(es) for a 'stub' selector, but found 3.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Passes_WhenCountEqualsExactly()
    {
        var outcome = Assertion([new object(), new object()], exactly: 2).Evaluate(TestModels.Type("Contoso.Domain.Order"), EmptyModel);

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenCountDoesNotEqualExactly()
    {
        var outcome = Assertion([], exactly: 1).Evaluate(TestModels.Type("Contoso.Domain.Order"), EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Equal("Expected exactly 1 match(es) for a 'stub' selector, but found 0.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Passes_ForZeroMatches_EquivalentToMustNotExist()
    {
        var outcome = Assertion([], exactly: 0).Evaluate(TestModels.Type("Contoso.Domain.Order"), EmptyModel);

        Assert.True(outcome.Passed);
    }
}
