using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Analyzers;

namespace RulesEngine.Evaluation.Tests.Analyzers;

public class CatchClauseCountAnalyzerTests
{
    private static TryBlockModel TryBlock(string containingType, int catchClauseCount) => new(
        ContainingMethod: "Handle",
        ContainingType: containingType,
        ProjectName: "Contoso.Application",
        CatchClauseCount: catchClauseCount,
        CatchTypeNames: Enumerable.Range(0, catchClauseCount).Select(_ => "Exception").ToList(),
        FilePath: "OrderCommandHandler.cs",
        Line: 30);

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Analyze_Flags_TryBlockInScopeOutsideDefaultRangeOfExactlyOne(int catchClauseCount)
    {
        var model = TestModels.RepositoryWithFacts(
            tryBlocks: [TryBlock("Contoso.Application.OrderCommandHandler", catchClauseCount)]);
        var analyzer = new CatchClauseCountAnalyzer("Contoso.Application.*");

        var violations = analyzer.Analyze(model).ToList();

        var violation = Assert.Single(violations);
        Assert.Equal("OrderCommandHandler.cs", violation.FilePath);
        Assert.Contains("Contoso.Application.OrderCommandHandler.Handle", violation.Message);
    }

    [Fact]
    public void Analyze_DoesNotFlag_TryBlockInScopeWithExactlyOneCatch()
    {
        var model = TestModels.RepositoryWithFacts(
            tryBlocks: [TryBlock("Contoso.Application.OrderCommandHandler", 1)]);
        var analyzer = new CatchClauseCountAnalyzer("Contoso.Application.*");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_DoesNotFlag_TryBlockOutsideConfiguredNamespaceEvenWithoutOneCatch()
    {
        var model = TestModels.RepositoryWithFacts(
            tryBlocks: [TryBlock("Contoso.Domain.OrderProcessor", 0)]);
        var analyzer = new CatchClauseCountAnalyzer("Contoso.Application.*");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_RespectsConfiguredRange_AllowingUpToTwoCatches()
    {
        var model = TestModels.RepositoryWithFacts(
            tryBlocks: [TryBlock("Contoso.Application.OrderCommandHandler", 2)]);
        var analyzer = new CatchClauseCountAnalyzer("Contoso.Application.*", minCatches: 1, maxCatches: 2);

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Name_IsCatchClauseCount()
    {
        var analyzer = new CatchClauseCountAnalyzer("*");

        Assert.Equal("catch-clause-count", analyzer.Name);
    }
}
