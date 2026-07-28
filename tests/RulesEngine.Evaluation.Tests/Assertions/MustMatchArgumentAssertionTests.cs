using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustMatchArgumentAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    private static CallSiteModel CallSite(params CallSiteArgument[] arguments) => new(
        CallSiteKind.Invocation, "MapGet", null, "Configure", "Contoso.Application.Startup",
        "Contoso.Application", arguments, "Startup.cs", 1, 1);

    [Fact]
    public void Evaluate_Passes_WhenLiteralArgumentMatchesPattern()
    {
        var callSite = CallSite(new CallSiteArgument(0, "/api/v1/orders", IsLiteral: true));

        var outcome = new MustMatchArgumentAssertion(0, @"^/api/v[0-9]+/").Evaluate(callSite, EmptyModel);

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenLiteralArgumentDoesNotMatchPattern()
    {
        var callSite = CallSite(new CallSiteArgument(0, "/orders", IsLiteral: true));

        var outcome = new MustMatchArgumentAssertion(0, @"^/api/v[0-9]+/").Evaluate(callSite, EmptyModel);

        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenArgumentIsNotLiteral()
    {
        var callSite = CallSite(new CallSiteArgument(0, null, IsLiteral: false));

        var outcome = new MustMatchArgumentAssertion(0, @"^/api/").Evaluate(callSite, EmptyModel);

        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustMatchArgumentAssertion(0, ".*").Evaluate(42, EmptyModel);

        Assert.False(outcome.Passed);
    }
}
