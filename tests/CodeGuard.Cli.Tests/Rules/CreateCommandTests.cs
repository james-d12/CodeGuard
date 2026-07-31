using CodeGuard.Cli.Commands.Rules;
using CodeGuard.Cli.Tests;

namespace CodeGuard.Cli.Tests.Rules;

/// <summary>
/// Covers the interactive `rules create` scaffold end-to-end: scripts stdin via
/// <see cref="Console.SetIn(TextReader)"/> (symmetric to the existing <see cref="Console.SetOut(TextWriter)"/>
/// redirection convention in <see cref="CheckCommandTests"/>) to walk through the prompts
/// deterministically.
/// </summary>
[Collection(ConsoleOutputCollection.Name)]
public class CreateCommandTests : IDisposable
{
    private readonly string _rulesDir = Directory.CreateTempSubdirectory("rulesengine-createrule-").FullName;

    [Fact]
    public async Task Run_FullInteractiveWalkthrough_WritesValidRuleAndExitsZero()
    {
        var input = Lines(
            "TEST-CREATE-001",              // id
            "Test rule",                    // name
            "",                             // description (blank)
            "",                             // severity (blank -> default)
            "",                             // tags (blank)
            "class",                        // target selector kind
            "namespace",                    // target param name
            "Contoso.Domain.Entities",      // target param value
            "",                             // target params finished
            "must_inherit_from",            // assertion kind
            "type",                         // assertion param name
            "Contoso.Domain.Entity<TId>",   // assertion param value
            "",                             // assertion params finished
            "");                            // "add another assertion?" -> no

        var (exitCode, output) = await RunCreate(input);

        Assert.Equal(0, exitCode);
        var filePath = Path.Combine(_rulesDir, "test-create-001.yml");
        Assert.True(File.Exists(filePath));
        var yaml = File.ReadAllText(filePath);
        Assert.Contains("TEST-CREATE-001", yaml);
        Assert.Contains("namespace: Contoso.Domain.Entities", yaml);
        Assert.Contains("must_inherit_from", yaml);
        Assert.Contains($"Created rule 'TEST-CREATE-001' at {filePath}", output);
    }

    [Fact]
    public async Task Run_DuplicateRuleId_WritesFileButExitsOneWithDuplicateReport()
    {
        WriteRuleFile("existing.yml", RuleYaml("DUP-001"));

        var input = Lines(
            "DUP-001",
            "Another rule with the same id",
            "",
            "",
            "",
            "class",
            "namespace",
            "Contoso.Domain.Entities",
            "",
            "must_inherit_from",
            "type",
            "Contoso.Domain.Entity<TId>",
            "",
            "");

        var (exitCode, output) = await RunCreate(input);

        Assert.Equal(1, exitCode);
        Assert.Contains("Duplicate rule id 'DUP-001'", output);
    }

    [Fact]
    public async Task Run_MetadataFlagsSupplied_SkipsThosePromptsAndUsesFlagValues()
    {
        var input = Lines(
            "",                             // description (blank) - id/name/severity prompts skipped via flags
            "",                             // tags (blank)
            "class",                        // target selector kind
            "namespace",
            "Contoso.Domain.Entities",
            "",
            "must_inherit_from",
            "type",
            "Contoso.Domain.Entity<TId>",
            "",
            "");

        var (exitCode, output) = await RunCreate(
            input,
            ["--id", "TEST-CREATE-002", "--name", "Flagged rule", "--severity", "error"]);

        Assert.Equal(0, exitCode);
        var filePath = Path.Combine(_rulesDir, "test-create-002.yml");
        Assert.True(File.Exists(filePath));
        var yaml = File.ReadAllText(filePath);
        Assert.Contains("severity: error", yaml);
        Assert.Contains($"Created rule 'TEST-CREATE-002' at {filePath}", output);
    }

    private async Task<(int ExitCode, string Output)> RunCreate(string input, IReadOnlyList<string>? extraArgs = null)
    {
        var args = new List<string> { "--rules-source", _rulesDir };
        if (extraArgs is not null)
        {
            args.AddRange(extraArgs);
        }

        var originalOut = Console.Out;
        var originalIn = Console.In;
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetIn(new StringReader(input));
        try
        {
            var exitCode = await CreateCommand.Build().Parse(args.ToArray()).InvokeAsync();
            return (exitCode, writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetIn(originalIn);
        }
    }

    private static string Lines(params string[] lines) => string.Join('\n', lines) + "\n";

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
