using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Selectors;

namespace CodeGuard.Evaluation.Tests.Selectors;

public class DiagnosticSelectorTests
{
    private static DiagnosticModel Diagnostic(string id = "CS1591", string projectName = "Contoso.Domain") =>
        new(id, "message", projectName, "Order.cs", 1, 1);

    [Fact]
    public void SelectCandidates_FiltersById()
    {
        var model = TestModels.RepositoryWithFacts(diagnostics: [Diagnostic(id: "CS1591"), Diagnostic(id: "CS0219")]);

        var candidates = new DiagnosticSelector(idPattern: "CS1591").SelectCandidates(model).Cast<DiagnosticModel>().ToList();

        var match = Assert.Single(candidates);
        Assert.Equal("CS1591", match.Id);
    }

    [Fact]
    public void SelectCandidates_FiltersByProject()
    {
        var model = TestModels.RepositoryWithFacts(diagnostics:
        [
            Diagnostic(projectName: "Contoso.Domain"),
            Diagnostic(projectName: "Contoso.Infrastructure")
        ]);

        var candidates = new DiagnosticSelector(projectPattern: "Contoso.Infrastructure").SelectCandidates(model).ToList();

        Assert.Single(candidates);
    }

    [Fact]
    public void SelectCandidates_ReturnsEmpty_WhenRepositoryHasNoDiagnostics()
    {
        var model = TestModels.RepositoryWithFacts();

        var candidates = new DiagnosticSelector().SelectCandidates(model).ToList();

        Assert.Empty(candidates);
    }
}
