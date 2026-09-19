using System.Text.Json;
using CodeGuard.Cli.Commands.Rules;

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
    public async Task Run_MalformedConfig_PrintsFriendlyErrorAndExitsOne()
    {
        var repoDir = Directory.CreateTempSubdirectory("codeguard-explain-malformed-repo-").FullName;
        try
        {
            var configDir = Directory.CreateDirectory(Path.Combine(repoDir, ".codeguard"));
            var configPath = Path.Combine(configDir.FullName, "config.yml");
            await File.WriteAllTextAsync(configPath, "repository: [this, is, not, a, map]");

            var (exitCode, _, error) = await RunExplainRaw(["--path", repoDir, "ANY-001"]);

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

            // No metadata.source on this rule - serialized as an explicit JSON null, same as the
            // other optional fields here (description/remediation), not omitted.
            Assert.Equal(JsonValueKind.Null, root.GetProperty("metadata").ValueKind);
        }
        finally
        {
            Directory.Delete(rulesDir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_Json_WithMetadataSource_IncludesProvenance()
    {
        var rulesDir = Directory.CreateTempSubdirectory("codeguard-explain-json-metadata-").FullName;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(rulesDir, "rule.yml"), """
                id: DDD-ENTITY-001
                name: Entities inherit Entity
                metadata:
                  source:
                    document: Architecture Standards
                    section: "4.2 Layering"
                    statement: Domain entities must inherit from the shared Entity base.
                    file: docs/architecture.md
                    fingerprint: "sha256:0000000000000000000000000000000000000000000000000000000000000000"
                target:
                  kind: class
                  namespace: "Contoso.Domain"
                assertions:
                  - must_inherit_from:
                      type: "Entity<*>"
                """);

            var (exitCode, output) = await RunExplain(["--rules-source", rulesDir, "DDD-ENTITY-001", "--format", "json"]);

            Assert.Equal(0, exitCode);
            using var document = JsonDocument.Parse(output);
            var source = document.RootElement.GetProperty("metadata").GetProperty("source");

            Assert.Equal("Architecture Standards", source.GetProperty("document").GetString());
            Assert.Equal("4.2 Layering", source.GetProperty("section").GetString());
            Assert.Equal("Domain entities must inherit from the shared Entity base.", source.GetProperty("statement").GetString());
            Assert.Equal("docs/architecture.md", source.GetProperty("file").GetString());
            Assert.Equal(
                "sha256:0000000000000000000000000000000000000000000000000000000000000000",
                source.GetProperty("fingerprint").GetString());
        }
        finally
        {
            Directory.Delete(rulesDir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_Json_WithMetadataSource_NoFileOrFingerprint_SerializedAsExplicitNull()
    {
        var rulesDir = Directory.CreateTempSubdirectory("codeguard-explain-json-metadata-nofile-").FullName;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(rulesDir, "rule.yml"), """
                id: DDD-ENTITY-001
                name: Entities inherit Entity
                metadata:
                  source:
                    document: Architecture Standards
                target:
                  kind: class
                  namespace: "Contoso.Domain"
                assertions:
                  - must_inherit_from:
                      type: "Entity<*>"
                """);

            var (exitCode, output) = await RunExplain(["--rules-source", rulesDir, "DDD-ENTITY-001", "--format", "json"]);

            Assert.Equal(0, exitCode);
            using var document = JsonDocument.Parse(output);
            var source = document.RootElement.GetProperty("metadata").GetProperty("source");

            // file/fingerprint are optional, same "explicit null, not omitted" convention as
            // document/section/statement - the feature is opt-in, this rule doesn't use it.
            Assert.Equal(JsonValueKind.Null, source.GetProperty("file").ValueKind);
            Assert.Equal(JsonValueKind.Null, source.GetProperty("fingerprint").ValueKind);
        }
        finally
        {
            Directory.Delete(rulesDir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_Json_WithVersionFingerprint_IncludesItAtTopLevel()
    {
        var rulesDir = Directory.CreateTempSubdirectory("codeguard-explain-json-versionfingerprint-").FullName;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(rulesDir, "rule.yml"), """
                id: DDD-ENTITY-001
                name: Entities inherit Entity
                version: 3
                versionFingerprint: "sha256:0000000000000000000000000000000000000000000000000000000000000000"
                target:
                  kind: class
                  namespace: "Contoso.Domain"
                assertions:
                  - must_inherit_from:
                      type: "Entity<*>"
                """);

            var (exitCode, output) = await RunExplain(["--rules-source", rulesDir, "DDD-ENTITY-001", "--format", "json"]);

            Assert.Equal(0, exitCode);
            using var document = JsonDocument.Parse(output);
            var root = document.RootElement;

            Assert.Equal(3, root.GetProperty("version").GetInt32());
            Assert.Equal(
                "sha256:0000000000000000000000000000000000000000000000000000000000000000",
                root.GetProperty("versionFingerprint").GetString());
            // No metadata.source on this rule - versionFingerprint living at the top level rather
            // than under metadata means it has no bearing on whether "metadata" itself is null.
            Assert.Equal(JsonValueKind.Null, root.GetProperty("metadata").ValueKind);
        }
        finally
        {
            Directory.Delete(rulesDir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_Json_WithMetadataSourceAndVersionFingerprint_IncludesBothIndependently()
    {
        var rulesDir = Directory.CreateTempSubdirectory("codeguard-explain-json-both-").FullName;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(rulesDir, "rule.yml"), """
                id: DDD-ENTITY-001
                name: Entities inherit Entity
                versionFingerprint: "sha256:0000000000000000000000000000000000000000000000000000000000000000"
                metadata:
                  source:
                    document: Architecture Standards
                target:
                  kind: class
                  namespace: "Contoso.Domain"
                assertions:
                  - must_inherit_from:
                      type: "Entity<*>"
                """);

            var (exitCode, output) = await RunExplain(["--rules-source", rulesDir, "DDD-ENTITY-001", "--format", "json"]);

            Assert.Equal(0, exitCode);
            using var document = JsonDocument.Parse(output);
            var root = document.RootElement;

            Assert.Equal(
                "sha256:0000000000000000000000000000000000000000000000000000000000000000",
                root.GetProperty("versionFingerprint").GetString());
            Assert.Equal(
                "Architecture Standards", root.GetProperty("metadata").GetProperty("source").GetProperty("document").GetString());
        }
        finally
        {
            Directory.Delete(rulesDir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_Console_PrintsVersionAndFingerprintNotYetCaptured()
    {
        var rulesDir = Directory.CreateTempSubdirectory("codeguard-explain-console-").FullName;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(rulesDir, "rule.yml"), """
                id: DDD-ENTITY-001
                name: Entities inherit Entity
                version: 2
                target:
                  kind: class
                  namespace: "Contoso.Domain"
                assertions:
                  - must_inherit_from:
                      type: "Entity<*>"
                """);

            var (exitCode, output) = await RunExplain(["--rules-source", rulesDir, "DDD-ENTITY-001"]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Version:       2", output);
            Assert.Contains("Fingerprint:   not yet captured", output);
            Assert.Contains("Target kind:   class", output);
            Assert.Contains("--- Raw YAML ---", output);
        }
        finally
        {
            Directory.Delete(rulesDir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_Console_WithCapturedFingerprint_PrintsFingerprintLine()
    {
        var rulesDir = Directory.CreateTempSubdirectory("codeguard-explain-console-captured-").FullName;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(rulesDir, "rule.yml"), """
                id: DDD-ENTITY-001
                name: Entities inherit Entity
                versionFingerprint: "sha256:0000000000000000000000000000000000000000000000000000000000000000"
                target:
                  kind: class
                  namespace: "Contoso.Domain"
                assertions:
                  - must_inherit_from:
                      type: "Entity<*>"
                """);

            var (exitCode, output) = await RunExplain(["--rules-source", rulesDir, "DDD-ENTITY-001"]);

            Assert.Equal(0, exitCode);
            Assert.Contains(
                "Fingerprint:   sha256:0000000000000000000000000000000000000000000000000000000000000000", output);
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

    private static async Task<(int ExitCode, string Output, string Error)> RunExplainRaw(IReadOnlyList<string> args)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var outWriter = new StringWriter();
        var errorWriter = new StringWriter();
        Console.SetOut(outWriter);
        Console.SetError(errorWriter);
        try
        {
            var exitCode = await ExplainCommand.Build().Parse(args.ToArray()).InvokeAsync();
            return (exitCode, outWriter.ToString(), errorWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }
}
