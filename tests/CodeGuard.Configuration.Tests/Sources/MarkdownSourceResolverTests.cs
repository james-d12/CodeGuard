using CodeGuard.Configuration.Sources;

namespace CodeGuard.Configuration.Tests.Sources;

public class MarkdownSourceResolverTests
{
    private const string Document = """
        # Architecture Standards

        Intro text.

        ## Domain Layer

        Domain projects must not reference Infrastructure.

        ## Application Layer

        Application projects may reference Domain.

        ### Application Layer Details

        Nested content under Application Layer.

        ## Domain Layer

        Duplicate heading text, used for the ambiguous case.
        """;

    [Fact]
    public void Resolve_NoHeading_ReturnsWholeFile()
    {
        var resolution = MarkdownSourceResolver.Resolve("Just some text.\n", heading: null);

        Assert.Equal(MarkdownResolutionKind.Resolved, resolution.Kind);
        Assert.Equal("Just some text.", resolution.Content);
        Assert.NotNull(resolution.Fingerprint);
        Assert.StartsWith("sha256:", resolution.Fingerprint);
    }

    [Fact]
    public void Resolve_UniqueHeading_ReturnsOnlyThatSection()
    {
        const string content = """
            # Title

            ## Application Layer

            Application projects may reference Domain.

            ### Application Layer Details

            Nested content under Application Layer.

            ## Next Section

            Unrelated content.
            """;

        var resolution = MarkdownSourceResolver.Resolve(content, "Application Layer");

        Assert.Equal(MarkdownResolutionKind.Resolved, resolution.Kind);
        Assert.Contains("Application projects may reference Domain.", resolution.Content);
        Assert.Contains("Nested content under Application Layer.", resolution.Content);
        Assert.DoesNotContain("Unrelated content.", resolution.Content);
    }

    [Fact]
    public void Resolve_HeadingNotFound_ReturnsSectionNotFound()
    {
        var resolution = MarkdownSourceResolver.Resolve(Document, "Nonexistent Heading");

        Assert.Equal(MarkdownResolutionKind.SectionNotFound, resolution.Kind);
        Assert.Null(resolution.Content);
        Assert.Null(resolution.Fingerprint);
    }

    [Fact]
    public void Resolve_AmbiguousHeading_ReturnsSectionAmbiguousWithLineNumbers()
    {
        var resolution = MarkdownSourceResolver.Resolve(Document, "Domain Layer");

        Assert.Equal(MarkdownResolutionKind.SectionAmbiguous, resolution.Kind);
        Assert.Equal(2, resolution.AmbiguousHeadingLines.Count);
    }

    [Fact]
    public void Resolve_SectionEndsAtNextHeadingOfSameOrShallowerLevel_NotAtDeeperNestedHeading()
    {
        const string content = """
            ## Outer

            Outer content.

            ### Inner

            Inner content.

            ## Sibling

            Sibling content.
            """;

        var resolution = MarkdownSourceResolver.Resolve(content, "Outer");

        Assert.Contains("Outer content.", resolution.Content);
        Assert.Contains("Inner content.", resolution.Content);
        Assert.DoesNotContain("Sibling content.", resolution.Content);
    }

    [Fact]
    public void Resolve_SectionAtEndOfFile_ReturnsRemainingContent()
    {
        const string content = """
            ## First

            First content.

            ## Last

            Last content.
            """;

        var resolution = MarkdownSourceResolver.Resolve(content, "Last");

        Assert.Equal("Last content.", resolution.Content);
    }

    [Fact]
    public void Resolve_EmptySection_ReturnsEmptyContent()
    {
        const string content = """
            ## Empty

            ## Next

            Next content.
            """;

        var resolution = MarkdownSourceResolver.Resolve(content, "Empty");

        Assert.Equal(MarkdownResolutionKind.Resolved, resolution.Kind);
        Assert.Equal(string.Empty, resolution.Content);
    }

    [Fact]
    public void Resolve_CrlfLineEndings_NormalizedBeforeHashing()
    {
        var lf = MarkdownSourceResolver.Resolve("## Heading\n\nSome text.\n", "Heading");
        var crlf = MarkdownSourceResolver.Resolve("## Heading\r\n\r\nSome text.\r\n", "Heading");

        Assert.Equal(lf.Fingerprint, crlf.Fingerprint);
    }

    [Fact]
    public void Resolve_TrailingWhitespaceOnLines_DoesNotAffectFingerprint()
    {
        var clean = MarkdownSourceResolver.Resolve("## Heading\n\nSome text.\n", "Heading");
        var trailingWhitespace = MarkdownSourceResolver.Resolve("## Heading   \n\nSome text.   \n", "Heading");

        Assert.Equal(clean.Fingerprint, trailingWhitespace.Fingerprint);
    }

    [Fact]
    public void Resolve_SurroundingBlankLines_DoNotAffectFingerprint()
    {
        var tight = MarkdownSourceResolver.Resolve("## Heading\nSome text.\n## Next", "Heading");
        var padded = MarkdownSourceResolver.Resolve("## Heading\n\n\nSome text.\n\n\n## Next", "Heading");

        Assert.Equal(tight.Fingerprint, padded.Fingerprint);
    }

    [Fact]
    public void Resolve_DifferentContent_ProducesDifferentFingerprint()
    {
        var a = MarkdownSourceResolver.Resolve("## Heading\n\nOriginal text.\n", "Heading");
        var b = MarkdownSourceResolver.Resolve("## Heading\n\nChanged text.\n", "Heading");

        Assert.NotEqual(a.Fingerprint, b.Fingerprint);
    }

    [Fact]
    public void Resolve_HeadingTrimsSurroundingWhitespaceFromParameter()
    {
        const string content = """
            ## Heading

            Content.
            """;

        var resolution = MarkdownSourceResolver.Resolve(content, "  Heading  ");

        Assert.Equal(MarkdownResolutionKind.Resolved, resolution.Kind);
        Assert.Equal("Content.", resolution.Content);
    }
}
