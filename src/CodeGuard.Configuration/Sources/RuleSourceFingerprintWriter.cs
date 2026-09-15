using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace CodeGuard.Configuration.Sources;

/// <summary>
/// Writes a recomputed <c>fingerprint</c> value into a rule YAML file's <c>metadata.source</c> block -
/// backs <c>codeguard rules validate --update-fingerprints</c>, see
/// docs/done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md. Deliberately does not round-trip the file through any
/// YAML *serializer* (every other YAML write in this repo, e.g. <c>RuleYamlWriter</c>, fully
/// regenerates a file from a fresh in-memory model, which would drop comments/reorder keys). Instead
/// this uses YamlDotNet's <c>RepresentationModel</c> only to locate exact character offsets in
/// the original text (every <see cref="YamlNode"/> carries <c>Start</c>/<c>End</c> marks from the
/// parse - the same representation <c>YamlDocumentReader</c> already loads, just for reading), then
/// splices the raw text directly. Everything else in the file - comments, key order, quoting, blank
/// lines - is untouched.
/// </summary>
public static class RuleSourceFingerprintWriter
{
    public static void WriteFingerprint(string sourceFile, string fingerprint) =>
        File.WriteAllText(sourceFile, SpliceFingerprint(File.ReadAllText(sourceFile), fingerprint));

    internal static string SpliceFingerprint(string yamlText, string fingerprint)
    {
        var newline = yamlText.Contains("\r\n") ? "\r\n" : "\n";

        var yamlStream = new YamlStream();
        using (var reader = new StringReader(yamlText))
        {
            yamlStream.Load(reader);
        }

        var root = (YamlMappingNode)yamlStream.Documents[0].RootNode;
        var source = GetMapping(GetMapping(root, "metadata"), "source");

        if (source.Children.TryGetValue(new YamlScalarNode("fingerprint"), out var existingValue))
        {
            return yamlText[..(int)existingValue.Start.Index] + fingerprint + yamlText[(int)existingValue.End.Index..];
        }

        var lastEntry = source.Children.Last();

        if (source.Style == MappingStyle.Flow)
        {
            var closeBrace = yamlText.IndexOf('}', (int)lastEntry.Value.End.Index);
            if (closeBrace < 0)
            {
                throw new FormatException("Expected a closing '}' for flow-style `metadata.source` mapping.");
            }

            // Insert right after the last value's own trailing whitespace before '}', then re-add a
            // single space before '}' - keeps "{ a: b }" spacing instead of leaving "b }" collapsed
            // into "b, fingerprint: ...}".
            var flowInsertAt = closeBrace;
            while (flowInsertAt > 0 && char.IsWhiteSpace(yamlText[flowInsertAt - 1]))
            {
                flowInsertAt--;
            }

            return yamlText[..flowInsertAt] + $", fingerprint: {fingerprint} " + yamlText[closeBrace..];
        }

        var indent = new string(' ', (int)(lastEntry.Key.Start.Column - 1));
        var lastValueEnd = (int)lastEntry.Value.End.Index;
        var nextNewline = yamlText.IndexOf('\n', lastValueEnd);

        if (nextNewline < 0)
        {
            return yamlText + newline + indent + $"fingerprint: {fingerprint}";
        }

        var insertAt = nextNewline + 1;
        return yamlText[..insertAt] + indent + $"fingerprint: {fingerprint}" + newline + yamlText[insertAt..];
    }

    private static YamlMappingNode GetMapping(YamlMappingNode parent, string key)
    {
        if (!parent.Children.TryGetValue(new YamlScalarNode(key), out var node) || node is not YamlMappingNode mapping)
        {
            throw new InvalidOperationException(
                $"Expected a `{key}` mapping - this should be unreachable, `RuleSourceChecker` only calls " +
                "into fingerprint write-back for rules that already have `metadata.source.file` set.");
        }

        return mapping;
    }
}
