using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Selectors;

namespace RulesEngine.Evaluation.Tests.Selectors;

public class PropertySelectorTests
{
    private static PropertyModel Property(string name, string declaringType, string projectName, bool isStatic = false, Accessibility accessibility = Accessibility.Public) => new(
        name, "System.String", accessibility, HasGetter: true, HasSetter: true, SetterAccessibility: Accessibility.Public,
        IsRequired: false, IsInit: false, IsStatic: isStatic, Attributes: [],
        DeclaringType: declaringType, ProjectName: projectName, FilePath: $"{declaringType}.cs", Line: 1, Column: 1);

    [Fact]
    public void SelectCandidates_FiltersByDeclaringType()
    {
        var matching = Property("Id", "Contoso.Domain.Order", "Contoso.Domain");
        var other = Property("Id", "Contoso.Domain.Customer", "Contoso.Domain");
        var orderType = TestModels.Type("Contoso.Domain.Order", properties: [matching]);
        var customerType = TestModels.Type("Contoso.Domain.Customer", properties: [other]);
        var project = TestModels.Project("Contoso.Domain", types: [orderType, customerType]);
        var model = TestModels.Repository(project);

        var candidates = new PropertySelector(declaringTypePattern: "Contoso.Domain.Order").SelectCandidates(model).Cast<PropertyModel>().ToList();

        Assert.Single(candidates);
    }

    [Fact]
    public void SelectCandidates_FiltersByIsStatic()
    {
        var staticProperty = Property("Default", "Contoso.Domain.Order", "Contoso.Domain", isStatic: true);
        var instanceProperty = Property("Id", "Contoso.Domain.Order", "Contoso.Domain");
        var type = TestModels.Type("Contoso.Domain.Order", properties: [staticProperty, instanceProperty]);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var candidates = new PropertySelector(isStatic: true).SelectCandidates(model).Cast<PropertyModel>().ToList();

        var property = Assert.Single(candidates);
        Assert.Equal("Default", property.Name);
    }

    [Fact]
    public void SelectCandidates_FiltersByAccessibility()
    {
        var publicProperty = Property("Id", "Contoso.Domain.Order", "Contoso.Domain", accessibility: Accessibility.Public);
        var privateProperty = Property("Secret", "Contoso.Domain.Order", "Contoso.Domain", accessibility: Accessibility.Private);
        var type = TestModels.Type("Contoso.Domain.Order", properties: [publicProperty, privateProperty]);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var candidates = new PropertySelector(accessibility: Accessibility.Private).SelectCandidates(model).Cast<PropertyModel>().ToList();

        var property = Assert.Single(candidates);
        Assert.Equal("Secret", property.Name);
    }

    [Fact]
    public void SelectCandidates_ReturnsEmpty_WhenRepositoryHasNoProjects()
    {
        var model = TestModels.Repository();

        var candidates = new PropertySelector().SelectCandidates(model).ToList();

        Assert.Empty(candidates);
    }

    [Fact]
    public void SelectCandidates_CombinesIsStaticAndAccessibility_RequiringBothToMatch()
    {
        var staticPrivate = Property("Instance", "Contoso.Domain.Order", "Contoso.Domain", isStatic: true, accessibility: Accessibility.Private);
        var staticPublic = Property("Default", "Contoso.Domain.Order", "Contoso.Domain", isStatic: true, accessibility: Accessibility.Public);
        var instancePrivate = Property("Secret", "Contoso.Domain.Order", "Contoso.Domain", accessibility: Accessibility.Private);
        var type = TestModels.Type("Contoso.Domain.Order", properties: [staticPrivate, staticPublic, instancePrivate]);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var candidates = new PropertySelector(isStatic: true, accessibility: Accessibility.Private).SelectCandidates(model).Cast<PropertyModel>().ToList();

        var property = Assert.Single(candidates);
        Assert.Equal("Instance", property.Name);
    }
}
