using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace CodeGuard.Evaluation.Assertions;

internal static class SelectorTemplateResolver
{
    // Matches every `${PropName}` occurrence anywhere in a string, not just a value that IS
    // exactly one placeholder - this lets a nested selector template express something like
    // `name: "${Name}Handler"` (cross-entity correspondence by suffix), not only `name: "${Name}"`
    // verbatim.
    private static readonly Regex PlaceholderPattern = new(@"\$\{(\w+)\}");

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

    private static string ResolveString(string value, object candidate) =>
        PlaceholderPattern.Replace(value, match =>
        {
            var property = candidate.GetType().GetProperty(match.Groups[1].Value);
            return property?.GetValue(candidate)?.ToString() ?? match.Value;
        });
}
