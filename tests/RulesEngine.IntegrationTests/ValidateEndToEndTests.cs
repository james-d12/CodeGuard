using System.Runtime.CompilerServices;
using RulesEngine.Analysis.Providers;
using RulesEngine.Analyzers.MSBuild;
using RulesEngine.Configuration.Loading;
using RulesEngine.Core.Evaluation;
using RulesEngine.Core.Results;

namespace RulesEngine.IntegrationTests;

public class ValidateEndToEndTests
{
    [Fact]
    public async Task Validate_AgainstSimpleDomainSolution_ProducesExpectedResults()
    {
        var solutionPath = GetFixtureSolutionPath();
        var rulesDirectory = GetRepoRulesDirectory();

        var builder = new AnalysisModelBuilder([new MsBuildAnalysisProvider([solutionPath])]);
        var model = await builder.BuildAsync(Path.GetDirectoryName(solutionPath)!);

        var rules = RuleFileLoader.CreateDefault().LoadFromDirectory(rulesDirectory);
        var result = new RuleEvaluator().Evaluate(rules, model);

        var violationsByRuleId = result.Violations.ToLookup(v => v.RuleId);

        // DDD-ENTITY-001: Order inherits Entity<int> (pass); LegacyThing has no base type (fail).
        Assert.Contains(violationsByRuleId["DDD-ENTITY-001"], v => v.Symbol == "Contoso.Domain.Entities.LegacyThing");
        Assert.DoesNotContain(violationsByRuleId["DDD-ENTITY-001"], v => v.Symbol == "Contoso.Domain.Entities.Order");

        // DDD-ENTITY-002: Order has a private constructor (pass); LegacyThing has none (fail).
        Assert.Contains(violationsByRuleId["DDD-ENTITY-002"], v => v.Symbol == "Contoso.Domain.Entities.LegacyThing");
        Assert.DoesNotContain(violationsByRuleId["DDD-ENTITY-002"], v => v.Symbol == "Contoso.Domain.Entities.Order");

        // DDD-AGGREGATE-001/002: Order inherits Entity<*>, implements IAggregateRoot, has Create.
        Assert.DoesNotContain(violationsByRuleId["DDD-AGGREGATE-001"], v => v.Symbol == "Contoso.Domain.Entities.Order");
        Assert.DoesNotContain(violationsByRuleId["DDD-AGGREGATE-002"], v => v.Symbol == "Contoso.Domain.Entities.Order");

        // DDD-EVENT-001: OrderPlaced is correctly namespaced (pass); BadlyPlacedEvent is not (fail).
        Assert.Contains(violationsByRuleId["DDD-EVENT-001"], v => v.Symbol == "Contoso.Domain.BadlyPlacedEvent");
        Assert.DoesNotContain(violationsByRuleId["DDD-EVENT-001"], v => v.Symbol == "Contoso.Domain.Events.OrderPlaced");

        // DDD-EVENT-002: both events still live in the Contoso.Domain project.
        Assert.DoesNotContain(violationsByRuleId["DDD-EVENT-002"], v => v.Symbol == "Contoso.Domain.Events.OrderPlaced");
        Assert.DoesNotContain(violationsByRuleId["DDD-EVENT-002"], v => v.Symbol == "Contoso.Domain.BadlyPlacedEvent");

        // APP-COMMANDHANDLER-001: PlaceOrderCommandHandler implements ICommandHandler<PlaceOrderCommand>.
        Assert.DoesNotContain(
            violationsByRuleId["APP-COMMANDHANDLER-001"],
            v => v.Symbol == "Contoso.Application.Handlers.PlaceOrderCommandHandler");

        // ARCH-DEPENDENCY-001: Contoso.Domain illegally references Contoso.Infrastructure.
        Assert.Contains(violationsByRuleId["ARCH-DEPENDENCY-001"], v => v.Project == "Contoso.Domain");

        // ARCH-DEPENDENCY-002: no Domain type actually references an Infrastructure type.
        Assert.Empty(violationsByRuleId["ARCH-DEPENDENCY-002"]);

        // ARCH-PACKAGE-001: Contoso.Domain has no EF Core package reference.
        Assert.Empty(violationsByRuleId["ARCH-PACKAGE-001"]);

        // CSHARP-NAMESPACE-001: every type in the fixture lives under Contoso.*.
        Assert.Empty(violationsByRuleId["CSHARP-NAMESPACE-001"]);

        Assert.Equal(ValidationStatus.Failed, result.Status);
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
