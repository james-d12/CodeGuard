using CodeGuard.Configuration.Sources;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Configuration.Tests.Sources;

public sealed class RuleSourceCheckerTests : IDisposable
{
    private readonly string _repoRoot = Directory.CreateTempSubdirectory("codeguard-rulesourcechecker-").FullName;

    [Fact]
    public void Check_RuleWithNoSourceFile_IsSkipped()
    {
        var rule = RuleWithSource(source: null);

        var report = RuleSourceChecker.Check([(rule, "rule.yml")], _repoRoot);

        Assert.Empty(report.Issues);
    }

    [Fact]
    public void Check_MatchingFingerprint_ReportsNoIssue()
    {
        WriteMarkdown("docs/architecture.md", "## Domain Layer\n\nDomain projects must not reference Infrastructure.\n");
        var resolution = MarkdownSourceResolver.Resolve(
            File.ReadAllText(Path.Combine(_repoRoot, "docs/architecture.md")), "Domain Layer");

        var rule = RuleWithSource(new RuleSource(
            "Architecture Standards", "Domain Layer", "statement", "docs/architecture.md", resolution.Fingerprint));

        var report = RuleSourceChecker.Check([(rule, "rule.yml")], _repoRoot);

        Assert.Empty(report.Issues);
    }

    [Fact]
    public void Check_MissingFile_ReportsFileMissing()
    {
        var rule = RuleWithSource(new RuleSource("Standards", null, null, "docs/missing.md"));

        var report = RuleSourceChecker.Check([(rule, "rule.yml")], _repoRoot);

        var issue = Assert.Single(report.Issues);
        Assert.Equal(RuleSourceIssueKind.FileMissing, issue.Kind);
        Assert.Equal("docs/missing.md", issue.File);
    }

    [Fact]
    public void Check_SectionNotFound_ReportsSectionNotFound()
    {
        WriteMarkdown("docs/architecture.md", "## Something Else\n\nContent.\n");
        var rule = RuleWithSource(new RuleSource("Standards", "Domain Layer", null, "docs/architecture.md"));

        var report = RuleSourceChecker.Check([(rule, "rule.yml")], _repoRoot);

        var issue = Assert.Single(report.Issues);
        Assert.Equal(RuleSourceIssueKind.SectionNotFound, issue.Kind);
    }

    [Fact]
    public void Check_AmbiguousSection_ReportsSectionAmbiguous()
    {
        WriteMarkdown("docs/architecture.md", "## Domain Layer\n\nFirst.\n\n## Domain Layer\n\nSecond.\n");
        var rule = RuleWithSource(new RuleSource("Standards", "Domain Layer", null, "docs/architecture.md"));

        var report = RuleSourceChecker.Check([(rule, "rule.yml")], _repoRoot);

        var issue = Assert.Single(report.Issues);
        Assert.Equal(RuleSourceIssueKind.SectionAmbiguous, issue.Kind);
        Assert.Equal(2, issue.AmbiguousHeadingLines.Count);
    }

    [Fact]
    public void Check_NoFingerprintCaptured_ReportsFingerprintMissing()
    {
        WriteMarkdown("docs/architecture.md", "## Domain Layer\n\nContent.\n");
        var rule = RuleWithSource(new RuleSource("Standards", "Domain Layer", null, "docs/architecture.md"));

        var report = RuleSourceChecker.Check([(rule, "rule.yml")], _repoRoot);

        var issue = Assert.Single(report.Issues);
        Assert.Equal(RuleSourceIssueKind.FingerprintMissing, issue.Kind);
        Assert.NotNull(issue.ComputedFingerprint);
        Assert.Null(issue.PreviousFingerprint);
    }

    [Fact]
    public void Check_FingerprintMismatch_ReportsContentChangedWithPreviousAndComputedValues()
    {
        WriteMarkdown("docs/architecture.md", "## Domain Layer\n\nUpdated content.\n");
        var rule = RuleWithSource(new RuleSource(
            "Standards", "Domain Layer", "Recorded paraphrase.", "docs/architecture.md", "sha256:" + new string('0', 64)));

        var report = RuleSourceChecker.Check([(rule, "rule.yml")], _repoRoot);

        var issue = Assert.Single(report.Issues);
        Assert.Equal(RuleSourceIssueKind.ContentChanged, issue.Kind);
        Assert.Equal("sha256:" + new string('0', 64), issue.PreviousFingerprint);
        Assert.NotEqual(issue.PreviousFingerprint, issue.ComputedFingerprint);
        Assert.Equal("Recorded paraphrase.", issue.RecordedStatement);
        Assert.Contains("Updated content.", issue.CurrentContent);
    }

    [Fact]
    public void Check_FileWithNoSectionSet_FingerprintsWholeFile()
    {
        WriteMarkdown("docs/standalone.md", "Whole file content.\n");
        var resolution = MarkdownSourceResolver.Resolve(
            File.ReadAllText(Path.Combine(_repoRoot, "docs/standalone.md")), heading: null);

        var rule = RuleWithSource(new RuleSource("Standards", null, null, "docs/standalone.md", resolution.Fingerprint));

        var report = RuleSourceChecker.Check([(rule, "rule.yml")], _repoRoot);

        Assert.Empty(report.Issues);
    }

    [Fact]
    public void Check_MultipleRules_OnlyChecksOnesWithSourceFile()
    {
        WriteMarkdown("docs/architecture.md", "## Domain Layer\n\nContent.\n");
        var withFile = RuleWithSource(new RuleSource("Standards", "Domain Layer", null, "docs/architecture.md"), id: "RULE-1");
        var withoutFile = RuleWithSource(new RuleSource("Standards", "Domain Layer", "statement"), id: "RULE-2");
        var withNoSourceAtAll = RuleWithSource(source: null, id: "RULE-3");

        var report = RuleSourceChecker.Check(
            [(withFile, "a.yml"), (withoutFile, "b.yml"), (withNoSourceAtAll, "c.yml")], _repoRoot);

        var issue = Assert.Single(report.Issues);
        Assert.Equal("RULE-1", issue.RuleId);
    }

    private void WriteMarkdown(string relativePath, string content)
    {
        var fullPath = Path.Combine(_repoRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    private static RuleDefinition RuleWithSource(RuleSource? source, string id = "RULE-1") => new()
    {
        Id = id,
        Name = "Some rule",
        Metadata = source is null ? null : new RuleMetadata { Source = source }
    };

    public void Dispose() => Directory.Delete(_repoRoot, recursive: true);
}
