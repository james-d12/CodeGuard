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
        Assert.Equal("Project 'Contoso.Domain' must not depend on 'Contoso.Infrastructure.*' (via: Contoso.Domain.Order).", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_WhenAMethodSignatureReferencesForbiddenNamespace()
    {
        var method = new MethodModel(
            "Save", "Contoso.Infrastructure.SaveResult", [], Accessibility.Public, MethodModifiers.None,
            [], "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 1, 1);
        var type = TestModels.Type("Contoso.Domain.Order", methods: [method]);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var outcome = new MustNotDependOnAssertion("Contoso.Infrastructure.*").Evaluate(project, model);

        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenAMethodParameterReferencesForbiddenNamespace()
    {
        var method = new MethodModel(
            "Save", "System.Void", [new ParameterModel("result", "Contoso.Infrastructure.SaveResult", [], false)],
            Accessibility.Public, MethodModifiers.None, [], "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 1, 1);
        var type = TestModels.Type("Contoso.Domain.Order", methods: [method]);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var outcome = new MustNotDependOnAssertion("Contoso.Infrastructure.*").Evaluate(project, model);

        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenAnInterfaceReferencesForbiddenNamespace()
    {
        var type = TestModels.Type("Contoso.Domain.Order", interfaces: ["Contoso.Infrastructure.IPersistable"]);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var outcome = new MustNotDependOnAssertion("Contoso.Infrastructure.*").Evaluate(project, model);

        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Passes_WhenTypeHasNoBaseTypeAtAll()
    {
        var type = TestModels.Type("Contoso.Domain.Order", baseType: null);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var outcome = new MustNotDependOnAssertion("Contoso.Infrastructure.*").Evaluate(project, model);

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Passes_WhenProjectHasNoTypesAtAll()
    {
        var project = TestModels.Project("Contoso.Domain");
        var model = TestModels.Repository(project);

        var outcome = new MustNotDependOnAssertion("Contoso.Infrastructure.*").Evaluate(project, model);

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var model = TestModels.Repository();

        var outcome = new MustNotDependOnAssertion("Contoso.Infrastructure.*").Evaluate(TestModels.Type("Contoso.Domain.Order"), model);

        Assert.False(outcome.Passed);
        Assert.Equal("'must_not_depend_on' can only be evaluated against projects.", outcome.Message);
    }
}
