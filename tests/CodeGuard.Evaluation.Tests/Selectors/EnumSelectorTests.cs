using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Selectors;

namespace CodeGuard.Evaluation.Tests.Selectors;

public class EnumSelectorTests
{
    [Fact]
    public void SelectCandidates_ReturnsOnlyEnumsMatchingNamespacePattern()
    {
        var matchingEnum = TestModels.Type("Contoso.Domain.Status", kind: TypeKind.Enum);
        var wrongNamespace = TestModels.Type("Contoso.Application.Status", kind: TypeKind.Enum);
        var wrongKind = TestModels.Type("Contoso.Domain.Order", kind: TypeKind.Class);

        var project = TestModels.Project("Contoso.Domain", types: [matchingEnum, wrongNamespace, wrongKind]);
        var model = TestModels.Repository(project);

        var candidates = new EnumSelector("Contoso.Domain").SelectCandidates(model).Cast<TypeModel>().ToList();

        var result = Assert.Single(candidates);
        Assert.Equal("Contoso.Domain.Status", result.FullName);
    }

    [Fact]
    public void SelectCandidates_DefaultsToAllNamespaces()
    {
        var enumType = TestModels.Type("Contoso.Domain.Status", kind: TypeKind.Enum);
        var project = TestModels.Project("Contoso.Domain", types: [enumType]);
        var model = TestModels.Repository(project);

        var candidates = new EnumSelector().SelectCandidates(model).ToList();

        Assert.Single(candidates);
    }

    [Fact]
    public void SelectCandidates_ReturnsEmpty_WhenRepositoryHasNoProjects()
    {
        var model = TestModels.Repository();

        var candidates = new EnumSelector().SelectCandidates(model).ToList();

        Assert.Empty(candidates);
    }
}
