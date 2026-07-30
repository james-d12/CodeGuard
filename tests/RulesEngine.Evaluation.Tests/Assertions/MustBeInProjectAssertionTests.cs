using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustBeInProjectAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    [Fact]
    public void Evaluate_Passes_WhenProjectNameMatches()
    {
        var type = TestModels.Type("Contoso.Domain.Events.OrderPlaced", projectName: "Contoso.Domain");
        var outcome = new MustBeInProjectAssertion("*.Domain").Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenProjectNameDoesNotMatch()
    {
        var type = TestModels.Type("Contoso.Domain.Events.OrderPlaced", projectName: "Contoso.Application");
        var outcome = new MustBeInProjectAssertion("*.Domain").Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'Contoso.Domain.Events.OrderPlaced' must be in a project matching '*.Domain'.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustBeInProjectAssertion("*").Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'must_be_in_project' can only be evaluated against types.", outcome.Message);
    }
}
