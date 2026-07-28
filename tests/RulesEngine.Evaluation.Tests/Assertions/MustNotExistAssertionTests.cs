using System.Text.Json.Nodes;
using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustNotExistAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    private sealed class StubSelector(IEnumerable<object> candidates) : ITargetSelector
    {
        public string Kind => "stub";
        public IEnumerable<object> SelectCandidates(RepositoryModel model) => candidates;
    }

    [Fact]
    public void Evaluate_Passes_WhenNestedSelectorFindsNoMatches()
    {
        var template = new JsonObject { ["kind"] = "stub" };
        var assertion = new MustNotExistAssertion(template, _ => new StubSelector([]));

        var outcome = assertion.Evaluate(TestModels.Type("Contoso.Domain.Order"), EmptyModel);

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenNestedSelectorFindsAtLeastOneMatch()
    {
        var template = new JsonObject { ["kind"] = "stub" };
        var assertion = new MustNotExistAssertion(template, _ => new StubSelector([new object()]));

        var outcome = assertion.Evaluate(TestModels.Type("Contoso.Domain.Order"), EmptyModel);

        Assert.False(outcome.Passed);
    }
}
