using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

public class MustOnlyDependOnAssertionTests
{
    [Fact]
    public void Evaluate_Passes_WhenEveryReferencedTypeMatchesAnAllowedPattern()
    {
        var type = TestModels.Type("Contoso.Domain.Order", baseType: "Contoso.Domain.Entity<TId>", interfaces: ["Contoso.Domain.IAggregateRoot"]);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var outcome = new MustOnlyDependOnAssertion(["Contoso.Domain.*"]).Evaluate(project, model);

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenABaseTypeMatchesNoAllowedPattern()
    {
        var type = TestModels.Type("Contoso.Domain.Order", baseType: "Contoso.Infrastructure.EfEntity");
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var outcome = new MustOnlyDependOnAssertion(["Contoso.Domain.*"]).Evaluate(project, model);

        Assert.False(outcome.Passed);
        Assert.Contains("Contoso.Infrastructure.EfEntity", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_WhenAFieldTypeMatchesNoAllowedPattern()
    {
        var field = new FieldModel(
            "_repository", "Contoso.Infrastructure.OrderRepository", Accessibility.Private, FieldModifiers.None,
            [], "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 1, 1);
        var type = TestModels.Type("Contoso.Domain.Order", fields: [field]);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var outcome = new MustOnlyDependOnAssertion(["Contoso.Domain.*"]).Evaluate(project, model);

        Assert.False(outcome.Passed);
        Assert.Contains("Contoso.Infrastructure.OrderRepository", outcome.Message);
    }

    [Fact]
    public void Evaluate_Passes_WhenNoTypesAtAll()
    {
        var project = TestModels.Project("Contoso.Domain");
        var model = TestModels.Repository(project);

        var outcome = new MustOnlyDependOnAssertion(["Contoso.Domain.*"]).Evaluate(project, model);

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var model = TestModels.Repository();

        var outcome = new MustOnlyDependOnAssertion(["Contoso.Domain.*"]).Evaluate(TestModels.Type("Contoso.Domain.Order"), model);

        Assert.False(outcome.Passed);
        Assert.Equal("'must_only_depend_on' can only be evaluated against projects.", outcome.Message);
    }
}
