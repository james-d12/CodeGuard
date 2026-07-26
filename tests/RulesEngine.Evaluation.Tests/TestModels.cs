using RulesEngine.Analysis.AnalysisModel;

namespace RulesEngine.Evaluation.Tests;

internal static class TestModels
{
    public static TypeModel Type(
        string fullName,
        TypeKind kind = TypeKind.Class,
        string? baseType = null,
        IReadOnlyList<string>? interfaces = null,
        IReadOnlyList<MethodModel>? methods = null,
        IReadOnlyList<PropertyModel>? properties = null,
        IReadOnlyList<ConstructorModel>? constructors = null,
        IReadOnlyList<FieldModel>? fields = null,
        string projectName = "Contoso.Domain")
    {
        var lastDot = fullName.LastIndexOf('.');
        var ns = lastDot >= 0 ? fullName[..lastDot] : string.Empty;
        var name = lastDot >= 0 ? fullName[(lastDot + 1)..] : fullName;

        return new TypeModel(
            Name: name,
            FullName: fullName,
            Namespace: ns,
            Kind: kind,
            BaseType: baseType,
            Interfaces: interfaces ?? [],
            Accessibility: Accessibility.Public,
            Modifiers: TypeModifiers.None,
            Attributes: [],
            Methods: methods ?? [],
            Properties: properties ?? [],
            Constructors: constructors ?? [],
            Fields: fields ?? [],
            ProjectName: projectName,
            FilePath: $"{name}.cs",
            Line: 1,
            Column: 1);
    }

    public static ProjectModel Project(
        string name,
        IReadOnlyList<string>? projectReferences = null,
        IReadOnlyList<PackageReferenceModel>? packageReferences = null,
        IReadOnlyList<TypeModel>? types = null) => new(
        name,
        $"{name}.csproj",
        "net10.0",
        "Microsoft.NET.Sdk",
        projectReferences ?? [],
        packageReferences ?? [],
        new Dictionary<string, string>(),
        types ?? []);

    public static RepositoryModel Repository(params ProjectModel[] projects) =>
        new(".", [new SolutionModel("Contoso.sln", projects)], []);
}
