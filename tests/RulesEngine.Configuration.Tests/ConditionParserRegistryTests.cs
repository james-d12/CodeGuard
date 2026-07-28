using System.Text.Json.Nodes;
using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Configuration.Parsing;

namespace RulesEngine.Configuration.Tests;

public class ConditionParserRegistryTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    private readonly ConditionParserRegistry _parser =
        DefaultParsers.CreateConditionRegistry(DefaultParsers.CreateAssertionRegistry(DefaultParsers.CreateSelectorRegistry()));

    private static JsonObject Node(string json) => JsonNode.Parse(json)!.AsObject();

    private static TypeModel Entity(string? baseType) => new(
        Name: "Order",
        FullName: "Contoso.Domain.Order",
        Namespace: "Contoso.Domain",
        Kind: TypeKind.Class,
        BaseType: baseType,
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

    [Fact]
    public void Parse_LeafAssertionKind_WrapsInAssertionCondition()
    {
        var condition = _parser.Parse(Node("""
            { "must_inherit_from": { "type": "Contoso.Domain.Entity<TId>" } }
            """));

        Assert.True(condition.Evaluate(Entity("Contoso.Domain.Entity<TId>"), EmptyModel));
        Assert.False(condition.Evaluate(Entity(null), EmptyModel));
    }

    [Fact]
    public void Parse_Not_NegatesChild()
    {
        var condition = _parser.Parse(Node("""
            { "not": { "must_inherit_from": { "type": "Contoso.Domain.Entity<TId>" } } }
            """));

        Assert.False(condition.Evaluate(Entity("Contoso.Domain.Entity<TId>"), EmptyModel));
        Assert.True(condition.Evaluate(Entity(null), EmptyModel));
    }

    [Fact]
    public void Parse_And_RequiresAllChildren()
    {
        var condition = _parser.Parse(Node("""
            {
              "and": [
                { "must_inherit_from": { "type": "Contoso.Domain.Entity<TId>" } },
                { "must_implement": { "interface": "Contoso.Domain.IAggregateRoot" } }
              ]
            }
            """));

        Assert.False(condition.Evaluate(Entity("Contoso.Domain.Entity<TId>"), EmptyModel));
    }

    [Fact]
    public void Parse_Or_RequiresAnyChild()
    {
        var condition = _parser.Parse(Node("""
            {
              "or": [
                { "must_inherit_from": { "type": "Contoso.Domain.Entity<TId>" } },
                { "must_inherit_from": { "type": "Contoso.Domain.Aggregate" } }
              ]
            }
            """));

        Assert.True(condition.Evaluate(Entity("Contoso.Domain.Entity<TId>"), EmptyModel));
        Assert.False(condition.Evaluate(Entity("Contoso.Domain.SomethingElse"), EmptyModel));
    }

    [Fact]
    public void Parse_MultiKeyNode_Throws()
    {
        Assert.Throws<RuleParsingException>(() => _parser.Parse(Node("""
            { "must_inherit_from": { "type": "X" }, "must_implement": { "type": "Y" } }
            """)));
    }
}
