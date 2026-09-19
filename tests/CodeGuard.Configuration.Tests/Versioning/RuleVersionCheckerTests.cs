using CodeGuard.Configuration.Analysis;
using CodeGuard.Configuration.Loading;
using CodeGuard.Configuration.Versioning;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Configuration.Tests.Versioning;

public sealed class RuleVersionCheckerTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("codeguard-versionchecker-").FullName;

    [Fact]
    public void Check_RuleNotTracked_IsSkipped()
    {
        var file = WriteRuleFile("not-tracked.yml", RuleYaml("DDD-ENTITY-001"));
        var rule = RuleWith("DDD-ENTITY-001", metadata: null);

        var report = RuleVersionChecker.Check([(rule, file)]);

        Assert.Empty(report.Issues);
        Assert.True(report.IsValid);
    }

    [Fact]
    public void Check_TrackedWithNoFingerprintYet_ReportsFingerprintMissing()
    {
        var file = WriteRuleFile("no-fingerprint.yml", RuleYaml("DDD-ENTITY-001"));
        var rule = RuleWith("DDD-ENTITY-001", new RuleMetadata { TrackVersion = true });

        var report = RuleVersionChecker.Check([(rule, file)]);

        var issue = Assert.Single(report.Issues);
        Assert.Equal(RuleVersionIssueKind.FingerprintMissing, issue.Kind);
        Assert.Null(issue.RecordedFingerprint);
        Assert.Matches("^sha256:[0-9a-f]{64}$", issue.ComputedFingerprint);
        Assert.False(report.IsValid);
    }

    [Fact]
    public void Check_TrackedWithMatchingFingerprint_ReportsNoIssue()
    {
        var file = WriteRuleFile("matching.yml", RuleYaml("DDD-ENTITY-001"));
        var fingerprint = RuleBodyCanonicalizer.ComputeFingerprint(RuleFileLoader.ReadDocument(file).AsObject());
        var rule = RuleWith("DDD-ENTITY-001", new RuleMetadata { TrackVersion = true, VersionFingerprint = fingerprint });

        var report = RuleVersionChecker.Check([(rule, file)]);

        Assert.Empty(report.Issues);
        Assert.True(report.IsValid);
    }

    [Fact]
    public void Check_TrackedWithMismatchedFingerprint_ReportsContentChanged()
    {
        var file = WriteRuleFile("mismatched.yml", RuleYaml("DDD-ENTITY-001"));
        var staleFingerprint = "sha256:" + new string('0', 64);
        var rule = RuleWith("DDD-ENTITY-001", new RuleMetadata { TrackVersion = true, VersionFingerprint = staleFingerprint });

        var report = RuleVersionChecker.Check([(rule, file)]);

        var issue = Assert.Single(report.Issues);
        Assert.Equal(RuleVersionIssueKind.ContentChanged, issue.Kind);
        Assert.Equal(staleFingerprint, issue.RecordedFingerprint);
        Assert.NotEqual(staleFingerprint, issue.ComputedFingerprint);
        Assert.False(report.IsValid);
    }

    [Fact]
    public void Check_MultipleRules_OnlyChecksTrackedOnes()
    {
        var trackedFile = WriteRuleFile("tracked.yml", RuleYaml("RULE-1"));
        var untrackedFile = WriteRuleFile("untracked.yml", RuleYaml("RULE-2"));

        var tracked = RuleWith("RULE-1", new RuleMetadata { TrackVersion = true });
        var untracked = RuleWith("RULE-2", metadata: null);

        var report = RuleVersionChecker.Check([(tracked, trackedFile), (untracked, untrackedFile)]);

        var issue = Assert.Single(report.Issues);
        Assert.Equal("RULE-1", issue.RuleId);
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

    private static RuleDefinition RuleWith(string id, RuleMetadata? metadata) => new()
    {
        Id = id,
        Name = "Some rule",
        Metadata = metadata
    };

    private string WriteRuleFile(string relativePath, string yaml)
    {
        var path = Path.Combine(_directory, relativePath);
        File.WriteAllText(path, yaml);
        return path;
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
