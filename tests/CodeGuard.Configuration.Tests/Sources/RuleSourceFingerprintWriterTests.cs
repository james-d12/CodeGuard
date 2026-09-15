using CodeGuard.Configuration.Sources;

namespace CodeGuard.Configuration.Tests.Sources;

public class RuleSourceFingerprintWriterTests
{
    [Fact]
    public void SpliceFingerprint_NoExistingFingerprint_InsertsAfterLastSourceKeyWithMatchingIndentation()
    {
        const string original = """
            id: DDD-ENTITY-001
            name: Some rule
            metadata:
              source:
                document: Architecture Standards
                section: Domain Layer
                statement: Some statement.
                file: docs/architecture.md
            target:
              kind: class

            """;

        var result = RuleSourceFingerprintWriter.SpliceFingerprint(original, "sha256:new");

        const string expected = """
            id: DDD-ENTITY-001
            name: Some rule
            metadata:
              source:
                document: Architecture Standards
                section: Domain Layer
                statement: Some statement.
                file: docs/architecture.md
                fingerprint: sha256:new
            target:
              kind: class

            """;

        Assert.Equal(expected, result);
    }

    [Fact]
    public void SpliceFingerprint_ExistingFingerprint_ReplacesValueOnly()
    {
        const string original = """
            id: DDD-ENTITY-001
            name: Some rule
            metadata:
              source:
                document: Architecture Standards
                file: docs/architecture.md
                fingerprint: sha256:old
            target:
              kind: class

            """;

        var result = RuleSourceFingerprintWriter.SpliceFingerprint(original, "sha256:new");

        const string expected = """
            id: DDD-ENTITY-001
            name: Some rule
            metadata:
              source:
                document: Architecture Standards
                file: docs/architecture.md
                fingerprint: sha256:new
            target:
              kind: class

            """;

        Assert.Equal(expected, result);
    }

    [Fact]
    public void SpliceFingerprint_CommentsAndKeyOrderElsewhereInFile_ArePreservedVerbatim()
    {
        const string original = """
            # A comment above the id.
            id: DDD-ENTITY-001
            name: Some rule
            metadata:
              source:
                document: Architecture Standards
                file: docs/architecture.md
            # A comment above target.
            target:
              kind: class
            assertions:
              - must_inherit_from:
                  type: "Entity<*>"

            """;

        var result = RuleSourceFingerprintWriter.SpliceFingerprint(original, "sha256:new");

        Assert.Contains("# A comment above the id.", result);
        Assert.Contains("# A comment above target.", result);
        Assert.Contains("must_inherit_from", result);
        Assert.Contains("fingerprint: sha256:new", result);
    }

    [Fact]
    public void SpliceFingerprint_ExistingFingerprintQuoted_ReplacesEntireQuotedToken()
    {
        const string original = """
            id: DDD-ENTITY-001
            name: Some rule
            metadata:
              source:
                document: Architecture Standards
                file: docs/architecture.md
                fingerprint: "sha256:old"
            target:
              kind: class

            """;

        var result = RuleSourceFingerprintWriter.SpliceFingerprint(original, "sha256:new");

        Assert.Contains("fingerprint: sha256:new", result);
        Assert.DoesNotContain("\"sha256:old\"", result);
        Assert.DoesNotContain("sha256:old", result);
    }

    [Fact]
    public void SpliceFingerprint_CrlfLineEndings_Preserved()
    {
        var original = string.Join("\r\n",
            "id: DDD-ENTITY-001",
            "name: Some rule",
            "metadata:",
            "  source:",
            "    document: Architecture Standards",
            "    file: docs/architecture.md",
            "target:",
            "  kind: class",
            "");

        var result = RuleSourceFingerprintWriter.SpliceFingerprint(original, "sha256:new");

        Assert.Contains("\r\n    fingerprint: sha256:new\r\n", result);
        Assert.DoesNotMatch(@"(?<!\r)\n", result);
    }

    [Fact]
    public void SpliceFingerprint_FlowStyleSourceMapping_InsertsBeforeClosingBrace()
    {
        const string original = """
            id: DDD-ENTITY-001
            name: Some rule
            metadata:
              source: { document: "Architecture Standards", file: "docs/architecture.md" }
            target:
              kind: class

            """;

        var result = RuleSourceFingerprintWriter.SpliceFingerprint(original, "sha256:new");

        Assert.Contains(
            """source: { document: "Architecture Standards", file: "docs/architecture.md", fingerprint: sha256:new }""",
            result);
    }

    [Fact]
    public void SpliceFingerprint_RunTwiceWithSameValue_IsIdempotent()
    {
        const string original = """
            id: DDD-ENTITY-001
            name: Some rule
            metadata:
              source:
                document: Architecture Standards
                file: docs/architecture.md
            target:
              kind: class

            """;

        var first = RuleSourceFingerprintWriter.SpliceFingerprint(original, "sha256:new");
        var second = RuleSourceFingerprintWriter.SpliceFingerprint(first, "sha256:new");

        Assert.Equal(first, second);
    }

    [Fact]
    public void WriteFingerprint_WritesToRealFile()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, """
                id: DDD-ENTITY-001
                name: Some rule
                metadata:
                  source:
                    document: Architecture Standards
                    file: docs/architecture.md
                target:
                  kind: class

                """);

            RuleSourceFingerprintWriter.WriteFingerprint(tempFile, "sha256:new");

            Assert.Contains("fingerprint: sha256:new", File.ReadAllText(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
