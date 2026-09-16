using CodeGuard.Cli.Commands.Rules;

namespace CodeGuard.Cli.Tests.Rules;

/// <summary>Covers the `rules analyze` command end-to-end via its System.CommandLine `Command`.</summary>
[Collection(ConsoleOutputCollection.Name)]
public sealed class AnalyzeCommandTests : IDisposable
{
    private readonly string _rulesDir = Directory.CreateTempSubdirectory("codeguard-rulesanalyze-").FullName;

    [Fact]
    public async Task Run_CleanRuleSet_ExitsZero()
    {
        WriteRuleFile("a.yml", RuleYamlWithTests("DDD-ENTITY-001"));

        var (exitCode, output) = await RunAnalyze();

        Assert.Equal(0, exitCode);
        Assert.Contains("Rules:                    1", output);
        Assert.Contains("Invalid:                  0", output);
    }

    [Fact]
    public async Task Run_RuleWithoutTests_ExitsOneAndListsIt()
    {
        WriteRuleFile("a.yml", RuleYamlWithoutTests("DDD-ENTITY-001"));

        var (exitCode, output) = await RunAnalyze();

        Assert.Equal(1, exitCode);
        Assert.Contains("Rules without tests:      1", output);
        Assert.Contains("DDD-ENTITY-001", output);
    }

    [Fact]
    public async Task Run_JsonFormat_ReportsStructuredFindings()
    {
        WriteRuleFile("a.yml", RuleYamlWithoutTests("DDD-ENTITY-001"));

        var (exitCode, output) = await RunAnalyze(["--format", "json"]);

        Assert.Equal(1, exitCode);
        Assert.Contains("\"rulesWithoutTests\"", output);
        Assert.Contains("DDD-ENTITY-001", output);
    }

    [Fact]
    public async Task Run_NoRulesConfigured_ExitsOneAndPrintsHint()
    {
        using var globalSettings = new IsolatedGlobalSettingsScope();
        var repoDir = Directory.CreateTempSubdirectory("codeguard-rulesanalyze-norules-repo-").FullName;
        try
        {
            var (exitCode, _, error) = await RunAnalyzeRaw(["--path", repoDir]);

            Assert.Equal(1, exitCode);
            Assert.Contains("No rules directory is configured.", error);
        }
        finally
        {
            Directory.Delete(repoDir, recursive: true);
        }
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunAnalyzeRaw(IReadOnlyList<string> args)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var outWriter = new StringWriter();
        var errorWriter = new StringWriter();
        Console.SetOut(outWriter);
        Console.SetError(errorWriter);
        try
        {
            var exitCode = await AnalyzeCommand.Build().Parse(args.ToArray()).InvokeAsync();
            return (exitCode, outWriter.ToString(), errorWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private async Task<(int ExitCode, string Output)> RunAnalyze(IReadOnlyList<string>? extraArgs = null)
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
            var exitCode = await AnalyzeCommand.Build().Parse(args.ToArray()).InvokeAsync();
            return (exitCode, writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static string RuleYamlWithoutTests(string id) => $"""
        id: {id}
        name: Some rule
        target:
          kind: class
          namespace: "Contoso.Domain.Entities"
        assertions:
          - must_inherit_from:
              type: "Contoso.Domain.Entity<TId>"
        """;

    private static string RuleYamlWithTests(string id) => $$"""
        id: {{id}}
        name: Some rule
        target:
          kind: class
          namespace: "Contoso.Domain.Entities"
        assertions:
          - must_inherit_from:
              type: "Contoso.Domain.Entity<TId>"
        tests:
          - name: A passing case
            setup:
              types:
                - name: Order
                  namespace: Contoso.Domain.Entities
                  baseType: "Contoso.Domain.Entity<Guid>"
            expect: pass
          - name: A failing case
            setup:
              types:
                - name: LegacyThing
                  namespace: Contoso.Domain.Entities
            expect: fail
        """;

    private void WriteRuleFile(string relativePath, string yaml) =>
        File.WriteAllText(Path.Combine(_rulesDir, relativePath), yaml);

    public void Dispose() => Directory.Delete(_rulesDir, recursive: true);
}
