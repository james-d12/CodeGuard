using System.Text.Json.Nodes;

namespace CodeGuard.Configuration.Parsing;

internal static class JsonNodeExtensions
{
    public static string GetRequiredString(this JsonObject node, string property) =>
        node[property]?.GetValue<string>()
            ?? throw new RuleParsingException($"Missing required property '{property}'.");

    public static string? GetOptionalString(this JsonObject node, string property) =>
        node[property]?.GetValue<string>();

    public static bool GetOptionalBool(this JsonObject node, string property, bool defaultValue) =>
        node[property]?.GetValue<bool>() ?? defaultValue;

    public static int? GetOptionalInt(this JsonObject node, string property) =>
        node[property]?.GetValue<int>();

    public static bool? GetOptionalBoolNullable(this JsonObject node, string property) =>
        node[property]?.GetValue<bool>();

    public static IReadOnlyList<string> GetStringArray(this JsonObject node, string property) =>
        node[property]?.AsArray().Select(n => n!.GetValue<string>()).ToList() ?? [];
}
