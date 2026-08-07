using CodeGuard.Cli.Commands.Rules;
using CodeGuard.Cli.Tests;

namespace CodeGuard.Cli.Tests.Rules;

/// <summary>Covers the `rules list` command end-to-end via its System.CommandLine `Command`: the
/// "no rules directory configured" guard, table/json output, and --tag/--enabled-only filtering.</summary>
[Collection(ConsoleOutputCollection.Name)]
public class ListCommandTests : IDisposable
{
    private readonly string _rulesDir = Directory.CreateTempSubdirectory("codeguard-list-rules-").FullName;

    [Fact]
    public async Task Run_NoRulesConfigured_ExitsOneAndPrintsHint()
    {
        using var globalSettings = new IsolatedGlobalSettingsScope();
        var repoDir = Directory.CreateTempSubdirectory("codeguard-list-norules-repo-").FullName;
        try
        {
            var (exitCode, _, error) = await RunListRulesRaw(["--path", repoDir]);

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
    public async Task Run_TableFormat_PrintsHeaderSeparatorAndAlignedRows()
    {
        WriteRuleFile("short.yml", RuleYaml("A-1", severity: "info", tags: ["x"]));
        WriteRuleFile(
            "long.yml",
            RuleYaml(
                "DDD-VERY-LONG-RULE-ID-EXAMPLE-001",
                severity: "critical",
                classification: "not_currently_enforceable",
                enabled: false,
                tags: ["alpha", "beta"]));

        var (exitCode, output) = await RunListRules();

        Assert.Equal(0, exitCode);
        var lines = SplitLines(output);
        Assert.Equal(4, lines.Count); // header, separator, then the two rules sorted by id.

        var headerLine = lines[0];
        var separatorLine = lines[1];
        Assert.Equal(headerLine.Length, separatorLine.Length);
        Assert.True(separatorLine.Length > 0 && separatorLine.All(c => c == '-'));

        var severityColumn = headerLine.IndexOf("SEVERITY", StringComparison.Ordinal);
        var enforcementColumn = headerLine.IndexOf("ENFORCEMENT", StringComparison.Ordinal);
        var enabledColumn = headerLine.IndexOf("ENABLED", StringComparison.Ordinal);
        var tagsColumn = headerLine.IndexOf("TAGS", StringComparison.Ordinal);
        Assert.True(severityColumn > "ID".Length);
        Assert.True(enforcementColumn > severityColumn);
        Assert.True(enabledColumn > enforcementColumn);
        Assert.True(tagsColumn > enabledColumn);

        // Rows are sorted by id ("A-1" < "DDD-...", ordinal), so row order is deterministic.
        var shortRow = lines[2];
        Assert.StartsWith("A-1", shortRow, StringComparison.Ordinal);
        Assert.Equal("Info", shortRow.Substring(severityColumn, "Info".Length));
        Assert.Equal("True", shortRow.Substring(enabledColumn, "True".Length));
        Assert.EndsWith("x", shortRow, StringComparison.Ordinal);

        var longRow = lines[3];
        Assert.StartsWith("DDD-VERY-LONG-RULE-ID-EXAMPLE-001", longRow, StringComparison.Ordinal);
        Assert.Equal("Critical", longRow.Substring(severityColumn, "Critical".Length));
        Assert.Equal("NotCurrentlyEnforceable", longRow.Substring(enforcementColumn, "NotCurrentlyEnforceable".Length));
        Assert.Equal("False", longRow.Substring(enabledColumn, "False".Length));
        Assert.EndsWith("alpha,beta", longRow, StringComparison.Ordinal);

        // The wide id/enforcement values must widen their columns rather than get truncated or
        // overlap the next column's header.
        Assert.True(severityColumn > "DDD-VERY-LONG-RULE-ID-EXAMPLE-001".Length);
        Assert.True(enabledColumn - enforcementColumn > "NotCurrentlyEnforceable".Length);
    }

    [Fact]
    public async Task Run_JsonFormat_ReturnsRuleSummaries()
    {
        WriteRuleFile("a.yml", RuleYaml("DDD-ENTITY-001", severity: "error", tags: ["ddd", "entities"]));

        var (exitCode, output) = await RunListRules(["--format", "json"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("\"id\": \"DDD-ENTITY-001\"", output);
        Assert.Contains("\"severity\": \"error\"", output);
        Assert.Contains("\"ddd\"", output);
        Assert.Contains("\"entities\"", output);
    }

    [Fact]
    public async Task Run_TagFilter_OnlyIncludesRulesWithMatchingTag()
    {
        WriteRuleFile("a.yml", RuleYaml("DDD-ENTITY-001", tags: ["ddd"]));
        WriteRuleFile("b.yml", RuleYaml("ARCH-LAYER-001", tags: ["architecture"]));

        var (exitCode, output) = await RunListRules(["--tag", "ddd"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("DDD-ENTITY-001", output);
        Assert.DoesNotContain("ARCH-LAYER-001", output);
    }

    [Fact]
    public async Task Run_EnabledOnlyFlag_ExcludesDisabledRules()
    {
        WriteRuleFile("a.yml", RuleYaml("DDD-ENTITY-001", enabled: true));
        WriteRuleFile("b.yml", RuleYaml("DDD-ENTITY-002", enabled: false));

        var (exitCode, output) = await RunListRules(["--enabled-only"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("DDD-ENTITY-001", output);
        Assert.DoesNotContain("DDD-ENTITY-002", output);
    }

    [Fact]
    public async Task Run_FilterMatchesNoRules_PrintsNoRulesFound()
    {
        WriteRuleFile("a.yml", RuleYaml("DDD-ENTITY-001", tags: ["ddd"]));

        var (exitCode, output) = await RunListRules(["--tag", "nonexistent"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("No rules found.", output);
    }

    private static string RuleYaml(
        string id,
        string? severity = null,
        string? classification = null,
        IReadOnlyList<string>? tags = null,
        bool enabled = true)
    {
        var tagsYaml = tags is { Count: > 0 }
            ? "tags: [" + string.Join(", ", tags) + "]"
            : "";
        var severityYaml = severity is null ? "" : $"severity: {severity}";
        var enforcementYaml = classification is null ? "" : $"enforcement:\n  classification: {classification}";

        return $"""
            id: {id}
            name: Some rule
            {severityYaml}
            {enforcementYaml}
            enabled: {(enabled ? "true" : "false")}
            {tagsYaml}
            target:
              kind: class
              namespace: "Contoso.Domain.Entities"
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<TId>"
            """;
    }

    private void WriteRuleFile(string relativePath, string yaml) =>
        File.WriteAllText(Path.Combine(_rulesDir, relativePath), yaml);

    private static List<string> SplitLines(string output) =>
        output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static async Task<(int ExitCode, string Output, string Error)> RunListRulesRaw(IReadOnlyList<string> args)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var outWriter = new StringWriter();
        var errorWriter = new StringWriter();
        Console.SetOut(outWriter);
        Console.SetError(errorWriter);
        try
        {
            var exitCode = await ListCommand.Build().Parse(args.ToArray()).InvokeAsync();
            return (exitCode, outWriter.ToString(), errorWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private async Task<(int ExitCode, string Output)> RunListRules(IReadOnlyList<string>? extraArgs = null)
    {
        var args = new List<string> { "--rules-source", _rulesDir };
        if (extraArgs is not null)
        {
            args.AddRange(extraArgs);
        }

        var (exitCode, output, _) = await RunListRulesRaw(args);
        return (exitCode, output);
    }

    public void Dispose() => Directory.Delete(_rulesDir, recursive: true);
}
