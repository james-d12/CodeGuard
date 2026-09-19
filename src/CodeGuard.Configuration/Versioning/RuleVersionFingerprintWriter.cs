using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace CodeGuard.Configuration.Versioning;

/// <summary>
/// Writes a recomputed <c>versionFingerprint</c> value into a rule YAML file's <c>metadata</c>
/// block - backs <c>codeguard rules validate --update-fingerprints</c>, see
/// docs/RULE_VERSIONING_PLAN.md. Mirrors <c>CodeGuard.Configuration.Sources.RuleSourceFingerprintWriter</c>
/// exactly (splices raw text at exact character offsets rather than round-tripping through a YAML
/// serializer, to leave comments/key order/formatting untouched) but targets <c>metadata.versionFingerprint</c>
/// directly rather than <c>metadata.source.fingerprint</c>.
/// </summary>
public static class RuleVersionFingerprintWriter
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
        var metadata = GetMapping(root, "metadata");

        if (metadata.Children.TryGetValue(new YamlScalarNode("versionFingerprint"), out var existingValue))
        {
            return yamlText[..(int)existingValue.Start.Index] + fingerprint + yamlText[(int)existingValue.End.Index..];
        }

        var lastEntry = metadata.Children.Last();

        if (metadata.Style == MappingStyle.Flow)
        {
            var closeBrace = yamlText.IndexOf('}', (int)lastEntry.Value.End.Index);
            if (closeBrace < 0)
            {
                throw new FormatException("Expected a closing '}' for flow-style `metadata` mapping.");
            }

            var flowInsertAt = closeBrace;
            while (flowInsertAt > 0 && char.IsWhiteSpace(yamlText[flowInsertAt - 1]))
            {
                flowInsertAt--;
            }

            return yamlText[..flowInsertAt] + $", versionFingerprint: {fingerprint} " + yamlText[closeBrace..];
        }

        var indent = new string(' ', (int)(lastEntry.Key.Start.Column - 1));
        var lastValueEnd = (int)lastEntry.Value.End.Index;
        var nextNewline = yamlText.IndexOf('\n', lastValueEnd);

        if (nextNewline < 0)
        {
            return yamlText + newline + indent + $"versionFingerprint: {fingerprint}";
        }

        var insertAt = nextNewline + 1;
        return yamlText[..insertAt] + indent + $"versionFingerprint: {fingerprint}" + newline + yamlText[insertAt..];
    }

    private static YamlMappingNode GetMapping(YamlMappingNode parent, string key)
    {
        if (!parent.Children.TryGetValue(new YamlScalarNode(key), out var node) || node is not YamlMappingNode mapping)
        {
            throw new InvalidOperationException(
                $"Expected a `{key}` mapping - this should be unreachable, `RuleVersionChecker` only calls " +
                "into fingerprint write-back for rules that already have `metadata.trackVersion: true` set.");
        }

        return mapping;
    }
}
