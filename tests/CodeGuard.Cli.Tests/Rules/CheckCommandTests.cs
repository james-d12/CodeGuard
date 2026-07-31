using CodeGuard.Cli.Commands.Rules;
using CodeGuard.Cli.Tests;

namespace CodeGuard.Cli.Tests.Rules;

/// <summary>Covers the `rules check` command end-to-end via its System.CommandLine `Command`, and the
/// pre-flight gate `validate` shares with it (docs/done/RULE_VALIDATION_PLAN.md).</summary>
[Collection(ConsoleOutputCollection.Name)]
public class CheckCommandTests : IDisposable
{
    private readonly string _rulesDir = Directory.CreateTempSubdirectory("rulesengine-checkrules-").FullName;

    [Fact]
    public async Task Run_AllRulesValid_ExitsZeroAndReportsAllPassed()
    {
        WriteRuleFile("a.yml", RuleYaml("DDD-ENTITY-001"));
        WriteRuleFile("b.yml", RuleYaml("DDD-ENTITY-002"));

        var (exitCode, output) = await RunCheckRules();

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

        var (exitCode, output) = await RunCheckRules();

        Assert.Equal(1, exitCode);
        Assert.Contains("Checked 1 rule file: 0 passed, 1 failed.", output);
        Assert.Contains("not_a_real_kind", output);
    }

    [Fact]
    public async Task Run_DuplicateRuleId_ExitsOneAndReportsBoth()
    {
        WriteRuleFile("a.yml", RuleYaml("DDD-ENTITY-001"));
        WriteRuleFile("b.yml", RuleYaml("DDD-ENTITY-001"));

        var (exitCode, output) = await RunCheckRules();

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

        var (exitCode, output) = await RunCheckRules(["--format", "json"]);

        Assert.Equal(1, exitCode);
        Assert.Contains("\"isValid\": false", output);
    }

    [Fact]
    public async Task Run_NoRulesConfigured_ExitsOneAndPrintsHint()
    {
        using var globalSettings = new IsolatedGlobalSettingsScope();
        var repoDir = Directory.CreateTempSubdirectory("codeguard-check-norules-repo-").FullName;
        try
        {
            var (exitCode, _, error) = await RunCheckRulesRaw(["--path", repoDir]);

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

    private static async Task<(int ExitCode, string Output, string Error)> RunCheckRulesRaw(IReadOnlyList<string> args)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var outWriter = new StringWriter();
        var errorWriter = new StringWriter();
        Console.SetOut(outWriter);
        Console.SetError(errorWriter);
        try
        {
            var exitCode = await CheckCommand.Build().Parse(args.ToArray()).InvokeAsync();
            return (exitCode, outWriter.ToString(), errorWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private async Task<(int ExitCode, string Output)> RunCheckRules(IReadOnlyList<string>? extraArgs = null)
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
            var exitCode = await CheckCommand.Build().Parse(args.ToArray()).InvokeAsync();
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

    public void Dispose() => Directory.Delete(_rulesDir, recursive: true);
}
