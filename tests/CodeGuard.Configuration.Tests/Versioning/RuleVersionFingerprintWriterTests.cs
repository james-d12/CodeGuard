using CodeGuard.Configuration.Versioning;

namespace CodeGuard.Configuration.Tests.Versioning;

public class RuleVersionFingerprintWriterTests
{
    [Fact]
    public void SpliceFingerprint_NoExistingFingerprint_InsertsAfterLastTopLevelKey()
    {
        const string original = """
            id: DDD-ENTITY-001
            name: Some rule
            version: 1
            target:
              kind: class

            """;

        var result = RuleVersionFingerprintWriter.SpliceFingerprint(original, "sha256:new");

        const string expected = """
            id: DDD-ENTITY-001
            name: Some rule
            version: 1
            target:
              kind: class
            versionFingerprint: sha256:new

            """;

        Assert.Equal(expected, result);
    }

    [Fact]
    public void SpliceFingerprint_ExistingFingerprint_ReplacesValueOnly()
    {
        const string original = """
            id: DDD-ENTITY-001
            name: Some rule
            versionFingerprint: sha256:old
            target:
              kind: class

            """;

        var result = RuleVersionFingerprintWriter.SpliceFingerprint(original, "sha256:new");

        const string expected = """
            id: DDD-ENTITY-001
            name: Some rule
            versionFingerprint: sha256:new
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
            # A comment above target.
            target:
              kind: class
            assertions:
              - must_inherit_from:
                  type: "Entity<*>"

            """;

        var result = RuleVersionFingerprintWriter.SpliceFingerprint(original, "sha256:new");

        Assert.Contains("# A comment above the id.", result);
        Assert.Contains("# A comment above target.", result);
        Assert.Contains("must_inherit_from", result);
        Assert.Contains("versionFingerprint: sha256:new", result);
    }

    [Fact]
    public void SpliceFingerprint_CrlfLineEndings_Preserved()
    {
        var original = string.Join("\r\n",
            "id: DDD-ENTITY-001",
            "name: Some rule",
            "target:",
            "  kind: class",
            "");

        var result = RuleVersionFingerprintWriter.SpliceFingerprint(original, "sha256:new");

        Assert.Contains("\r\nversionFingerprint: sha256:new", result);
        Assert.DoesNotMatch(@"(?<!\r)\n", result);
    }

    [Fact]
    public void SpliceFingerprint_NoTrailingNewline_AppendsAtEndOfFile()
    {
        const string original = "id: DDD-ENTITY-001\nname: Some rule";

        var result = RuleVersionFingerprintWriter.SpliceFingerprint(original, "sha256:new");

        Assert.Equal("id: DDD-ENTITY-001\nname: Some rule\nversionFingerprint: sha256:new", result);
    }

    [Fact]
    public void SpliceFingerprint_RunTwiceWithSameValue_IsIdempotent()
    {
        const string original = """
            id: DDD-ENTITY-001
            name: Some rule
            target:
              kind: class

            """;

        var first = RuleVersionFingerprintWriter.SpliceFingerprint(original, "sha256:new");
        var second = RuleVersionFingerprintWriter.SpliceFingerprint(first, "sha256:new");

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
                target:
                  kind: class

                """);

            RuleVersionFingerprintWriter.WriteFingerprint(tempFile, "sha256:new");

            Assert.Contains("versionFingerprint: sha256:new", File.ReadAllText(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
