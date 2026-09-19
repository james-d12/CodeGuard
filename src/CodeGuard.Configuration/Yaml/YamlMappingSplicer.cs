using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace CodeGuard.Configuration.Yaml;

/// <summary>
/// Splices a scalar leaf value into a specific nested YAML mapping by exact character offset,
/// leaving everything else in the file - comments, key order, quoting, blank lines - untouched.
/// Deliberately does not round-trip the file through any YAML *serializer* (every other YAML write
/// in this repo, e.g. <c>RuleYamlWriter</c>, fully regenerates a file from a fresh in-memory model,
/// which would drop comments/reorder keys). Instead this uses YamlDotNet's <c>RepresentationModel</c>
/// only to locate exact character offsets in the original text (every <see cref="YamlNode"/> carries
/// <c>Start</c>/<c>End</c> marks from the parse), then splices the raw text directly.
///
/// Shared by <c>CodeGuard.Configuration.Sources.RuleSourceFingerprintWriter</c> (targets
/// <c>metadata.source.fingerprint</c>) and <c>CodeGuard.Configuration.Versioning.RuleVersionFingerprintWriter</c>
/// (targets <c>metadata.versionFingerprint</c>) - the two differ only in which nested mapping they
/// target (<paramref name="mappingPath"/>) and which leaf key they write (<paramref name="leafKey"/>);
/// the splicing mechanics (replace-in-place, flow-style vs. block-style insertion, indentation,
/// line-ending preservation) are identical, so both delegate here rather than duplicating it.
/// </summary>
internal static class YamlMappingSplicer
{
    public static string SpliceScalar(string yamlText, IReadOnlyList<string> mappingPath, string leafKey, string value)
    {
        var newline = yamlText.Contains("\r\n") ? "\r\n" : "\n";

        var yamlStream = new YamlStream();
        using (var reader = new StringReader(yamlText))
        {
            yamlStream.Load(reader);
        }

        var mapping = (YamlMappingNode)yamlStream.Documents[0].RootNode;
        foreach (var key in mappingPath)
        {
            mapping = GetMapping(mapping, key);
        }

        if (mapping.Children.TryGetValue(new YamlScalarNode(leafKey), out var existingValue))
        {
            return yamlText[..(int)existingValue.Start.Index] + value + yamlText[(int)existingValue.End.Index..];
        }

        var lastEntry = mapping.Children.Last();
        var lastValueEnd = (int)GetTrueEnd(lastEntry.Value);

        if (mapping.Style == MappingStyle.Flow)
        {
            var closeBrace = yamlText.IndexOf('}', lastValueEnd);
            if (closeBrace < 0)
            {
                throw new FormatException($"Expected a closing '}}' for flow-style '{mappingPath[^1]}' mapping.");
            }

            // Insert right after the last value's own trailing whitespace before '}', then re-add a
            // single space before '}' - keeps "{ a: b }" spacing instead of leaving "b }" collapsed
            // into "b, key: value}".
            var flowInsertAt = closeBrace;
            while (flowInsertAt > 0 && char.IsWhiteSpace(yamlText[flowInsertAt - 1]))
            {
                flowInsertAt--;
            }

            return yamlText[..flowInsertAt] + $", {leafKey}: {value} " + yamlText[closeBrace..];
        }

        var indent = new string(' ', (int)(lastEntry.Key.Start.Column - 1));
        var nextNewline = yamlText.IndexOf('\n', lastValueEnd);

        if (nextNewline < 0)
        {
            return yamlText + newline + indent + $"{leafKey}: {value}";
        }

        var insertAt = nextNewline + 1;
        return yamlText[..insertAt] + indent + $"{leafKey}: {value}" + newline + yamlText[insertAt..];
    }

    /// <summary>
    /// A <see cref="YamlNode.End"/> mark is only reliable on a <see cref="YamlScalarNode"/> - on a
    /// <see cref="YamlMappingNode"/>/<see cref="YamlSequenceNode"/>, YamlDotNet leaves it equal to
    /// <see cref="YamlNode.Start"/> rather than advancing it past the node's nested content. Using
    /// that directly (as an earlier version of this splicer did) inserts mid-structure whenever the
    /// mapping's last entry is itself a container - e.g. a rule's last top-level key being
    /// <c>assertions:</c>, a sequence of mappings, corrupted the file by inserting right after
    /// <c>must_inherit_from:</c> and before its own nested <c>type:</c> key. Recursing to the last
    /// actual scalar in the tree finds the true end of content instead.
    /// </summary>
    private static long GetTrueEnd(YamlNode node) => node switch
    {
        YamlScalarNode scalar => scalar.End.Index,
        YamlMappingNode mapping when mapping.Children.Count > 0 => GetTrueEnd(mapping.Children.Last().Value),
        YamlSequenceNode sequence when sequence.Children.Count > 0 => GetTrueEnd(sequence.Children[^1]),
        _ => node.End.Index
    };

    private static YamlMappingNode GetMapping(YamlMappingNode parent, string key)
    {
        if (!parent.Children.TryGetValue(new YamlScalarNode(key), out var node) || node is not YamlMappingNode mapping)
        {
            throw new InvalidOperationException(
                $"Expected a '{key}' mapping - callers must only splice into a mapping path they have " +
                "already confirmed is present.");
        }

        return mapping;
    }
}
