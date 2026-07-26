using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Selectors;

namespace RulesEngine.Evaluation.Tests.Selectors;

public class MethodSelectorTests
{
    private static MethodModel Method(string name, string declaringType, string projectName, MethodModifiers modifiers = MethodModifiers.None, Accessibility accessibility = Accessibility.Public) =>
        new(name, "System.Void", [], accessibility, modifiers, [], declaringType, projectName, $"{declaringType}.cs", 1, 1);

    [Fact]
    public void SelectCandidates_FiltersByNamespaceAndDeclaringType()
    {
        var matching = Method("Save", "Contoso.Domain.Order", "Contoso.Domain");
        var type = TestModels.Type("Contoso.Domain.Order", methods: [matching]);
        var otherType = TestModels.Type("Contoso.Application.Handler", methods: [Method("Handle", "Contoso.Application.Handler", "Contoso.Application")]);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var otherProject = TestModels.Project("Contoso.Application", types: [otherType]);
        var model = TestModels.Repository(project, otherProject);

        var candidates = new MethodSelector(namespacePattern: "Contoso.Domain*").SelectCandidates(model).Cast<MethodModel>().ToList();

        var method = Assert.Single(candidates);
        Assert.Equal("Save", method.Name);
    }

    [Fact]
    public void SelectCandidates_FiltersByIsStatic()
    {
        var staticMethod = Method("Create", "Contoso.Domain.Order", "Contoso.Domain", MethodModifiers.Static);
        var instanceMethod = Method("Save", "Contoso.Domain.Order", "Contoso.Domain");
        var type = TestModels.Type("Contoso.Domain.Order", methods: [staticMethod, instanceMethod]);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var candidates = new MethodSelector(isStatic: true).SelectCandidates(model).Cast<MethodModel>().ToList();

        var method = Assert.Single(candidates);
        Assert.Equal("Create", method.Name);
    }

    [Fact]
    public void SelectCandidates_FiltersByAccessibility()
    {
        var publicMethod = Method("Save", "Contoso.Domain.Order", "Contoso.Domain", accessibility: Accessibility.Public);
        var privateMethod = Method("Validate", "Contoso.Domain.Order", "Contoso.Domain", accessibility: Accessibility.Private);
        var type = TestModels.Type("Contoso.Domain.Order", methods: [publicMethod, privateMethod]);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var candidates = new MethodSelector(accessibility: Accessibility.Private).SelectCandidates(model).Cast<MethodModel>().ToList();

        var method = Assert.Single(candidates);
        Assert.Equal("Validate", method.Name);
    }
}
