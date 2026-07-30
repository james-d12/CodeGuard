using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustNotInheritFromAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    [Fact]
    public void Evaluate_Passes_WhenBaseTypeDoesNotMatch()
    {
        var type = TestModels.Type("Contoso.Domain.Order", baseType: "Contoso.Domain.Entity<TId>");
        var outcome = new MustNotInheritFromAssertion("Contoso.Infrastructure.EfEntity").Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenBaseTypeMatches()
    {
        var type = TestModels.Type("Contoso.Domain.Order", baseType: "Contoso.Infrastructure.EfEntity");
        var outcome = new MustNotInheritFromAssertion("Contoso.Infrastructure.EfEntity").Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustNotInheritFromAssertion("Contoso.Infrastructure.EfEntity").Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
    }
}
