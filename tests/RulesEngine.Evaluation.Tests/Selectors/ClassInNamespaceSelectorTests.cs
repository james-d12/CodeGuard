using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Selectors;

namespace RulesEngine.Evaluation.Tests.Selectors;

public class ClassInNamespaceSelectorTests
{
    [Fact]
    public void SelectCandidates_ReturnsOnlyClassesMatchingNamespacePattern()
    {
        var matching = CreateType("Contoso.Domain.Entities.Order", "Contoso.Domain.Entities", TypeKind.Class);
        var wrongNamespace = CreateType("Contoso.Application.Commands.PlaceOrder", "Contoso.Application.Commands", TypeKind.Class);
        var wrongKind = CreateType("Contoso.Domain.Entities.IOrder", "Contoso.Domain.Entities", TypeKind.Interface);

        var model = BuildModel(matching, wrongNamespace, wrongKind);
        var selector = new ClassInNamespaceSelector("*.Domain.Entities");

        var candidates = selector.SelectCandidates(model).Cast<TypeModel>().ToList();

        Assert.Single(candidates);
        Assert.Equal("Contoso.Domain.Entities.Order", candidates[0].FullName);
    }

    private static TypeModel CreateType(string fullName, string ns, TypeKind kind) => new(
        Name: fullName.Split('.')[^1],
        FullName: fullName,
        Namespace: ns,
        Kind: kind,
        BaseType: null,
        Interfaces: [],
        Accessibility: Accessibility.Public,
        Modifiers: TypeModifiers.None,
        Attributes: [],
        Methods: [],
        Properties: [],
        Constructors: [],
        Fields: [],
        ProjectName: "Contoso.Domain",
        FilePath: "Order.cs",
        Line: 1,
        Column: 1);

    private static RepositoryModel BuildModel(params TypeModel[] types)
    {
        var project = new ProjectModel(
            "Contoso.Domain", "Contoso.Domain.csproj", "net10.0", "Microsoft.NET.Sdk",
            [], [], new Dictionary<string, string>(), types);
        var solution = new SolutionModel("Contoso.sln", [project]);
        return new RepositoryModel(".", [solution], []);
    }
}
