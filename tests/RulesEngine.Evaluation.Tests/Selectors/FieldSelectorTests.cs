using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Selectors;

namespace RulesEngine.Evaluation.Tests.Selectors;

public class FieldSelectorTests
{
    private static FieldModel Field(string name, string declaringType, FieldModifiers modifiers = FieldModifiers.None) => new(
        name, "System.String", Accessibility.Private, modifiers, [], declaringType, "Contoso.Domain", $"{declaringType}.cs", 1, 1);

    [Fact]
    public void SelectCandidates_FiltersByIsReadonly()
    {
        var readonlyField = Field("_id", "Contoso.Domain.Order", FieldModifiers.Readonly);
        var mutableField = Field("_name", "Contoso.Domain.Order");
        var type = TestModels.Type("Contoso.Domain.Order", fields: [readonlyField, mutableField]);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var candidates = new FieldSelector(isReadonly: true).SelectCandidates(model).Cast<FieldModel>().ToList();

        var field = Assert.Single(candidates);
        Assert.Equal("_id", field.Name);
    }

    [Fact]
    public void SelectCandidates_FiltersByDeclaringType()
    {
        var order = TestModels.Type("Contoso.Domain.Order", fields: [Field("_id", "Contoso.Domain.Order")]);
        var customer = TestModels.Type("Contoso.Domain.Customer", fields: [Field("_id", "Contoso.Domain.Customer")]);
        var project = TestModels.Project("Contoso.Domain", types: [order, customer]);
        var model = TestModels.Repository(project);

        var candidates = new FieldSelector(declaringTypePattern: "Contoso.Domain.Customer").SelectCandidates(model).Cast<FieldModel>().ToList();

        Assert.Single(candidates);
    }

    [Fact]
    public void SelectCandidates_FiltersByIsStatic()
    {
        var staticField = Field("Default", "Contoso.Domain.Order", FieldModifiers.Static);
        var instanceField = Field("_id", "Contoso.Domain.Order");
        var type = TestModels.Type("Contoso.Domain.Order", fields: [staticField, instanceField]);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var candidates = new FieldSelector(isStatic: true).SelectCandidates(model).Cast<FieldModel>().ToList();

        var field = Assert.Single(candidates);
        Assert.Equal("Default", field.Name);
    }

    [Fact]
    public void SelectCandidates_ReturnsEmpty_WhenRepositoryHasNoProjects()
    {
        var model = TestModels.Repository();

        var candidates = new FieldSelector().SelectCandidates(model).ToList();

        Assert.Empty(candidates);
    }

    [Fact]
    public void SelectCandidates_CombinesIsReadonlyAndIsStatic_RequiringBothToMatch()
    {
        var staticReadonly = Field("MaxCount", "Contoso.Domain.Order", FieldModifiers.Static | FieldModifiers.Readonly);
        var staticMutable = Field("Counter", "Contoso.Domain.Order", FieldModifiers.Static);
        var instanceReadonly = Field("_id", "Contoso.Domain.Order", FieldModifiers.Readonly);
        var type = TestModels.Type("Contoso.Domain.Order", fields: [staticReadonly, staticMutable, instanceReadonly]);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var candidates = new FieldSelector(isReadonly: true, isStatic: true).SelectCandidates(model).Cast<FieldModel>().ToList();

        var field = Assert.Single(candidates);
        Assert.Equal("MaxCount", field.Name);
    }
}
