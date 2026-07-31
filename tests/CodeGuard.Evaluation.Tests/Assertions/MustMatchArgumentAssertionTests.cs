using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

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
        Assert.Equal("Argument 0 of call site 'MapGet' must match '^/api/v[0-9]+/' (found '/orders').", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_WhenArgumentIsNotLiteral()
    {
        var callSite = CallSite(new CallSiteArgument(0, null, IsLiteral: false));

        var outcome = new MustMatchArgumentAssertion(0, @"^/api/").Evaluate(callSite, EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Equal("Call site 'MapGet' must have a literal argument at index 0.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_WhenArgumentIsNotLiteral_EvenIfLiteralValueIsSet()
    {
        var callSite = CallSite(new CallSiteArgument(0, "/api/orders", IsLiteral: false));

        var outcome = new MustMatchArgumentAssertion(0, @"^/api/orders$").Evaluate(callSite, EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Equal("Call site 'MapGet' must have a literal argument at index 0.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_WhenNoArgumentExistsAtIndex()
    {
        var callSite = CallSite();

        var outcome = new MustMatchArgumentAssertion(0, ".*").Evaluate(callSite, EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Equal("Call site 'MapGet' must have a literal argument at index 0.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustMatchArgumentAssertion(0, ".*").Evaluate(42, EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Equal("'must_match_argument' can only be evaluated against call sites.", outcome.Message);
    }
}
