using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

public class MustDependOnAssertionTests
{
    [Fact]
    public void Evaluate_Passes_WhenABaseTypeReferencesRequiredNamespace()
    {
        var type = TestModels.Type("Contoso.Domain.Order", baseType: "Contoso.Domain.Entity<TId>");
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var outcome = new MustDependOnAssertion("Contoso.Domain.Entity<*>").Evaluate(project, model);

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenNoTypeReferencesRequiredNamespace()
    {
        var type = TestModels.Type("Contoso.Domain.Order", baseType: "System.Object");
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var outcome = new MustDependOnAssertion("Contoso.Domain.Entity<*>").Evaluate(project, model);

        Assert.False(outcome.Passed);
        Assert.Equal("Project 'Contoso.Domain' must depend on a type matching 'Contoso.Domain.Entity<*>'.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var model = TestModels.Repository();

        var outcome = new MustDependOnAssertion("Contoso.Domain.*").Evaluate(TestModels.Type("Contoso.Domain.Order"), model);

        Assert.False(outcome.Passed);
        Assert.Equal("'must_depend_on' can only be evaluated against projects.", outcome.Message);
    }
}
