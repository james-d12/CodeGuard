using System.Text.Json.Nodes;
using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustExistAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    private sealed class StubSelector(IEnumerable<object> candidates) : ITargetSelector
    {
        public string Kind => "stub";
        public IEnumerable<object> SelectCandidates(RepositoryModel model) => candidates;
    }

    [Fact]
    public void Evaluate_Passes_WhenNestedSelectorFindsAtLeastOneMatch()
    {
        var template = new JsonObject { ["kind"] = "stub" };
        var assertion = new MustExistAssertion(template, _ => new StubSelector([new object()]));

        var outcome = assertion.Evaluate(TestModels.Type("Contoso.Domain.Order"), EmptyModel);

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenNestedSelectorFindsNoMatches()
    {
        var template = new JsonObject { ["kind"] = "stub" };
        var assertion = new MustExistAssertion(template, _ => new StubSelector([]));

        var outcome = assertion.Evaluate(TestModels.Type("Contoso.Domain.Order"), EmptyModel);

        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_ResolvesPlaceholders_FromOuterCandidateProperties()
    {
        var template = new JsonObject { ["kind"] = "stub", ["declaring_type"] = "${FullName}" };
        JsonObject? resolvedSeen = null;
        var assertion = new MustExistAssertion(template, resolved =>
        {
            resolvedSeen = resolved;
            return new StubSelector([]);
        });

        assertion.Evaluate(TestModels.Type("Contoso.Domain.Order"), EmptyModel);

        Assert.Equal("Contoso.Domain.Order", resolvedSeen!["declaring_type"]!.GetValue<string>());
    }

    [Fact]
    public void Evaluate_LeavesNonPlaceholderStrings_Untouched()
    {
        var template = new JsonObject { ["kind"] = "stub", ["namespace"] = "Contoso.*" };
        JsonObject? resolvedSeen = null;
        var assertion = new MustExistAssertion(template, resolved =>
        {
            resolvedSeen = resolved;
            return new StubSelector([]);
        });

        assertion.Evaluate(TestModels.Type("Contoso.Domain.Order"), EmptyModel);

        Assert.Equal("Contoso.*", resolvedSeen!["namespace"]!.GetValue<string>());
    }
}
