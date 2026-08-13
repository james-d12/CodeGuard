using CodeGuard.Cli.Commands.Rules;
using CodeGuard.Cli.Tests;

namespace CodeGuard.Cli.Tests.Rules;

/// <summary>Covers the `rules test` command end-to-end via its System.CommandLine `Command`.</summary>
[Collection(ConsoleOutputCollection.Name)]
public class TestCommandTests : IDisposable
{
    private readonly string _rulesDir = Directory.CreateTempSubdirectory("codeguard-rulestest-").FullName;

    [Fact]
    public async Task Run_RuleWithPassingAndFailingTests_ExitsOneAndReportsBoth()
    {
        WriteRuleFile("entity.yml", $$"""
            id: DDD-ENTITY-001
            name: Some rule
            target:
              kind: class
              namespace: "Contoso.Domain.Entities"
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<*>"
            tests:
              - name: Entity inheriting from Entity
                setup:
                  types:
                    - name: Order
                      namespace: Contoso.Domain.Entities
                      baseType: "Contoso.Domain.Entity<Guid>"
                expect: pass
              - name: Entity not inheriting from Entity
                setup:
                  types:
                    - name: Order
                      namespace: Contoso.Domain.Entities
                expect: fail
              - name: Entity incorrectly expected to fail
                setup:
                  types:
                    - name: Order
                      namespace: Contoso.Domain.Entities
                      baseType: "Contoso.Domain.Entity<Guid>"
                expect: fail
            """);

        var (exitCode, output) = await RunRulesTest();

        Assert.Equal(1, exitCode);
        Assert.Contains("Tests: 3", output);
        Assert.Contains("Passed: 2", output);
        Assert.Contains("Failed: 1", output);
    }

    [Fact]
    public async Task Run_RuleWithNoTests_ExitsZeroAndReportsNone()
    {
        WriteRuleFile("no-tests.yml", """
            id: DDD-ENTITY-002
            name: Some rule
            target:
              kind: class
              namespace: "Contoso.Domain.Entities"
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<*>"
            """);

        var (exitCode, output) = await RunRulesTest();

        Assert.Equal(0, exitCode);
        Assert.Contains("Tests: 0", output);
    }

    [Fact]
    public async Task Run_RuleFilter_OnlyRunsSelectedRule()
    {
        WriteRuleFile("a.yml", PassingRuleYaml("DDD-ENTITY-001"));
        WriteRuleFile("b.yml", PassingRuleYaml("DDD-ENTITY-002"));

        var (exitCode, output) = await RunRulesTest(["--rule", "DDD-ENTITY-001"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("DDD-ENTITY-001", output);
        Assert.DoesNotContain("DDD-ENTITY-002", output);
    }

    [Fact]
    public async Task Run_JsonFormat_ReportsOutcomesAsStrings()
    {
        WriteRuleFile("a.yml", PassingRuleYaml("DDD-ENTITY-001"));

        var (exitCode, output) = await RunRulesTest(["--format", "json"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("\"outcome\": \"passed\"", output);
    }

    [Fact]
    public async Task Run_MalformedSetup_ReportsErrored()
    {
        WriteRuleFile("errors.yml", """
            id: REPO-001
            name: Some rule
            target:
              kind: repository
            assertions:
              - must_have_directory:
                  path: src
            tests:
              - name: Setup type is missing a required 'name'
                setup:
                  types:
                    - namespace: Contoso.Domain
                expect: pass
            """);

        var (exitCode, output) = await RunRulesTest();

        Assert.Equal(1, exitCode);
        Assert.Contains("Errored: 1", output);
    }

    private static string PassingRuleYaml(string id) => $$"""
        id: {{id}}
        name: Some rule
        target:
          kind: class
          namespace: "Contoso.Domain.Entities"
        assertions:
          - must_inherit_from:
              type: "Contoso.Domain.Entity<*>"
        tests:
          - name: Entity inheriting from Entity
            setup:
              types:
                - name: Order
                  namespace: Contoso.Domain.Entities
                  baseType: "Contoso.Domain.Entity<Guid>"
            expect: pass
        """;

    private async Task<(int ExitCode, string Output)> RunRulesTest(IReadOnlyList<string>? extraArgs = null)
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
            var exitCode = await TestCommand.Build().Parse(args.ToArray()).InvokeAsync();
            return (exitCode, writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private void WriteRuleFile(string relativePath, string yaml) =>
        File.WriteAllText(Path.Combine(_rulesDir, relativePath), yaml);

    public void Dispose() => Directory.Delete(_rulesDir, recursive: true);
}
