using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Selectors;

namespace RulesEngine.Evaluation.Tests.Selectors;

public class ConstructorSelectorTests
{
    private static ConstructorModel Constructor(string declaringType, params string[] parameterTypes) => new(
        Accessibility.Public,
        parameterTypes.Select((t, i) => new ParameterModel($"p{i}", t, [], false)).ToList(),
        [], declaringType, "Contoso.Domain", $"{declaringType}.cs", 1, 1);

    [Fact]
    public void SelectCandidates_FiltersByDeclaringType()
    {
        var order = TestModels.Type("Contoso.Domain.Order", constructors: [Constructor("Contoso.Domain.Order")]);
        var customer = TestModels.Type("Contoso.Domain.Customer", constructors: [Constructor("Contoso.Domain.Customer")]);
        var project = TestModels.Project("Contoso.Domain", types: [order, customer]);
        var model = TestModels.Repository(project);

        var candidates = new ConstructorSelector(declaringTypePattern: "Contoso.Domain.Order").SelectCandidates(model).Cast<ConstructorModel>().ToList();

        Assert.Single(candidates);
    }

    [Fact]
    public void SelectCandidates_FiltersByParameterTypes()
    {
        var matching = Constructor("Contoso.Domain.Order", "string", "int");
        var nonMatching = Constructor("Contoso.Domain.Order", "string");
        var type = TestModels.Type("Contoso.Domain.Order", constructors: [matching, nonMatching]);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.Repository(project);

        var candidates = new ConstructorSelector(parameterTypePatterns: ["string", "int"]).SelectCandidates(model).Cast<ConstructorModel>().ToList();

        var constructor = Assert.Single(candidates);
        Assert.Equal(2, constructor.Parameters.Count);
    }

    [Fact]
    public void SelectCandidates_ReturnsEmpty_WhenRepositoryHasNoProjects()
    {
        var model = TestModels.Repository();

        var candidates = new ConstructorSelector().SelectCandidates(model).ToList();

        Assert.Empty(candidates);
    }
}
