using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

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
        Assert.Equal("'Contoso.Domain.Order' must not inherit from 'Contoso.Infrastructure.EfEntity'.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustNotInheritFromAssertion("Contoso.Infrastructure.EfEntity").Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'must_not_inherit_from' can only be evaluated against types.", outcome.Message);
    }
}
