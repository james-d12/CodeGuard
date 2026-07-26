using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustNotDependOnAssertionTests
{
    [Fact]
    public void Evaluate_Passes_WhenNoTypeReferencesForbiddenNamespace()
    {
        var type = TestModels.Type("Contoso.Domain.Order", baseType: "Contoso.Domain.Entity<TId>");
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var outcome = new MustNotDependOnAssertion("Contoso.Infrastructure.*").Evaluate(project, model);

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenABaseTypeReferencesForbiddenNamespace()
    {
        var type = TestModels.Type("Contoso.Domain.Order", baseType: "Contoso.Infrastructure.EfEntity");
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var outcome = new MustNotDependOnAssertion("Contoso.Infrastructure.*").Evaluate(project, model);

        Assert.False(outcome.Passed);
        Assert.Contains("Contoso.Domain.Order", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_WhenAMethodSignatureReferencesForbiddenNamespace()
    {
        var method = new MethodModel(
            "Save", "Contoso.Infrastructure.SaveResult", [], Accessibility.Public, MethodModifiers.None);
        var type = TestModels.Type("Contoso.Domain.Order", methods: [method]);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var outcome = new MustNotDependOnAssertion("Contoso.Infrastructure.*").Evaluate(project, model);

        Assert.False(outcome.Passed);
    }
}
