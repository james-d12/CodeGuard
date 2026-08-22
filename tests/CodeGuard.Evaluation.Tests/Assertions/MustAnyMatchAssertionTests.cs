using System.Text.Json.Nodes;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Tests.Assertions;

public class MustAnyMatchAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);
    private static readonly JsonObject Template = new() { ["kind"] = "stub" };

    private sealed class StubSelector(IEnumerable<object> candidates) : ITargetSelector
    {
        public string Kind => "stub";
        public IEnumerable<object> SelectCandidates(RepositoryModel model) => candidates;
    }

    private sealed class StubAssertion(Func<object, bool> predicate, string failureMessage) : IAssertion
    {
        public string Kind => "stub_assertion";

        public AssertionOutcome Evaluate(object candidate, RepositoryModel model) =>
            predicate(candidate) ? AssertionOutcome.Success() : AssertionOutcome.Failure(failureMessage);
    }

    private static MustAnyMatchAssertion Assertion(IEnumerable<object> candidates, params IAssertion[] assertions) =>
        new(Template, _ => new StubSelector(candidates), assertions);

    [Fact]
    public void Evaluate_Passes_WhenAtLeastOneMatchSatisfiesEveryNestedAssertion()
    {
        var onlyA = new StubAssertion(c => ((TypeModel)c).Name == "A", "must be named A");

        var outcome = Assertion([TestModels.Type("A"), TestModels.Type("B")], onlyA)
            .Evaluate(TestModels.Type("Outer"), EmptyModel);

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenSelectorProducesNoMatches()
    {
        var alwaysPasses = new StubAssertion(_ => true, "never happens");

        var outcome = Assertion([], alwaysPasses).Evaluate(TestModels.Type("Outer"), EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Contains("none of the 0 match(es)", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_WhenNoMatchSatisfiesEveryNestedAssertion()
    {
        var onlyC = new StubAssertion(c => ((TypeModel)c).Name == "C", "must be named C");

        var outcome = Assertion([TestModels.Type("A"), TestModels.Type("B")], onlyC)
            .Evaluate(TestModels.Type("Outer"), EmptyModel);

        Assert.False(outcome.Passed);
    }
}
