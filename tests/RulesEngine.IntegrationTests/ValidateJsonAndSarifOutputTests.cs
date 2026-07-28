using System.Runtime.CompilerServices;
using System.Text.Json;
using RulesEngine.Analysis.Providers;
using RulesEngine.Analyzers.MSBuild;
using RulesEngine.Analyzers.Repository;
using RulesEngine.Configuration.Loading;
using RulesEngine.Core.Evaluation;
using RulesEngine.Core.Results;
using RulesEngine.Reporting.Json;
using RulesEngine.Reporting.Sarif;

namespace RulesEngine.IntegrationTests;

public class ValidateJsonAndSarifOutputTests
{
    [Fact]
    public async Task JsonReporter_ProducesSchemaConformantOutput_ForFixtureSolution()
    {
        var result = await EvaluateFixtureAsync();

        var writer = new StringWriter();
        await new JsonViolationReporter().WriteAsync(result, writer);

        using var document = JsonDocument.Parse(writer.ToString());
        var root = document.RootElement;

        Assert.Equal("failed", root.GetProperty("status").GetString());
        Assert.True(root.GetProperty("violations").GetArrayLength() > 0);

        var ddd001Violation = root.GetProperty("violations").EnumerateArray()
            .First(v => v.GetProperty("ruleId").GetString() == "DDD-ENTITY-001");
        Assert.Equal("Contoso.Domain.Entities.LegacyThing", ddd001Violation.GetProperty("symbol").GetString());
        Assert.True(ddd001Violation.GetProperty("line").GetInt32() > 0);
    }

    [Fact]
    public async Task SarifReporter_ProducesValidSarifLog_ForFixtureSolution()
    {
        var result = await EvaluateFixtureAsync();

        var writer = new StringWriter();
        await new SarifViolationReporter().WriteAsync(result, writer);

        using var document = JsonDocument.Parse(writer.ToString());
        var root = document.RootElement;

        Assert.Equal("2.1.0", root.GetProperty("version").GetString());

        var run = root.GetProperty("runs")[0];
        Assert.Equal("rules-engine", run.GetProperty("tool").GetProperty("driver").GetProperty("name").GetString());
        Assert.True(run.GetProperty("results").GetArrayLength() > 0);

        var ddd001Result = run.GetProperty("results").EnumerateArray()
            .First(r => r.GetProperty("ruleId").GetString() == "DDD-ENTITY-001");
        Assert.Equal("error", ddd001Result.GetProperty("level").GetString());
        Assert.True(ddd001Result.GetProperty("locations")[0]
            .GetProperty("physicalLocation").GetProperty("region").GetProperty("startLine").GetInt32() > 0);
    }

    private static async Task<ValidationResult> EvaluateFixtureAsync()
    {
        var solutionPath = GetFixtureSolutionPath();
        var rulesDirectory = GetRepoRulesDirectory();

        var builder = new AnalysisModelBuilder(
            [new RepositoryFileProvider(), new MsBuildAnalysisProvider([solutionPath])]);
        var model = await builder.BuildAsync(Path.GetDirectoryName(solutionPath)!);

        var rules = RuleFileLoader.CreateDefault().LoadFromDirectory(rulesDirectory);
        return new RuleEvaluator().Evaluate(rules, model);
    }

    private static string GetFixtureSolutionPath([CallerFilePath] string sourceFilePath = "") =>
        Path.Combine(Path.GetDirectoryName(sourceFilePath)!, "Fixtures", "SimpleDomainSolution", "SimpleDomainSolution.sln");

    private static string GetRepoRulesDirectory([CallerFilePath] string sourceFilePath = "")
    {
        var testProjectDir = Path.GetDirectoryName(sourceFilePath)!;
        var repoRoot = Path.GetFullPath(Path.Combine(testProjectDir, "..", ".."));
        return Path.Combine(repoRoot, "rules");
    }
}
