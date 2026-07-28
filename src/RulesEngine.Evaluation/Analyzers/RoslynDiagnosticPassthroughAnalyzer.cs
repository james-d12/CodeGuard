using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Analyzers;

namespace RulesEngine.Evaluation.Analyzers;

/// <summary>
/// Republishes selected compiler/analyzer diagnostics already captured on
/// <see cref="RepositoryModel.Diagnostics"/> as rule violations, instead of reimplementing checks
/// the compiler or its analyzers already perform (docs/RULE_COVERAGE_PLAN.md Stage B §Phase B4).
/// Only compiler diagnostics (currently CS1591) are extracted today - true IDE analyzer diagnostics
/// (IDE0005/IDE0011/IDE0161) would require loading a DiagnosticAnalyzer via CompilationWithAnalyzers,
/// which the analyzer NuGet packages don't expose as a normal library reference; see the Stage B
/// plan's Risk 1 for why that was not pursued.
/// </summary>
public sealed class RoslynDiagnosticPassthroughAnalyzer(IReadOnlyList<string> diagnosticIds) : ICustomAnalyzer
{
    public string Name => "roslyn-diagnostic-passthrough";

    public IEnumerable<AnalyzerViolation> Analyze(RepositoryModel model) =>
        model.Diagnostics
            .Where(d => diagnosticIds.Contains(d.Id))
            .Select(d => new AnalyzerViolation(
                Message: $"{d.Id}: {d.Message}",
                FilePath: d.FilePath,
                Line: d.Line,
                Column: d.Column,
                ProjectName: d.ProjectName));
}
