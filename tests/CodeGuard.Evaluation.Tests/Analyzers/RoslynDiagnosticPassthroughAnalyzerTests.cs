using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Analyzers;

namespace CodeGuard.Evaluation.Tests.Analyzers;

public class RoslynDiagnosticPassthroughAnalyzerTests
{
    private static DiagnosticModel Diagnostic(string id, string message = "message") => new(
        Id: id,
        Message: message,
        ProjectName: "Contoso.Domain",
        FilePath: "Order.cs",
        Line: 12,
        Column: 5);

    [Fact]
    public void Analyze_ProducesViolation_ForMatchingDiagnosticId()
    {
        var model = TestModels.RepositoryWithFacts(diagnostics: [Diagnostic("CS1591", "Missing XML comment")]);
        var analyzer = new RoslynDiagnosticPassthroughAnalyzer(["CS1591"]);

        var violation = Assert.Single(analyzer.Analyze(model));

        Assert.Equal("CS1591: Missing XML comment", violation.Message);
        Assert.Equal("Order.cs", violation.FilePath);
        Assert.Equal(12, violation.Line);
        Assert.Equal(5, violation.Column);
        Assert.Equal("Contoso.Domain", violation.ProjectName);
    }

    [Fact]
    public void Analyze_IgnoresDiagnostic_WithNonMatchingId()
    {
        var model = TestModels.RepositoryWithFacts(diagnostics: [Diagnostic("CS0168")]);
        var analyzer = new RoslynDiagnosticPassthroughAnalyzer(["CS1591"]);

        Assert.Empty(analyzer.Analyze(model));
    }

    [Fact]
    public void Analyze_ReturnsEmpty_WhenNoDiagnosticsPresent()
    {
        var model = TestModels.RepositoryWithFacts();
        var analyzer = new RoslynDiagnosticPassthroughAnalyzer(["CS1591"]);

        Assert.Empty(analyzer.Analyze(model));
    }

    [Fact]
    public void Analyze_ProducesOneViolationPerMatchingDiagnostic_WhenMultiplePresent()
    {
        var model = TestModels.RepositoryWithFacts(diagnostics:
        [
            Diagnostic("CS1591", "first"),
            Diagnostic("CS0168", "ignored"),
            Diagnostic("CS1591", "second")
        ]);
        var analyzer = new RoslynDiagnosticPassthroughAnalyzer(["CS1591"]);

        var violations = analyzer.Analyze(model).ToList();

        Assert.Equal(2, violations.Count);
        Assert.Contains(violations, v => v.Message == "CS1591: first");
        Assert.Contains(violations, v => v.Message == "CS1591: second");
    }

    [Fact]
    public void Name_IsRoslynDiagnosticPassthrough()
    {
        var analyzer = new RoslynDiagnosticPassthroughAnalyzer([]);

        Assert.Equal("roslyn-diagnostic-passthrough", analyzer.Name);
    }
}
