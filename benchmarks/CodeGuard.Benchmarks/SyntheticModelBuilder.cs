using CodeGuard.Analysis.AnalysisModel;

namespace CodeGuard.Benchmarks;

/// <summary>
/// Builds a RepositoryModel directly (no MSBuild/Roslyn) so RuleEvaluationBenchmarks can measure
/// rule-evaluation cost in isolation, with enough candidates per namespace/interface that the
/// ExampleRules-derived synthetic rule set (see SyntheticRuleSetGenerator) has real work to do.
/// </summary>
internal static class SyntheticModelBuilder
{
    public static RepositoryModel Build(int typesPerNamespace = 100)
    {
        var entityTypes = Enumerable.Range(0, typesPerNamespace)
            .Select(i => CreateType(
                $"Entity{i}", "Contoso.Domain.Entities", "Contoso.Domain",
                baseType: i % 2 == 0 ? "Contoso.Domain.Entity<Guid>" : null,
                interfaces: []))
            .ToList();

        var eventTypes = Enumerable.Range(0, typesPerNamespace)
            .Select(i => CreateType(
                $"Event{i}", "Contoso.Domain.Events", "Contoso.Domain",
                baseType: null,
                interfaces: i % 2 == 0 ? ["Contoso.Domain.IDomainEvent"] : []))
            .ToList();

        var handlerTypes = Enumerable.Range(0, typesPerNamespace)
            .Select(i => CreateType(
                $"Handler{i}", "Contoso.Application.Handlers", "Contoso.Application",
                baseType: null,
                interfaces: i % 2 == 0 ? [$"Contoso.Application.ICommandHandler<Command{i}>"] : []))
            .ToList();

        var domainProject = new ProjectModel(
            Name: "Contoso.Domain",
            Path: "Contoso.Domain.csproj",
            TargetFramework: "net10.0",
            Sdk: "Microsoft.NET.Sdk",
            ProjectReferences: [],
            PackageReferences: [],
            Properties: new Dictionary<string, string>(),
            Types: [.. entityTypes, .. eventTypes]);

        var applicationProject = new ProjectModel(
            Name: "Contoso.Application",
            Path: "Contoso.Application.csproj",
            TargetFramework: "net10.0",
            Sdk: "Microsoft.NET.Sdk",
            ProjectReferences: ["Contoso.Domain"],
            PackageReferences: [],
            Properties: new Dictionary<string, string>(),
            Types: handlerTypes);

        var solution = new SolutionModel("Contoso.sln", [domainProject, applicationProject]);
        return new RepositoryModel(".", [solution], [], [], [], [], [], [], [], []);
    }

    private static TypeModel CreateType(string name, string ns, string projectName, string? baseType, IReadOnlyList<string> interfaces) => new(
        Name: name,
        FullName: $"{ns}.{name}",
        Namespace: ns,
        Kind: TypeKind.Class,
        BaseType: baseType,
        Interfaces: interfaces,
        Accessibility: Accessibility.Public,
        Modifiers: TypeModifiers.None,
        Attributes: [],
        Methods: [],
        Properties: [],
        Constructors: [],
        Fields: [],
        ProjectName: projectName,
        FilePath: $"{name}.cs",
        Line: 1,
        Column: 1);
}
