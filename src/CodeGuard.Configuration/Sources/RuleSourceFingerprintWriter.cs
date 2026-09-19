using CodeGuard.Configuration.Yaml;

namespace CodeGuard.Configuration.Sources;

/// <summary>
/// Writes a recomputed <c>fingerprint</c> value into a rule YAML file's <c>metadata.source</c> block -
/// backs <c>codeguard rules validate --update-fingerprints</c>, see
/// docs/done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md. Splicing mechanics (exact-offset text splice
/// rather than a YAML serializer round-trip, so comments/key order/formatting elsewhere are
/// untouched) are shared with <c>CodeGuard.Configuration.Versioning.RuleVersionFingerprintWriter</c>
/// via <see cref="YamlMappingSplicer"/> - this class only supplies the mapping path
/// (<c>metadata.source</c>) and leaf key (<c>fingerprint</c>) that differ from that writer's
/// <c>metadata.versionFingerprint</c>.
/// </summary>
public static class RuleSourceFingerprintWriter
{
    private static readonly string[] MappingPath = ["metadata", "source"];

    public static void WriteFingerprint(string sourceFile, string fingerprint) =>
        File.WriteAllText(sourceFile, SpliceFingerprint(File.ReadAllText(sourceFile), fingerprint));

    internal static string SpliceFingerprint(string yamlText, string fingerprint) =>
        YamlMappingSplicer.SpliceScalar(yamlText, MappingPath, "fingerprint", fingerprint);
}
