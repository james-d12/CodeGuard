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
}
