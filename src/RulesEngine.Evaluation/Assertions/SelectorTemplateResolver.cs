using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace RulesEngine.Evaluation.Assertions;

internal static class SelectorTemplateResolver
{
    private static readonly Regex PlaceholderPattern = new(@"^\$\{(\w+)\}$");

    public static JsonObject Resolve(JsonObject template, object candidate) =>
        (JsonObject)ResolveNode(template, candidate)!;

    private static JsonNode? ResolveNode(JsonNode? node, object candidate) => node switch
    {
        JsonObject obj => new JsonObject(obj.Select(kvp =>
            KeyValuePair.Create(kvp.Key, ResolveNode(kvp.Value, candidate)))),
        JsonArray array => new JsonArray(array.Select(item => ResolveNode(item, candidate)).ToArray()),
        JsonValue value when value.TryGetValue<string>(out var text) => JsonValue.Create(ResolveString(text, candidate)),
        _ => node?.DeepClone()
    };

    private static string ResolveString(string value, object candidate)
    {
        var match = PlaceholderPattern.Match(value);
        if (!match.Success)
        {
            return value;
        }

        var propertyName = match.Groups[1].Value;
        var property = candidate.GetType().GetProperty(propertyName);
        return property?.GetValue(candidate)?.ToString() ?? value;
    }
}
