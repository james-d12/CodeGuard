using CodeGuard.Cli.Commands;

namespace CodeGuard.Cli.Tests;

/// <summary>Covers `codeguard info` end-to-end via its System.CommandLine `Command`.</summary>
[Collection(ConsoleOutputCollection.Name)]
public class InfoCommandTests : IDisposable
{
    private readonly string _rulesDir = Directory.CreateTempSubdirectory("codeguard-info-rules-").FullName;

    [Fact]
    public async Task Run_CliOverride_ReportsCliOverrideProvenance()
    {
        WriteRuleFile("ddd", "a.yml", RuleYaml("DDD-ENTITY-001", severity: "error"));

        var (exitCode, output) = await RunInfo(["--rules-source", _rulesDir]);

        Assert.Equal(0, exitCode);
        Assert.Contains("--rules-source override", output);
        Assert.Contains(_rulesDir, output);
        Assert.Contains("Total:    1", output);
    }

    [Fact]
    public async Task Run_RepositoryConfig_ReportsRepositoryConfigProvenance()
    {
        using var globalSettings = new IsolatedGlobalSettingsScope();
        var repoDir = Directory.CreateTempSubdirectory("codeguard-info-repo-").FullName;
        try
        {
            WriteRuleFile("ddd", "a.yml", RuleYaml("DDD-ENTITY-001", severity: "error"));
            var configDir = Directory.CreateDirectory(Path.Combine(repoDir, ".codeguard"));
            File.WriteAllText(Path.Combine(configDir.FullName, "config.yml"), $"""
                repository:
                  rules:
                    - "{_rulesDir.Replace("\\", "/")}"
                """);

            var (exitCode, output) = await RunInfo(["--path", repoDir]);

            Assert.Equal(0, exitCode);
            Assert.Contains("repository config", output);
            Assert.Contains(Path.Combine(configDir.FullName, "config.yml"), output);
        }
        finally
        {
            Directory.Delete(repoDir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_NothingConfigured_ReportsDefaultProvenanceAndZeroRules()
    {
        using var globalSettings = new IsolatedGlobalSettingsScope();
        var repoDir = Directory.CreateTempSubdirectory("codeguard-info-empty-repo-").FullName;
        try
        {
            var (exitCode, output) = await RunInfo(["--path", repoDir]);

            Assert.Equal(0, exitCode);
            Assert.Contains("default", output);
            Assert.Contains("Total:    0", output);
        }
        finally
        {
            Directory.Delete(repoDir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_MultipleRulesAcrossStandards_ReportsCountsBrokenDown()
    {
        WriteRuleFile("ddd", "a.yml", RuleYaml("DDD-ENTITY-001", severity: "error"));
        WriteRuleFile("ddd", "b.yml", RuleYaml("DDD-ENTITY-002", severity: "warning"));
        WriteRuleFile("csharp", "c.yml", RuleYaml("CS-NAMING-001", severity: "error", enabled: false));

        var (exitCode, output) = await RunInfo(["--rules-source", _rulesDir]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Total:    3", output);
        Assert.Contains("Enabled:  2", output);
        Assert.Contains("Disabled: 1", output);
        Assert.Contains("ddd", output);
        Assert.Contains("csharp", output);
    }

    [Fact]
    public async Task Run_InvalidRuleFilePresent_DoesNotThrowAndReportsInvalidCount()
    {
        WriteRuleFile("ddd", "good.yml", RuleYaml("DDD-ENTITY-001", severity: "error"));
        WriteRuleFile("ddd", "bad.yml", """
            id: DDD-ENTITY-002
            name: Some rule
            target:
              kind: not_a_real_kind
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<TId>"
            """);

        var (exitCode, output) = await RunInfo(["--rules-source", _rulesDir]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Total:    1", output);
        Assert.Contains("rule file(s) failed to parse", output);
    }

    [Fact]
    public async Task Run_JsonFormat_ProducesValidJsonWithExpectedKeys()
    {
        WriteRuleFile("ddd", "a.yml", RuleYaml("DDD-ENTITY-001", severity: "error"));

        var (exitCode, output) = await RunInfo(["--rules-source", _rulesDir, "--format", "json"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("\"repoRoot\"", output);
        Assert.Contains("\"rulesSource\"", output);
        Assert.Contains("\"ruleCounts\"", output);
        Assert.Contains("\"layout\"", output);
        Assert.Contains("\"total\": 1", output);
    }

    private async Task<(int ExitCode, string Output)> RunInfo(IReadOnlyList<string> args)
    {
        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            var exitCode = await InfoCommand.Build().Parse(args.ToArray()).InvokeAsync();
            return (exitCode, writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static string RuleYaml(string id, string severity, bool enabled = true) => $"""
        id: {id}
        name: Some rule
        severity: {severity}
        enabled: {(enabled ? "true" : "false")}
        target:
          kind: class
          namespace: "Contoso.Domain.Entities"
        assertions:
          - must_inherit_from:
              type: "Contoso.Domain.Entity<TId>"
        """;

    private void WriteRuleFile(string subdirectory, string fileName, string yaml)
    {
        var dir = Directory.CreateDirectory(Path.Combine(_rulesDir, subdirectory));
        File.WriteAllText(Path.Combine(dir.FullName, fileName), yaml);
    }

    public void Dispose() => Directory.Delete(_rulesDir, recursive: true);
}
