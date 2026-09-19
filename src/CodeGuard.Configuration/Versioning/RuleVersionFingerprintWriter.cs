using CodeGuard.Configuration.Yaml;

namespace CodeGuard.Configuration.Versioning;

/// <summary>
/// Writes a recomputed top-level <c>versionFingerprint</c> value into a rule YAML file - backs
/// <c>codeguard rules validate --update-fingerprints</c>, see docs/RULE_VERSIONING_PLAN.md. Splicing
/// mechanics (exact-offset text splice rather than a YAML serializer round-trip, so comments/key
/// order/formatting elsewhere are untouched) are shared with
/// <c>CodeGuard.Configuration.Sources.RuleSourceFingerprintWriter</c> via
/// <see cref="YamlMappingSplicer"/> - this class only supplies an empty mapping path (splice directly
/// into the document root) and the leaf key (<c>versionFingerprint</c>). Because there's no opt-in
/// wrapper object to insert after, a first-time write lands after whatever the file's *last*
/// top-level key happens to be (typically <c>tests:</c>) rather than next to <c>version:</c> - a
/// cosmetic quirk, not a correctness issue.
/// </summary>
public static class RuleVersionFingerprintWriter
{
    private static readonly string[] MappingPath = [];

    public static void WriteFingerprint(string sourceFile, string fingerprint) =>
        File.WriteAllText(sourceFile, SpliceFingerprint(File.ReadAllText(sourceFile), fingerprint));

    internal static string SpliceFingerprint(string yamlText, string fingerprint) =>
        YamlMappingSplicer.SpliceScalar(yamlText, MappingPath, "versionFingerprint", fingerprint);
}
