using System.Text.Json.Nodes;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace CodeGuard.Configuration.Parsing;

internal static class YamlDocumentReader
{
    public static JsonNode? ReadDocument(string yamlText)
    {
        var yamlStream = new YamlStream();
        using var reader = new StringReader(yamlText);
        yamlStream.Load(reader);

        if (yamlStream.Documents.Count == 0)
        {
            return null;
        }

        return Convert(yamlStream.Documents[0].RootNode);
    }

    private static JsonNode? Convert(YamlNode node) => node switch
    {
        YamlScalarNode scalar => ConvertScalar(scalar),
        YamlSequenceNode sequence => new JsonArray(sequence.Children.Select(Convert).ToArray()),
        YamlMappingNode mapping => ConvertMapping(mapping),
        _ => throw new NotSupportedException($"Unsupported YAML node type '{node.GetType()}'.")
    };

    private static JsonObject ConvertMapping(YamlMappingNode mapping)
    {
        var obj = new JsonObject();
        foreach (var entry in mapping.Children)
        {
            var key = ((YamlScalarNode)entry.Key).Value
                ?? throw new FormatException("YAML mapping keys must be non-null scalars.");
            obj[key] = Convert(entry.Value);
        }

        return obj;
    }

    private static JsonNode? ConvertScalar(YamlScalarNode scalar)
    {
        var value = scalar.Value;
        if (value is null)
        {
            return null;
        }

        var isQuotedOrBlock = scalar.Style is ScalarStyle.SingleQuoted or ScalarStyle.DoubleQuoted
            or ScalarStyle.Literal or ScalarStyle.Folded;

        if (!isQuotedOrBlock)
        {
            if (value is "~" or "null") return null;
            if (bool.TryParse(value, out var boolValue)) return JsonValue.Create(boolValue);
            if (int.TryParse(value, out var intValue)) return JsonValue.Create(intValue);
            if (double.TryParse(value, out var doubleValue)) return JsonValue.Create(doubleValue);
        }

        return JsonValue.Create(value);
    }
}
