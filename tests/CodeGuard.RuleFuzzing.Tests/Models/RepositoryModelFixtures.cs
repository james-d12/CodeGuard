using System.Text.Json.Nodes;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Configuration.Testing;

namespace CodeGuard.RuleFuzzing.Tests.Models;

/// <summary>
/// Fixed virtual <see cref="RepositoryModel"/>s built via the same <see cref="TestSetupBuilder"/> used
/// by `codeguard rules test` - one per <c>CandidateKind</c>, so a crash can be isolated to a specific
/// candidate kind, plus one "kitchen sink" unioning everything for cross-kind interaction coverage.
/// </summary>
internal static class RepositoryModelFixtures
{
    public static IReadOnlyList<(string Name, JsonObject Setup)> RawSetups { get; } = BuildRawSetups();

    public static IReadOnlyList<(string Name, RepositoryModel Model)> All { get; } =
        RawSetups.Select(entry => (entry.Name, TestSetupBuilder.Build((JsonObject)entry.Setup.DeepClone()))).ToList();

    private static List<(string, JsonObject)> BuildRawSetups()
    {
        var perKind = new (string Name, JsonObject Setup)[]
        {
            ("Repository", new JsonObject { ["projects"] = new JsonArray(Project("P1")) }),
            ("Project", new JsonObject { ["projects"] = new JsonArray(Project("P1"), Project("P2")) }),
            ("Type", new JsonObject { ["types"] = new JsonArray(TypeNode()) }),
            ("Method", new JsonObject { ["types"] = new JsonArray(TypeWithMember("methods", new JsonObject { ["name"] = "M1" })) }),
            ("Property", new JsonObject { ["types"] = new JsonArray(TypeWithMember("properties", new JsonObject { ["name"] = "Prop1" })) }),
            ("Constructor", new JsonObject { ["types"] = new JsonArray(TypeWithMember("constructors", new JsonObject())) }),
            ("Field", new JsonObject { ["types"] = new JsonArray(TypeWithMember("fields", new JsonObject { ["name"] = "F1" })) }),
            ("File", new JsonObject { ["files"] = new JsonArray(FileNode()) }),
            ("Directory", new JsonObject { ["directories"] = new JsonArray("src", "src/Sub") }),
            ("CallSite", new JsonObject { ["callSites"] = new JsonArray(CallSiteNode()) }),
            ("Switch", new JsonObject { ["switches"] = new JsonArray(SwitchNode()) }),
            ("ThrowSite", new JsonObject { ["throwSites"] = new JsonArray(ThrowSiteNode()) }),
            ("MutationSite", new JsonObject { ["mutationSites"] = new JsonArray(MutationSiteNode()) }),
            ("TryBlock", new JsonObject { ["tryBlocks"] = new JsonArray(TryBlockNode()) }),
            ("MethodBodyShape", new JsonObject { ["methodBodyShapes"] = new JsonArray(MethodBodyShapeNode()) }),
            ("Diagnostic", new JsonObject { ["diagnostics"] = new JsonArray(DiagnosticNode()) })
        };

        var kitchenSink = new JsonObject
        {
            ["projects"] = new JsonArray(Project("P1")),
            ["types"] = new JsonArray(new JsonObject
            {
                ["name"] = "T1",
                ["namespace"] = "Contoso.Domain",
                ["methods"] = new JsonArray(new JsonObject { ["name"] = "M1" }),
                ["properties"] = new JsonArray(new JsonObject { ["name"] = "Prop1" }),
                ["constructors"] = new JsonArray(new JsonObject()),
                ["fields"] = new JsonArray(new JsonObject { ["name"] = "F1" })
            }),
            ["files"] = new JsonArray(FileNode()),
            ["directories"] = new JsonArray("src", "src/Sub"),
            ["callSites"] = new JsonArray(CallSiteNode()),
            ["switches"] = new JsonArray(SwitchNode()),
            ["throwSites"] = new JsonArray(ThrowSiteNode()),
            ["mutationSites"] = new JsonArray(MutationSiteNode()),
            ["tryBlocks"] = new JsonArray(TryBlockNode()),
            ["methodBodyShapes"] = new JsonArray(MethodBodyShapeNode()),
            ["diagnostics"] = new JsonArray(DiagnosticNode())
        };

        return perKind.Append(("KitchenSink", kitchenSink)).ToList();
    }

    private static JsonObject Project(string name) => new() { ["name"] = name };

    private static JsonObject TypeNode() => new() { ["name"] = "T1", ["namespace"] = "Contoso.Domain" };

    private static JsonObject TypeWithMember(string memberKey, JsonObject member) => new()
    {
        ["name"] = "T1",
        ["namespace"] = "Contoso.Domain",
        [memberKey] = new JsonArray(member)
    };

    private static JsonObject FileNode() => new() { ["path"] = "src/A.cs", ["content"] = "class A {}" };

    private static JsonObject CallSiteNode() => new() { ["invokedMember"] = "Foo", ["containingMethod"] = "M1", ["containingType"] = "T1" };

    private static JsonObject SwitchNode() => new()
    {
        ["containingMethod"] = "M1",
        ["containingType"] = "T1",
        ["armLabels"] = new JsonArray("A", "B"),
        ["hasDefaultOrDiscardArm"] = true
    };

    private static JsonObject ThrowSiteNode() => new()
    {
        ["containingMethod"] = "M1",
        ["containingType"] = "T1",
        ["exceptionTypeName"] = "System.InvalidOperationException"
    };

    private static JsonObject MutationSiteNode() => new()
    {
        ["containingMethod"] = "M1",
        ["containingType"] = "T1",
        ["targetMemberName"] = "F1"
    };

    private static JsonObject TryBlockNode() => new()
    {
        ["containingMethod"] = "M1",
        ["containingType"] = "T1",
        ["catchClauseCount"] = 1,
        ["catchTypeNames"] = new JsonArray("System.Exception")
    };

    private static JsonObject MethodBodyShapeNode() => new()
    {
        ["containingMethod"] = "M1",
        ["containingType"] = "T1",
        ["statementCount"] = 3
    };

    private static JsonObject DiagnosticNode() => new() { ["id"] = "CG001", ["message"] = "diagnostic message" };
}
