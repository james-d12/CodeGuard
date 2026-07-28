using Microsoft.CodeAnalysis.CSharp;
using RulesEngine.Analysis.AnalysisModel;

namespace RulesEngine.Analyzers.Roslyn;

/// <summary>
/// Reads pure compiler diagnostics (not full analyzer-driven IDE diagnostics - those require
/// running CompilationWithAnalyzers against a loaded DiagnosticAnalyzer, which is out of scope
/// here; see docs/RULE_COVERAGE_PLAN.md's Stage B risk notes) directly off the compilation, so
/// rules can reuse the compiler's own findings (e.g. CS1591) instead of reimplementing them.
/// </summary>
public static class RoslynDiagnosticExtractor
{
    private static readonly HashSet<string> SupportedDiagnosticIds = ["CS1591"];

    public static IReadOnlyList<DiagnosticModel> Extract(CSharpCompilation compilation, string projectName) =>
        compilation.GetDiagnostics()
            .Where(d => SupportedDiagnosticIds.Contains(d.Id))
            .Select(d =>
            {
                var lineSpan = d.Location.GetLineSpan();
                return new DiagnosticModel(
                    d.Id,
                    d.GetMessage(),
                    projectName,
                    lineSpan.Path,
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1);
            })
            .ToList();
}
