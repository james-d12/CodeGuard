using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeGuard.Configuration.Sources;

public enum MarkdownResolutionKind
{
    Resolved,
    SectionNotFound,
    SectionAmbiguous
}

/// <summary>
/// The outcome of resolving a <c>metadata.source.file</c>(+<c>section</c>) reference against markdown
/// content - see docs/done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md. <see cref="Content"/>/
/// <see cref="Fingerprint"/> are populated only when <see cref="Kind"/> is
/// <see cref="MarkdownResolutionKind.Resolved"/>; <see cref="AmbiguousHeadingLines"/> only for
/// <see cref="MarkdownResolutionKind.SectionAmbiguous"/> (1-based line numbers, for human-readable
/// reporting).
/// </summary>
public sealed record MarkdownResolution(
    MarkdownResolutionKind Kind,
    string? Content,
    string? Fingerprint,
    IReadOnlyList<int> AmbiguousHeadingLines)
{
    public static MarkdownResolution Resolved(string content) =>
        new(MarkdownResolutionKind.Resolved, content, MarkdownSourceResolver.ComputeFingerprint(content), []);

    public static MarkdownResolution SectionNotFound() =>
        new(MarkdownResolutionKind.SectionNotFound, null, null, []);

    public static MarkdownResolution SectionAmbiguous(IReadOnlyList<int> headingLines) =>
        new(MarkdownResolutionKind.SectionAmbiguous, null, null, headingLines);
}

/// <summary>
/// Resolves a <c>metadata.source.file</c>(+<c>section</c>) reference against already-read markdown
/// text and computes a stable fingerprint for drift detection - see
/// docs/done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md. Pure string logic, no disk I/O - the caller
/// (<see cref="RuleSourceChecker"/>) owns reading the file. Recognises ATX headings only
/// (<c>#</c>..<c>######</c>); Setext headings and fuzzy/case-insensitive matching are out of scope
/// for v1.
/// </summary>
public static partial class MarkdownSourceResolver
{
    // Source-generated at compile time (faster startup than RegexOptions.Compiled's runtime IL-emit)
    // with an explicit match timeout, satisfying Sonar's "regex could hang on pathological input" rule
    // even though this pattern (bounded #{1,6}, no nested/overlapping quantifiers) isn't itself prone
    // to catastrophic backtracking.
    [GeneratedRegex(@"^(#{1,6})\s+(.+?)\s*$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex AtxHeadingRegex();

    /// <param name="markdownContent">The full text of the linked markdown file.</param>
    /// <param name="heading">
    /// Exact (trimmed) heading text to scope the fingerprint to, or null to fingerprint the whole
    /// file.
    /// </param>
    public static MarkdownResolution Resolve(string markdownContent, string? heading)
    {
        var lines = Normalize(markdownContent).Split('\n');
        var headings = FindHeadings(lines);

        if (heading is null)
        {
            return MarkdownResolution.Resolved(ExtractSection(lines, 0, lines.Length));
        }

        var trimmedHeading = heading.Trim();
        var matches = headings.Where(h => h.Text == trimmedHeading).ToList();

        if (matches.Count == 0)
        {
            return MarkdownResolution.SectionNotFound();
        }

        if (matches.Count > 1)
        {
            return MarkdownResolution.SectionAmbiguous(matches.Select(h => h.LineIndex + 1).ToList());
        }

        var match = matches[0];
        var endLine = headings
            .Where(h => h.LineIndex > match.LineIndex && h.Level <= match.Level)
            .Select(h => h.LineIndex)
            .DefaultIfEmpty(lines.Length)
            .Min();

        return MarkdownResolution.Resolved(ExtractSection(lines, match.LineIndex + 1, endLine));
    }

    /// <summary>
    /// <c>sha256:&lt;64 hex&gt;</c> of already-normalized content, matching the only other hashing
    /// precedent in this repo (<see cref="CodeGuard.Configuration.GlobalConfig.GlobalSettingsPaths"/>).
    /// </summary>
    public static string ComputeFingerprint(string normalizedContent) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedContent)));

    private static List<(int Level, string Text, int LineIndex)> FindHeadings(string[] lines)
    {
        var headings = new List<(int Level, string Text, int LineIndex)>();
        for (var i = 0; i < lines.Length; i++)
        {
            var match = AtxHeadingRegex().Match(lines[i]);
            if (match.Success)
            {
                headings.Add((match.Groups[1].Length, match.Groups[2].Value.Trim(), i));
            }
        }

        return headings;
    }

    /// <summary>
    /// Trims trailing whitespace per line and leading/trailing blank lines from <c>[start, end)</c> -
    /// keeps the fingerprint resilient to incidental whitespace/line-ending noise while still catching
    /// real content changes.
    /// </summary>
    private static string ExtractSection(string[] lines, int start, int end)
    {
        var trimmed = lines[start..end].Select(l => l.TrimEnd()).ToArray();

        var firstNonBlank = Array.FindIndex(trimmed, l => l.Length > 0);
        if (firstNonBlank < 0)
        {
            return string.Empty;
        }

        var lastNonBlank = Array.FindLastIndex(trimmed, l => l.Length > 0);
        return string.Join('\n', trimmed[firstNonBlank..(lastNonBlank + 1)]);
    }

    private static string Normalize(string content) => content.Replace("\r\n", "\n").Replace("\r", "\n");
}
