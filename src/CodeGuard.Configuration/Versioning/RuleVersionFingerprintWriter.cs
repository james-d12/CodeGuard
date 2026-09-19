using CodeGuard.Configuration.Yaml;

namespace CodeGuard.Configuration.Versioning;

/// <summary>
/// Writes a recomputed <c>versionFingerprint</c> value into a rule YAML file's <c>metadata</c>
/// block - backs <c>codeguard rules validate --update-fingerprints</c>, see
/// docs/RULE_VERSIONING_PLAN.md. Splicing mechanics (exact-offset text splice rather than a YAML
/// serializer round-trip, so comments/key order/formatting elsewhere are untouched) are shared with
/// <c>CodeGuard.Configuration.Sources.RuleSourceFingerprintWriter</c> via <see cref="YamlMappingSplicer"/> -
/// this class only supplies the mapping path (<c>metadata</c>) and leaf key (<c>versionFingerprint</c>)
/// that differ from that writer's <c>metadata.source.fingerprint</c>.
/// </summary>
public static class RuleVersionFingerprintWriter
{
    private static readonly string[] MappingPath = ["metadata"];

    public static void WriteFingerprint(string sourceFile, string fingerprint) =>
        File.WriteAllText(sourceFile, SpliceFingerprint(File.ReadAllText(sourceFile), fingerprint));

    internal static string SpliceFingerprint(string yamlText, string fingerprint) =>
        YamlMappingSplicer.SpliceScalar(yamlText, MappingPath, "versionFingerprint", fingerprint);
}
