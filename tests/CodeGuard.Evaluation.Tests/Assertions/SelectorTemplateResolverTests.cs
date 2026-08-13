using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

public class SelectorTemplateResolverTests
{
    [Fact]
    public void Resolve_RecursesThroughNestedJsonObjects()
    {
        var candidate = TestModels.Type("Contoso.Domain.Order");
        var template = new JsonObject
        {
            ["outer"] = new JsonObject { ["inner"] = "${FullName}" }
        };

        var resolved = SelectorTemplateResolver.Resolve(template, candidate);

        Assert.Equal("Contoso.Domain.Order", resolved["outer"]!["inner"]!.GetValue<string>());
    }

    [Fact]
    public void Resolve_RecursesThroughJsonArrays()
    {
        var candidate = TestModels.Type("Contoso.Domain.Order");
        var template = new JsonObject
        {
            ["items"] = new JsonArray(JsonValue.Create("${FullName}"), new JsonObject { ["namespace"] = "${Namespace}" })
        };

        var resolved = SelectorTemplateResolver.Resolve(template, candidate);
        var items = resolved["items"]!.AsArray();

        Assert.Equal("Contoso.Domain.Order", items[0]!.GetValue<string>());
        Assert.Equal("Contoso.Domain", items[1]!["namespace"]!.GetValue<string>());
    }

    [Fact]
    public void Resolve_DeepClonesNonStringValues_Unchanged()
    {
        var candidate = TestModels.Type("Contoso.Domain.Order");
        var template = new JsonObject
        {
            ["count"] = 42,
            ["enabled"] = true,
            ["nothing"] = null
        };

        var resolved = SelectorTemplateResolver.Resolve(template, candidate);

        Assert.Equal(42, resolved["count"]!.GetValue<int>());
        Assert.True(resolved["enabled"]!.GetValue<bool>());
        Assert.Null(resolved["nothing"]);
    }

    [Fact]
    public void Resolve_FallsBackToLiteral_WhenPlaceholderPropertyNotFoundOnCandidate()
    {
        var candidate = TestModels.Type("Contoso.Domain.Order");
        var template = new JsonObject { ["value"] = "${NoSuchProperty}" };

        var resolved = SelectorTemplateResolver.Resolve(template, candidate);

        Assert.Equal("${NoSuchProperty}", resolved["value"]!.GetValue<string>());
    }

    [Fact]
    public void Resolve_FallsBackToLiteral_WhenPlaceholderPropertyValueIsNull()
    {
        var candidate = TestModels.Type("Contoso.Domain.Order", baseType: null);
        var template = new JsonObject { ["value"] = "${BaseType}" };

        var resolved = SelectorTemplateResolver.Resolve(template, candidate);

        Assert.Equal("${BaseType}", resolved["value"]!.GetValue<string>());
    }

    [Fact]
    public void Resolve_ResolvesPlaceholder_ToStringifiedNonStringPropertyValue()
    {
        var candidate = TestModels.Type("Contoso.Domain.Order");
        var template = new JsonObject { ["value"] = "${Kind}" };

        var resolved = SelectorTemplateResolver.Resolve(template, candidate);

        Assert.Equal("Class", resolved["value"]!.GetValue<string>());
    }

    [Fact]
    public void Resolve_ResolvesPlaceholder_WhenEmbeddedInALargerString()
    {
        // Regression test: cross-entity correspondence rules (e.g. "every Command must have a
        // corresponding {Name}Handler") need a placeholder embedded in a literal suffix/prefix,
        // not just a value that IS exactly one placeholder.
        var candidate = TestModels.Type("Contoso.Application.Commands.PlaceOrder");
        var template = new JsonObject { ["name"] = "${Name}Handler" };

        var resolved = SelectorTemplateResolver.Resolve(template, candidate);

        Assert.Equal("PlaceOrderHandler", resolved["name"]!.GetValue<string>());
    }

    [Fact]
    public void Resolve_ResolvesMultiplePlaceholders_WithinTheSameString()
    {
        var candidate = TestModels.Type("Contoso.Domain.Order");
        var template = new JsonObject { ["value"] = "${Namespace}.Generated.${Name}Dto" };

        var resolved = SelectorTemplateResolver.Resolve(template, candidate);

        Assert.Equal("Contoso.Domain.Generated.OrderDto", resolved["value"]!.GetValue<string>());
    }

    [Fact]
    public void Resolve_LeavesUnmatchedEmbeddedPlaceholder_AsLiteralText()
    {
        var candidate = TestModels.Type("Contoso.Domain.Order");
        var template = new JsonObject { ["value"] = "${Name}-${NoSuchProperty}" };

        var resolved = SelectorTemplateResolver.Resolve(template, candidate);

        Assert.Equal("Order-${NoSuchProperty}", resolved["value"]!.GetValue<string>());
    }
}
