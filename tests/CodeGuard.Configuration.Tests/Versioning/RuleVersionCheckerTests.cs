using CodeGuard.Configuration.Analysis;
using CodeGuard.Configuration.Loading;
using CodeGuard.Configuration.Versioning;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Configuration.Tests.Versioning;

public sealed class RuleVersionCheckerTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("codeguard-versionchecker-").FullName;

    [Fact]
    public void Check_RuleWithNoFingerprintYet_ReportsFingerprintMissing()
    {
        var file = WriteRuleFile("no-fingerprint.yml", RuleYaml("DDD-ENTITY-001"));
        var rule = RuleWith("DDD-ENTITY-001", versionFingerprint: null);

        var report = RuleVersionChecker.Check([(rule, file)]);

        var issue = Assert.Single(report.Issues);
        Assert.Equal(RuleVersionIssueKind.FingerprintMissing, issue.Kind);
        Assert.Null(issue.RecordedFingerprint);
        Assert.Matches("^sha256:[0-9a-f]{64}$", issue.ComputedFingerprint);
        Assert.False(report.IsValid);
    }

    [Fact]
    public void Check_MatchingFingerprint_ReportsNoIssue()
    {
        var file = WriteRuleFile("matching.yml", RuleYaml("DDD-ENTITY-001"));
        var fingerprint = RuleBodyCanonicalizer.ComputeFingerprint(RuleFileLoader.ReadDocument(file).AsObject());
        var rule = RuleWith("DDD-ENTITY-001", fingerprint);

        var report = RuleVersionChecker.Check([(rule, file)]);

        Assert.Empty(report.Issues);
        Assert.True(report.IsValid);
    }

    [Fact]
    public void Check_MismatchedFingerprint_ReportsContentChanged()
    {
        var file = WriteRuleFile("mismatched.yml", RuleYaml("DDD-ENTITY-001"));
        var staleFingerprint = "sha256:" + new string('0', 64);
        var rule = RuleWith("DDD-ENTITY-001", staleFingerprint);

        var report = RuleVersionChecker.Check([(rule, file)]);

        var issue = Assert.Single(report.Issues);
        Assert.Equal(RuleVersionIssueKind.ContentChanged, issue.Kind);
        Assert.Equal(staleFingerprint, issue.RecordedFingerprint);
        Assert.NotEqual(staleFingerprint, issue.ComputedFingerprint);
        Assert.False(report.IsValid);
    }

    [Fact]
    public void Check_MultipleRules_ChecksEveryOneUnconditionally()
    {
        var fileA = WriteRuleFile("a.yml", RuleYaml("RULE-1"));
        var fileB = WriteRuleFile("b.yml", RuleYaml("RULE-2"));

        var fingerprintA = RuleBodyCanonicalizer.ComputeFingerprint(RuleFileLoader.ReadDocument(fileA).AsObject());
        var ruleA = RuleWith("RULE-1", fingerprintA);
        var ruleB = RuleWith("RULE-2", versionFingerprint: null);

        var report = RuleVersionChecker.Check([(ruleA, fileA), (ruleB, fileB)]);

        var issue = Assert.Single(report.Issues);
        Assert.Equal("RULE-2", issue.RuleId);
        Assert.Equal(RuleVersionIssueKind.FingerprintMissing, issue.Kind);
    }

    private static string RuleYaml(string id) => $"""
        id: {id}
        name: Some rule
        target:
          kind: class
          namespace: "Contoso.Domain.Entities"
        assertions:
          - must_inherit_from:
              type: "Contoso.Domain.Entity<TId>"
        """;

    private static RuleDefinition RuleWith(string id, string? versionFingerprint) => new()
    {
        Id = id,
        Name = "Some rule",
        VersionFingerprint = versionFingerprint
    };

    private string WriteRuleFile(string relativePath, string yaml)
    {
        var path = Path.Combine(_directory, relativePath);
        File.WriteAllText(path, yaml);
        return path;
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
