using CodeGuard.Cli.Commands.Rules;
using CodeGuard.Configuration.Analysis;
using CodeGuard.Configuration.Loading;
using CodeGuard.Configuration.Sources;

namespace CodeGuard.Cli.Tests.Rules;

/// <summary>Covers the `rules validate` command end-to-end via its System.CommandLine `Command`, and the
/// pre-flight gate `validate` shares with it (docs/done/RULE_VALIDATION_PLAN.md).</summary>
[Collection(ConsoleOutputCollection.Name)]
public sealed class ValidateCommandTests : IDisposable
{
    private readonly string _rulesDir = Directory.CreateTempSubdirectory("codeguard-rulesvalidate-").FullName;
    private readonly string _repoRoot = Directory.CreateTempSubdirectory("codeguard-rulesvalidate-repo-").FullName;

    [Fact]
    public async Task Run_AllRulesValid_ExitsZeroAndReportsAllPassed()
    {
        WriteRuleFile("a.yml", RuleYaml("DDD-ENTITY-001"));
        WriteRuleFile("b.yml", RuleYaml("DDD-ENTITY-002"));

        var (exitCode, output) = await RunValidateRules();

        Assert.Equal(0, exitCode);
        Assert.Contains("Checked 2 rule files: 2 passed, 0 failed.", output);
    }

    [Fact]
    public async Task Run_BadRuleFile_ExitsOneAndReportsError()
    {
        WriteRuleFile("bad.yml", """
            id: DDD-ENTITY-001
            name: Some rule
            target:
              kind: not_a_real_kind
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<TId>"
            """);

        var (exitCode, output) = await RunValidateRules();

        Assert.Equal(1, exitCode);
        Assert.Contains("Checked 1 rule file: 0 passed, 1 failed.", output);
        Assert.Contains("not_a_real_kind", output);
    }

    [Fact]
    public async Task Run_DuplicateRuleId_ExitsOneAndReportsBoth()
    {
        WriteRuleFile("a.yml", RuleYaml("DDD-ENTITY-001"));
        WriteRuleFile("b.yml", RuleYaml("DDD-ENTITY-001"));

        var (exitCode, output) = await RunValidateRules();

        Assert.Equal(1, exitCode);
        Assert.Contains("Duplicate rule id 'DDD-ENTITY-001'", output);
    }

    [Fact]
    public async Task Run_JsonFormat_ReportsIsValidFalse()
    {
        WriteRuleFile("bad.yml", """
            id: DDD-ENTITY-001
            name: Some rule
            target:
              kind: not_a_real_kind
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<TId>"
            """);

        var (exitCode, output) = await RunValidateRules(["--format", "json"]);

        Assert.Equal(1, exitCode);
        Assert.Contains("\"isValid\": false", output);
    }

    [Fact]
    public async Task Run_NoRulesConfigured_ExitsOneAndPrintsHint()
    {
        using var globalSettings = new IsolatedGlobalSettingsScope();
        var repoDir = Directory.CreateTempSubdirectory("codeguard-rulesvalidate-norules-repo-").FullName;
        try
        {
            var (exitCode, _, error) = await RunValidateRulesRaw(["--path", repoDir]);

            Assert.Equal(1, exitCode);
            Assert.Contains("No rules directory is configured.", error);
            Assert.Contains("codeguard setup", error);
            Assert.Contains("--rules-source", error);
        }
        finally
        {
            Directory.Delete(repoDir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_MalformedConfig_PrintsFriendlyErrorAndExitsOne()
    {
        var repoDir = Directory.CreateTempSubdirectory("codeguard-rulesvalidate-malformed-repo-").FullName;
        try
        {
            var configDir = Directory.CreateDirectory(Path.Combine(repoDir, ".codeguard"));
            var configPath = Path.Combine(configDir.FullName, "config.yml");
            await File.WriteAllTextAsync(configPath, "repository: [this, is, not, a, map]");

            var (exitCode, _, error) = await RunValidateRulesRaw(["--path", repoDir]);

            Assert.Equal(1, exitCode);
            Assert.Contains("codeguard:", error);
            Assert.Contains(configPath, error);
            Assert.DoesNotContain("Unhandled exception", error);
            Assert.DoesNotContain(" at ", error);
        }
        finally
        {
            Directory.Delete(repoDir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_SourceFileWithMatchingFingerprint_NoWarningAndExitsZero()
    {
        WriteMarkdown("docs/architecture.md", "## Domain Layer\n\nContent.\n");
        var resolution = MarkdownSourceResolver.Resolve(
            await File.ReadAllTextAsync(Path.Combine(_repoRoot, "docs/architecture.md")), "Domain Layer");
        WriteRuleFile("a.yml", RuleYamlWithSource("DDD-ENTITY-001", resolution.Fingerprint));

        var (exitCode, output) = await RunValidateRules(["--path", _repoRoot]);

        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("Source checks:", output);
    }

    [Fact]
    public async Task Run_SourceFileWithDriftedFingerprint_WarnsButStillExitsZero()
    {
        WriteMarkdown("docs/architecture.md", "## Domain Layer\n\nUpdated content.\n");
        WriteRuleFile("a.yml", RuleYamlWithSource("DDD-ENTITY-001", "sha256:" + new string('0', 64)));

        var (exitCode, output) = await RunValidateRules(["--path", _repoRoot]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Source checks:", output);
        Assert.Contains("source content changed", output);
    }

    [Fact]
    public async Task Run_SourceFileMissing_WarnsButStillExitsZero()
    {
        WriteRuleFile("a.yml", RuleYamlWithSource("DDD-ENTITY-001", fingerprint: null));

        var (exitCode, output) = await RunValidateRules(["--path", _repoRoot]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Source checks:", output);
        Assert.Contains("source document no longer exists", output);
    }

    [Fact]
    public async Task Run_SourceDriftAlongsideAStructuralError_StillExitsOneForTheStructuralError()
    {
        WriteMarkdown("docs/architecture.md", "## Domain Layer\n\nUpdated content.\n");
        WriteRuleFile("a.yml", RuleYamlWithSource("DDD-ENTITY-001", "sha256:" + new string('0', 64)));
        WriteRuleFile("bad.yml", """
            id: DDD-ENTITY-002
            name: Some rule
            target:
              kind: not_a_real_kind
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<TId>"
            """);

        var (exitCode, output) = await RunValidateRules(["--path", _repoRoot]);

        Assert.Equal(1, exitCode);
        Assert.Contains("Source checks:", output);
        Assert.Contains("not_a_real_kind", output);
    }

    [Fact]
    public async Task Run_UpdateFingerprints_RewritesOnlyDriftedOrMissingRulesAndLeavesBrokenLinksAlone()
    {
        WriteMarkdown("docs/architecture.md", "## Domain Layer\n\nUpdated content.\n");
        WriteRuleFile("drifted.yml", RuleYamlWithSource("DDD-ENTITY-001", "sha256:" + new string('0', 64)));
        WriteRuleFile("broken.yml", RuleYamlWithSource("DDD-ENTITY-002", fingerprint: null, file: "docs/missing.md"));

        var (exitCode, output) = await RunValidateRules(["--path", _repoRoot, "--update-fingerprints"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Updated source fingerprint: DDD-ENTITY-001", output);
        Assert.DoesNotContain("DDD-ENTITY-002", output.Split("Source checks:")[0]);

        var drifted = await File.ReadAllTextAsync(Path.Combine(_rulesDir, "drifted.yml"));
        Assert.DoesNotContain("sha256:" + new string('0', 64), drifted);
        Assert.Contains("Contoso.Domain.Entities", drifted); // sanity: still the same file, not clobbered

        var broken = await File.ReadAllTextAsync(Path.Combine(_rulesDir, "broken.yml"));
        Assert.Contains("docs/missing.md", broken);
        Assert.DoesNotContain("fingerprint", broken);
    }

    [Fact]
    public async Task Run_TrackedVersionMatchingFingerprint_NoVersionChecksSectionAndExitsZero()
    {
        var path = Path.Combine(_rulesDir, "a.yml");
        WriteRuleFile("a.yml", RuleYamlWithVersionTracking("DDD-ENTITY-001", versionFingerprint: null));
        var fingerprint = RuleBodyCanonicalizer.ComputeFingerprint(RuleFileLoader.ReadDocument(path).AsObject());
        WriteRuleFile("a.yml", RuleYamlWithVersionTracking("DDD-ENTITY-001", fingerprint));

        var (exitCode, output) = await RunValidateRules();

        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("Version checks:", output);
    }

    [Fact]
    public async Task Run_TrackedVersionDrifted_FailsWithVersionChecksSectionAndExitsOne()
    {
        WriteRuleFile("a.yml", RuleYamlWithVersionTracking("DDD-ENTITY-001", "sha256:" + new string('0', 64)));

        var (exitCode, output) = await RunValidateRules();

        Assert.Equal(1, exitCode);
        Assert.Contains("Version checks:", output);
        Assert.Contains("enforceable body changed since its recorded versionFingerprint", output);
    }

    [Fact]
    public async Task Run_TrackedVersionFingerprintNeverCaptured_FailsWithVersionChecksSectionAndExitsOne()
    {
        WriteRuleFile("a.yml", RuleYamlWithVersionTracking("DDD-ENTITY-001", versionFingerprint: null));

        var (exitCode, output) = await RunValidateRules();

        Assert.Equal(1, exitCode);
        Assert.Contains("Version checks:", output);
        Assert.Contains("tracked but no versionFingerprint captured yet", output);
    }

    [Fact]
    public async Task Run_JsonFormat_VersionDrift_ReportsIsValidFalseWithVersionChecksEntry()
    {
        WriteRuleFile("a.yml", RuleYamlWithVersionTracking("DDD-ENTITY-001", "sha256:" + new string('0', 64)));

        var (exitCode, output) = await RunValidateRules(["--format", "json"]);

        Assert.Equal(1, exitCode);
        Assert.Contains("\"isValid\": false", output);
        Assert.Contains("\"versionChecks\"", output);
        Assert.Contains("\"contentChanged\"", output);
    }

    [Fact]
    public async Task Run_UpdateFingerprints_RewritesVersionFingerprint_ForBothMissingAndDrifted()
    {
        WriteRuleFile("missing.yml", RuleYamlWithVersionTracking("DDD-ENTITY-001", versionFingerprint: null));
        WriteRuleFile("drifted.yml", RuleYamlWithVersionTracking("DDD-ENTITY-002", "sha256:" + new string('0', 64)));

        var (exitCode, output) = await RunValidateRules(["--update-fingerprints"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Updated version fingerprint: DDD-ENTITY-001 (was missing, now sha256:", output);
        Assert.Contains($"Updated version fingerprint: DDD-ENTITY-002 (was sha256:{new string('0', 64)}, now sha256:", output);
        Assert.DoesNotContain("Version checks:", output);

        var missing = await File.ReadAllTextAsync(Path.Combine(_rulesDir, "missing.yml"));
        Assert.Contains("versionFingerprint: sha256:", missing);

        var drifted = await File.ReadAllTextAsync(Path.Combine(_rulesDir, "drifted.yml"));
        Assert.DoesNotContain("sha256:" + new string('0', 64), drifted);
    }

    [Fact]
    public async Task Run_SourceFingerprintNeverCaptured_UpdateFingerprints_RewritesWithWasMissingMessage()
    {
        WriteMarkdown("docs/architecture.md", "## Domain Layer\n\nContent.\n");
        WriteRuleFile("a.yml", RuleYamlWithSource("DDD-ENTITY-001", fingerprint: null));

        var (exitCode, output) = await RunValidateRules(["--path", _repoRoot, "--update-fingerprints"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Updated source fingerprint: DDD-ENTITY-001 (was missing, now sha256:", output);
    }

    private static string RuleYamlWithVersionTracking(string id, string? versionFingerprint)
    {
        var lines = new List<string>
        {
            $"id: {id}",
            "name: Some rule",
            "metadata:",
            "  trackVersion: true"
        };

        if (versionFingerprint is not null)
        {
            lines.Add($"  versionFingerprint: \"{versionFingerprint}\"");
        }

        lines.AddRange([
            "target:",
            "  kind: class",
            "  namespace: \"Contoso.Domain.Entities\"",
            "assertions:",
            "  - must_inherit_from:",
            "      type: \"Contoso.Domain.Entity<TId>\""
        ]);

        return string.Join('\n', lines);
    }

    private void WriteMarkdown(string relativePath, string content)
    {
        var fullPath = Path.Combine(_repoRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    private static string RuleYamlWithSource(string id, string? fingerprint, string file = "docs/architecture.md")
    {
        var lines = new List<string>
        {
            $"id: {id}",
            "name: Some rule",
            "metadata:",
            "  source:",
            "    document: Architecture Standards",
            "    section: Domain Layer",
            $"    file: {file}"
        };

        if (fingerprint is not null)
        {
            lines.Add($"    fingerprint: \"{fingerprint}\"");
        }

        lines.AddRange([
            "target:",
            "  kind: class",
            "  namespace: \"Contoso.Domain.Entities\"",
            "assertions:",
            "  - must_inherit_from:",
            "      type: \"Contoso.Domain.Entity<TId>\""
        ]);

        return string.Join('\n', lines);
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunValidateRulesRaw(IReadOnlyList<string> args)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var outWriter = new StringWriter();
        var errorWriter = new StringWriter();
        Console.SetOut(outWriter);
        Console.SetError(errorWriter);
        try
        {
            var exitCode = await ValidateCommand.Build().Parse(args.ToArray()).InvokeAsync();
            return (exitCode, outWriter.ToString(), errorWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private async Task<(int ExitCode, string Output)> RunValidateRules(IReadOnlyList<string>? extraArgs = null)
    {
        var args = new List<string> { "--rules-source", _rulesDir };
        if (extraArgs is not null)
        {
            args.AddRange(extraArgs);
        }

        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            var exitCode = await ValidateCommand.Build().Parse(args.ToArray()).InvokeAsync();
            return (exitCode, writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
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

    private void WriteRuleFile(string relativePath, string yaml) =>
        File.WriteAllText(Path.Combine(_rulesDir, relativePath), yaml);

    public void Dispose()
    {
        Directory.Delete(_rulesDir, recursive: true);
        Directory.Delete(_repoRoot, recursive: true);
    }
}
