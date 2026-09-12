using System.Text.Json;
using CodeGuard.Cli.Commands.Rules;
using CodeGuard.Cli.Tests;

namespace CodeGuard.Cli.Tests.Rules;

/// <summary>Covers the `rules explain` command end-to-end.</summary>
[Collection(ConsoleOutputCollection.Name)]
public class ExplainCommandTests
{
    [Fact]
    public async Task Run_NoRulesConfigured_ExitsOneAndPrintsHint()
    {
        using var globalSettings = new IsolatedGlobalSettingsScope();
        var repoDir = Directory.CreateTempSubdirectory("codeguard-explain-norules-repo-").FullName;
        try
        {
            var originalOut = Console.Out;
            var originalError = Console.Error;
            var outWriter = new StringWriter();
            var errorWriter = new StringWriter();
            Console.SetOut(outWriter);
            Console.SetError(errorWriter);
            int exitCode;
            try
            {
                exitCode = await ExplainCommand.Build().Parse(["--path", repoDir, "DDD-ENTITY-001"]).InvokeAsync();
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }

            Assert.Equal(1, exitCode);
            Assert.Contains("No rules directory is configured.", errorWriter.ToString());
            Assert.Contains("codeguard setup", errorWriter.ToString());
            Assert.Contains("--rules-source", errorWriter.ToString());
        }
        finally
        {
            Directory.Delete(repoDir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_Json_IncludesMetadataAndTheSourceDocument()
    {
        var rulesDir = Directory.CreateTempSubdirectory("codeguard-explain-json-").FullName;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(rulesDir, "rule.yml"), """
                id: DDD-ENTITY-001
                name: Entities inherit Entity
                description: Domain entities must inherit from the shared Entity base.
                severity: error
                enforcement:
                  classification: deterministic
                tags:
                  - ddd
                target:
                  kind: class
                  namespace: "Contoso.Domain"
                assertions:
                  - must_inherit_from:
                      type: "Entity<*>"
                tests:
                  - name: Inherits
                    setup:
                      types:
                        - name: Order
                          namespace: Contoso.Domain
                          baseType: "Entity<Guid>"
                    expect: pass
                """);

            var (exitCode, output) = await RunExplain(["--rules-source", rulesDir, "DDD-ENTITY-001", "--format", "json"]);

            Assert.Equal(0, exitCode);
            using var document = JsonDocument.Parse(output);
            var root = document.RootElement;

            Assert.Equal("DDD-ENTITY-001", root.GetProperty("id").GetString());
            Assert.Equal("error", root.GetProperty("severity").GetString());
            Assert.Equal("deterministic", root.GetProperty("enforcement").GetProperty("classification").GetString());
            Assert.Equal("declarative", root.GetProperty("shape").GetString());
            Assert.Equal(1, root.GetProperty("testCount").GetInt32());

            // The point of the json format: assertion parameter *values* survive, which they cannot
            // when reconstructed from the parsed rule - IAssertion exposes only a Kind.
            var assertion = root.GetProperty("document").GetProperty("assertions")[0];
            Assert.Equal("Entity<*>", assertion.GetProperty("must_inherit_from").GetProperty("type").GetString());
            Assert.Equal("class", root.GetProperty("document").GetProperty("target").GetProperty("kind").GetString());
        }
        finally
        {
            Directory.Delete(rulesDir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_Json_UnknownRule_ExitsOne()
    {
        var rulesDir = Directory.CreateTempSubdirectory("codeguard-explain-missing-").FullName;
        try
        {
            var (exitCode, _) = await RunExplain(["--rules-source", rulesDir, "NOPE-001", "--format", "json"]);
            Assert.Equal(1, exitCode);
        }
        finally
        {
            Directory.Delete(rulesDir, recursive: true);
        }
    }

    private static async Task<(int ExitCode, string Output)> RunExplain(IReadOnlyList<string> args)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(new StringWriter());
        try
        {
            var exitCode = await ExplainCommand.Build().Parse(args.ToArray()).InvokeAsync();
            return (exitCode, writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }
}
