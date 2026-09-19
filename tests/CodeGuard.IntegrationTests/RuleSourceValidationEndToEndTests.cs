using System.Runtime.CompilerServices;
using CodeGuard.Cli.Commands.Rules;

namespace CodeGuard.IntegrationTests;

/// <summary>
/// End-to-end coverage for `rules validate`'s metadata.source.file drift checking
/// (docs/done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md), run against real, committed fixture files
/// rather than synthetic temp directories - complements the temp-directory-based coverage in
/// CodeGuard.Configuration.Tests/Sources and CodeGuard.Cli.Tests/Rules/ValidateCommandTests.
///
/// Fixtures/SourceLinkedRules/ has three rules against one committed markdown doc: SRC-CLEAN-001
/// carries a fingerprint that matches the doc's current content exactly (captured via
/// `--update-fingerprints` when the fixture was authored - see the doc's own header comment for how
/// to regenerate it if the fixture markdown ever needs to change), SRC-DRIFTED-001 links the same
/// doc/section with a deliberately wrong fingerprint, and SRC-BROKEN-001 links a file that doesn't
/// exist. All three are otherwise structurally valid rules, so every finding here is a source-check
/// warning, never a schema/structural failure.
/// </summary>
public class RuleSourceValidationEndToEndTests
{
    [Fact]
    public async Task RulesValidate_AgainstFixtureRepo_ExitsZeroWithNoSourceWarningsAffectingTheBuild()
    {
        var (exitCode, _) = await RunValidate(GetFixtureRoot());

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RulesValidate_AgainstFixtureRepo_CleanRuleProducesNoSourceCheckOutputAtAll()
    {
        var (_, output) = await RunValidate(GetFixtureRoot());

        Assert.DoesNotContain("SRC-CLEAN-001", SourceChecksSection(output));
    }

    [Fact]
    public async Task RulesValidate_AgainstFixtureRepo_DriftedFingerprintWarnsWithRecordedAndCurrentContent()
    {
        var (_, output) = await RunValidate(GetFixtureRoot());
        var section = SourceChecksSection(output);

        Assert.Contains("⚠ SRC-DRIFTED-001 - source content changed", section);
        Assert.Contains("Recorded statement: A statement recorded before the doc changed underneath it.", section);
        Assert.Contains("Current content:    Domain projects must not reference Infrastructure.", section);
    }

    [Fact]
    public async Task RulesValidate_AgainstFixtureRepo_MissingSourceFileWarns()
    {
        var (_, output) = await RunValidate(GetFixtureRoot());
        var section = SourceChecksSection(output);

        Assert.Contains("✗ SRC-BROKEN-001 - source document no longer exists (docs/does-not-exist.md)", section);
    }

    [Fact]
    public async Task RulesExplain_AgainstFixtureRepo_SurfacesFileAndFingerprint()
    {
        var rulesDir = Path.Combine(GetFixtureRoot(), "rules");
        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            var exitCode = await ExplainCommand.Build()
                .Parse(["--rules-source", rulesDir, "SRC-CLEAN-001", "--format", "json"])
                .InvokeAsync();

            Assert.Equal(0, exitCode);
            Assert.Contains("\"file\": \"docs/architecture.md\"", writer.ToString());
            Assert.Contains(
                "\"fingerprint\": \"sha256:790a2de809da2018ffd6337a4d1764a4eb3c33e32231133658b1fe014c83b614\"",
                writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task RulesValidate_UpdateFingerprints_RewritesADriftedCopyWithoutTouchingTheCommittedFixture()
    {
        var tempRoot = Directory.CreateTempSubdirectory("codeguard-integration-sourcelink-").FullName;
        try
        {
            var fixtureRoot = GetFixtureRoot();
            Directory.CreateDirectory(Path.Combine(tempRoot, "docs"));
            Directory.CreateDirectory(Path.Combine(tempRoot, "rules"));
            File.Copy(
                Path.Combine(fixtureRoot, "docs", "architecture.md"),
                Path.Combine(tempRoot, "docs", "architecture.md"));
            File.Copy(
                Path.Combine(fixtureRoot, "rules", "drifted.yml"),
                Path.Combine(tempRoot, "rules", "drifted.yml"));
            File.Copy(
                Path.Combine(fixtureRoot, "rules", "broken.yml"),
                Path.Combine(tempRoot, "rules", "broken.yml"));

            var (exitCode, output) = await RunValidate(tempRoot, "--update-fingerprints");

            Assert.Equal(0, exitCode);
            Assert.Contains("Updated source fingerprint: SRC-DRIFTED-001", output);
            Assert.DoesNotContain("Updated source fingerprint: SRC-BROKEN-001", output); // nothing to fingerprint

            var updatedDrifted = await File.ReadAllTextAsync(Path.Combine(tempRoot, "rules", "drifted.yml"));
            Assert.Contains(
                "fingerprint: sha256:790a2de809da2018ffd6337a4d1764a4eb3c33e32231133658b1fe014c83b614",
                updatedDrifted);
            Assert.Contains("Domain must not reference Infrastructure (drifted fixture copy)", updatedDrifted); // rest of file preserved

            // The committed fixture that --update-fingerprints ran against a *copy* of must be untouched.
            var committedDrifted = await File.ReadAllTextAsync(Path.Combine(fixtureRoot, "rules", "drifted.yml"));
            Assert.Contains("sha256:0000000000000000000000000000000000000000000000000000000000000000", committedDrifted);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>
    /// Bounds the slice to the next section header ("Version checks:"/"Rule analysis:") rather than
    /// running to the end of the output - `rules validate` prints further sections after "Source
    /// checks:" now, so an unbounded slice would incidentally pick up rule ids mentioned there too.
    /// </summary>
    private static string SourceChecksSection(string output)
    {
        if (!output.Contains("Source checks:", StringComparison.Ordinal))
        {
            return "";
        }

        var start = output.IndexOf("Source checks:", StringComparison.Ordinal);
        var end = output.Length;
        foreach (var marker in new[] { "Version checks:", "Rule analysis:" })
        {
            var markerIndex = output.IndexOf(marker, start, StringComparison.Ordinal);
            if (markerIndex >= 0 && markerIndex < end)
            {
                end = markerIndex;
            }
        }

        return output[start..end];
    }

    private static async Task<(int ExitCode, string Output)> RunValidate(string repoRoot, params string[] extraArgs)
    {
        var args = new List<string> { "--rules-source", Path.Combine(repoRoot, "rules"), "--path", repoRoot };
        args.AddRange(extraArgs);

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

    private static string GetFixtureRoot([CallerFilePath] string sourceFilePath = "") =>
        Path.Combine(Path.GetDirectoryName(sourceFilePath)!, "Fixtures", "SourceLinkedRules");
}
