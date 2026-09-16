using CodeGuard.Cli.Commands;

namespace CodeGuard.Cli.Tests;

/// <summary>
/// Covers `validate`'s mandatory rule-set pre-flight gate (docs/done/RULE_VALIDATION_PLAN.md): a broken
/// ruleset must produce a clean aggregated report and a non-zero exit code, and must never reach
/// <c>MsBuildAnalysisProvider</c>/Buildalyzer. <c>--path</c> points at a directory with no `.sln` at
/// all, so any attempt to proceed past the gate would surface as a solution-resolution failure
/// instead of the rule-validation report asserted below.
/// </summary>
[Collection(ConsoleOutputCollection.Name)]
public sealed class ValidateCommandPreflightTests : IDisposable
{
    private readonly string _rulesDir = Directory.CreateTempSubdirectory("codeguard-validate-rules-").FullName;
    private readonly string _repoDir = Directory.CreateTempSubdirectory("codeguard-validate-repo-").FullName;

    [Fact]
    public async Task Validate_WithBrokenRuleFile_ExitsOneWithReport_WithoutReachingAnalysis()
    {
        await File.WriteAllTextAsync(Path.Combine(_rulesDir, "bad.yml"), """
                                                                         id: DDD-ENTITY-001
                                                                         name: Some rule
                                                                         target:
                                                                           kind: not_a_real_kind
                                                                         assertions:
                                                                           - must_inherit_from:
                                                                               type: "Contoso.Domain.Entity<TId>"
                                                                         """);

        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        int exitCode;
        try
        {
            exitCode = await ValidateCommand.Build()
                .Parse(["--path", _repoDir, "--rules-source", _rulesDir])
                .InvokeAsync();
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var output = writer.ToString();
        Assert.Equal(1, exitCode);
        Assert.Contains("Checked 1 rule file: 0 passed, 1 failed.", output);
        Assert.Contains("not_a_real_kind", output);
    }

    [Fact]
    public async Task Validate_NoRulesConfigured_ExitsOneAndPrintsHint()
    {
        using var globalSettings = new IsolatedGlobalSettingsScope();
        var repoDirWithNoConfig = Directory.CreateTempSubdirectory("codeguard-validate-norules-repo-").FullName;
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
                exitCode = await ValidateCommand.Build().Parse(["--path", repoDirWithNoConfig]).InvokeAsync();
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
            Directory.Delete(repoDirWithNoConfig, recursive: true);
        }
    }

    [Fact]
    public async Task Validate_WithMissingSolutionFile_PrintsCleanErrorAndExitsOne_WithoutRawStackTrace()
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
            exitCode = await ValidateCommand.Build()
                .Parse(["--path", _repoDir, "--rules-source", _rulesDir, "--solution", "does-not-exist.sln"])
                .InvokeAsync();
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }

        var errorOutput = errorWriter.ToString();
        Assert.Equal(1, exitCode);
        Assert.Contains("codeguard:", errorOutput);
        Assert.Contains("does-not-exist.sln", errorOutput);
        Assert.DoesNotContain("Unhandled exception", errorOutput);
        Assert.DoesNotContain(" at ", errorOutput);
    }

    [Fact]
    public async Task Validate_WithMultipleSolutionsFound_PrintsCleanErrorListingThemAndExitsOne()
    {
        var firstSlnDir = Directory.CreateDirectory(Path.Combine(_repoDir, "proj1"));
        var firstSlnPath = Path.Combine(firstSlnDir.FullName, "First.sln");
        await File.WriteAllTextAsync(firstSlnPath, "");

        var secondSlnDir = Directory.CreateDirectory(Path.Combine(_repoDir, "proj2"));
        var secondSlnPath = Path.Combine(secondSlnDir.FullName, "Second.sln");
        await File.WriteAllTextAsync(secondSlnPath, "");

        var originalOut = Console.Out;
        var originalError = Console.Error;
        var outWriter = new StringWriter();
        var errorWriter = new StringWriter();
        Console.SetOut(outWriter);
        Console.SetError(errorWriter);
        int exitCode;
        try
        {
            exitCode = await ValidateCommand.Build()
                .Parse(["--path", _repoDir, "--rules-source", _rulesDir])
                .InvokeAsync();
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }

        var errorOutput = errorWriter.ToString();
        Assert.Equal(1, exitCode);
        Assert.Contains("codeguard:", errorOutput);
        Assert.Contains(firstSlnPath, errorOutput);
        Assert.Contains(secondSlnPath, errorOutput);
        Assert.Contains("--solution", errorOutput);
        Assert.DoesNotContain("Unhandled exception", errorOutput);
        Assert.DoesNotContain(" at ", errorOutput);
    }

    public void Dispose()
    {
        Directory.Delete(_rulesDir, recursive: true);
        Directory.Delete(_repoDir, recursive: true);
    }
}
