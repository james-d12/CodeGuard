using System.Text.Json.Nodes;
using CodeGuard.Configuration.Parsing;
using CodeGuard.Configuration.Testing;

namespace CodeGuard.Configuration.Tests.Testing;

public sealed class TestSetupBuilderTests
{
    private static JsonObject Setup(string json) => JsonNode.Parse(json)!.AsObject();

    [Fact]
    public void Build_ParsesFiles()
    {
        var model = TestSetupBuilder.Build(Setup("""
            { "files": [ { "path": "azure-pipelines.yaml" } ] }
            """));

        var file = Assert.Single(model.Files);
        Assert.Equal("azure-pipelines.yaml", file.RelativePath);
    }

    [Fact]
    public void Build_ParsesFlatTypesShortcut_IntoSyntheticProject()
    {
        var model = TestSetupBuilder.Build(Setup("""
            {
              "types": [
                {
                  "name": "Order",
                  "namespace": "Contoso.Domain",
                  "baseType": "Entity<Guid>",
                  "interfaces": ["IAggregateRoot"],
                  "methods": [ { "name": "Create" } ]
                }
              ]
            }
            """));

        var solution = Assert.Single(model.Solutions);
        var project = Assert.Single(solution.Projects);
        var type = Assert.Single(project.Types);

        Assert.Equal("Contoso.Domain.Order", type.FullName);
        Assert.Equal("Entity<Guid>", type.BaseType);
        Assert.Equal(["IAggregateRoot"], type.Interfaces);
        Assert.Equal(project.Name, type.ProjectName);
        Assert.Single(type.Methods);
        Assert.Equal("Create", type.Methods[0].Name);
    }

    [Fact]
    public void Build_ParsesFullProjectForm_WithProjectReferences()
    {
        var model = TestSetupBuilder.Build(Setup("""
            {
              "projects": [
                { "name": "Domain" },
                { "name": "Infrastructure" }
              ]
            }
            """));

        var solution = Assert.Single(model.Solutions);
        Assert.Equal(["Domain", "Infrastructure"], solution.Projects.Select(p => p.Name));
    }

    [Fact]
    public void Build_ProjectReferences_AreCarriedThrough()
    {
        var model = TestSetupBuilder.Build(Setup("""
            {
              "projects": [
                { "name": "Domain", "projectReferences": ["Infrastructure"] }
              ]
            }
            """));

        var project = Assert.Single(model.Solutions.Single().Projects);
        Assert.Equal(["Infrastructure"], project.ProjectReferences);
    }

    [Fact]
    public void Build_ParsesCallSites()
    {
        var model = TestSetupBuilder.Build(Setup("""
            {
              "callSites": [
                { "invokedMember": "Result", "containingType": "Contoso.Domain.Order" }
              ]
            }
            """));

        var callSite = Assert.Single(model.CallSites);
        Assert.Equal("Result", callSite.InvokedMember);
        Assert.Equal("Contoso.Domain.Order", callSite.ContainingType);
    }

    [Fact]
    public void Build_ParsesDirectories()
    {
        var model = TestSetupBuilder.Build(Setup("""
            { "directories": ["src"] }
            """));

        Assert.Equal(["src"], model.Directories);
    }

    [Fact]
    public void Build_ParsesSwitches()
    {
        var model = TestSetupBuilder.Build(Setup("""
            { "switches": [ { "containingMethod": "M", "containingType": "T", "hasDefaultOrDiscardArm": true } ] }
            """));

        var @switch = Assert.Single(model.Switches);
        Assert.Equal("M", @switch.ContainingMethod);
        Assert.Equal("T", @switch.ContainingType);
        Assert.True(@switch.HasDefaultOrDiscardArm);
    }

    [Fact]
    public void Build_ParsesThrowSites()
    {
        var model = TestSetupBuilder.Build(Setup("""
            { "throwSites": [ { "exceptionTypeName": "System.ArgumentException", "isFirstStatementInMethod": true } ] }
            """));

        var throwSite = Assert.Single(model.ThrowSites);
        Assert.Equal("System.ArgumentException", throwSite.ExceptionTypeName);
        Assert.True(throwSite.IsFirstStatementInMethod);
    }

    [Fact]
    public void Build_ParsesMutationSites()
    {
        var model = TestSetupBuilder.Build(Setup("""
            { "mutationSites": [ { "targetMemberName": "_field" } ] }
            """));

        var mutationSite = Assert.Single(model.MutationSites);
        Assert.Equal("_field", mutationSite.TargetMemberName);
    }

    [Fact]
    public void Build_ParsesTryBlocks()
    {
        var model = TestSetupBuilder.Build(Setup("""
            { "tryBlocks": [ { "catchClauseCount": 2, "catchTypeNames": ["System.Exception"] } ] }
            """));

        var tryBlock = Assert.Single(model.TryBlocks);
        Assert.Equal(2, tryBlock.CatchClauseCount);
        Assert.Equal(["System.Exception"], tryBlock.CatchTypeNames);
    }

    [Fact]
    public void Build_ParsesMethodBodyShapes()
    {
        var model = TestSetupBuilder.Build(Setup("""
            { "methodBodyShapes": [ { "statementCount": 1, "isSingleBaseCallDelegation": true } ] }
            """));

        var shape = Assert.Single(model.MethodBodyShapes);
        Assert.Equal(1, shape.StatementCount);
        Assert.True(shape.IsSingleBaseCallDelegation);
    }

    [Fact]
    public void Build_ParsesDiagnostics()
    {
        var model = TestSetupBuilder.Build(Setup("""
            { "diagnostics": [ { "id": "CS1591", "message": "Missing XML comment" } ] }
            """));

        var diagnostic = Assert.Single(model.Diagnostics);
        Assert.Equal("CS1591", diagnostic.Id);
        Assert.Equal("Missing XML comment", diagnostic.Message);
    }
}
