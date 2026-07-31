using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Analyzers;

namespace CodeGuard.Evaluation.Tests.Analyzers;

public class NoExceptionsAnalyzerTests
{
    private static ThrowSiteModel ThrowSite(
        string containingType,
        bool isFirstStatementInMethod) => new(
        ContainingMethod: "Process",
        ContainingType: containingType,
        ProjectName: "Contoso.Domain",
        ExceptionTypeName: "InvalidOperationException",
        IsFirstStatementInMethod: isFirstStatementInMethod,
        FilePath: "Order.cs",
        Line: 20);

    [Fact]
    public void Analyze_Flags_AnyThrowInScope_WhenGuardClauseNotAllowed()
    {
        var model = TestModels.RepositoryWithFacts(
            throwSites: [ThrowSite("Contoso.Domain.Order", isFirstStatementInMethod: true)]);
        var analyzer = new NoExceptionsAnalyzer("Contoso.Domain.*");

        var violations = analyzer.Analyze(model).ToList();

        var violation = Assert.Single(violations);
        Assert.Equal("Order.cs", violation.FilePath);
        Assert.Contains("Contoso.Domain.Order.Process", violation.Message);
        Assert.Contains("is not allowed in this scope", violation.Message);
        Assert.DoesNotContain("leading guard-clause statement", violation.Message);
    }

    [Fact]
    public void Analyze_DoesNotFlag_ThrowOutsideConfiguredNamespace()
    {
        var model = TestModels.RepositoryWithFacts(
            throwSites: [ThrowSite("Contoso.Application.OrderService", isFirstStatementInMethod: true)]);
        var analyzer = new NoExceptionsAnalyzer("Contoso.Domain.*");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_Flags_ThrowThatIsNotFirstStatement_WhenGuardClauseAllowed()
    {
        var model = TestModels.RepositoryWithFacts(
            throwSites: [ThrowSite("Contoso.Domain.Order", isFirstStatementInMethod: false)]);
        var analyzer = new NoExceptionsAnalyzer("Contoso.Domain.*", allowGuardClause: true);

        var violations = analyzer.Analyze(model).ToList();

        var violation = Assert.Single(violations);
        Assert.Contains("Contoso.Domain.Order.Process", violation.Message);
        Assert.Contains("leading guard-clause statement", violation.Message);
        Assert.DoesNotContain("is not allowed in this scope", violation.Message);
    }

    [Fact]
    public void Analyze_DoesNotFlag_ThrowThatIsFirstStatement_WhenGuardClauseAllowed()
    {
        var model = TestModels.RepositoryWithFacts(
            throwSites: [ThrowSite("Contoso.Domain.Order", isFirstStatementInMethod: true)]);
        var analyzer = new NoExceptionsAnalyzer("Contoso.Domain.*", allowGuardClause: true);

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Name_IsNoExceptions()
    {
        var analyzer = new NoExceptionsAnalyzer("*");

        Assert.Equal("no-exceptions", analyzer.Name);
    }
}
