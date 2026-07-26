using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Selectors;

namespace RulesEngine.Evaluation.Tests.Selectors;

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
}
