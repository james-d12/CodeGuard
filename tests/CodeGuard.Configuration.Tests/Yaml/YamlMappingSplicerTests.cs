using CodeGuard.Configuration.Yaml;

namespace CodeGuard.Configuration.Tests.Yaml;

public sealed class YamlMappingSplicerTests
{
    [Fact]
    public void SpliceScalar_FlowStyleMappingWithNoTrailingWhitespaceBeforeClosingBrace_StillInsertsCorrectly()
    {
        // No space anywhere around the closing brace - the trim-back-whitespace loop must handle
        // zero iterations just as correctly as the "{ a: b }"-with-spacing case other tests cover.
        const string original = "id: DDD-ENTITY-001\nmetadata: {trackVersion: true}\ntarget:\n  kind: class\n";

        var result = YamlMappingSplicer.SpliceScalar(original, ["metadata"], "versionFingerprint", "sha256:new");

        Assert.Contains("metadata: {trackVersion: true, versionFingerprint: sha256:new }", result);
    }

    // No test for the "flow-style mapping missing its closing brace" FormatException: a document
    // whose flow mapping was successfully parsed by YamlDotNet must, by definition, have had a
    // matching '}' somewhere in the source text after the last value - malformed input instead fails
    // during YamlStream.Load itself, before this code ever runs. Confirmed empirically (a truncated
    // `{trackVersion: true` with no closing brace throws YamlDotNet's own parse exception, not this
    // one). Unlike the mapping-path checks below (both genuinely reachable and tested), this guard
    // appears to be unreachable in practice via any input that gets this far at all.

    [Fact]
    public void SpliceScalar_NoTrailingNewlineAfterLastEntry_AppendsAtEndOfFile()
    {
        // The last mapping entry is also the last thing in the file - there's no following newline to
        // insert after, so the new key must be appended at the very end instead.
        const string original = "id: DDD-ENTITY-001\nmetadata:\n  trackVersion: true";

        var result = YamlMappingSplicer.SpliceScalar(original, ["metadata"], "versionFingerprint", "sha256:new");

        Assert.Equal("id: DDD-ENTITY-001\nmetadata:\n  trackVersion: true\n  versionFingerprint: sha256:new", result);
    }

    [Fact]
    public void SpliceScalar_MappingPathNotPresent_ThrowsInvalidOperationException()
    {
        // Callers are documented as only splicing into a path they've already confirmed exists -
        // this proves that invariant actually fails loudly rather than NullReferenceException-ing if
        // it's ever violated.
        const string original = "id: DDD-ENTITY-001\ntarget:\n  kind: class\n";

        var exception = Assert.Throws<InvalidOperationException>(
            () => YamlMappingSplicer.SpliceScalar(original, ["metadata"], "versionFingerprint", "sha256:new"));

        Assert.Contains("metadata", exception.Message);
    }

    [Fact]
    public void SpliceScalar_MappingPathKeyIsNotAMapping_ThrowsInvalidOperationException()
    {
        const string original = "id: DDD-ENTITY-001\nmetadata: not-a-mapping\ntarget:\n  kind: class\n";

        Assert.Throws<InvalidOperationException>(
            () => YamlMappingSplicer.SpliceScalar(original, ["metadata"], "versionFingerprint", "sha256:new"));
    }
}
