using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Analyzers;

namespace RulesEngine.Evaluation.Analyzers;

/// <summary>
/// Flags try blocks in a configured namespace whose catch-clause count falls outside a configured
/// range - the default range of exactly one (<paramref name="minCatches"/> ==
/// <paramref name="maxCatches"/> == 1) means zero catches (the try adds nothing over letting the
/// exception propagate) and more than one (the method is doing exception-type-based branching
/// that likely belongs in its own handler) are both flagged
/// (docs/RULE_COVERAGE_PLAN.md skill.application.single-catch-block is one example configuration
/// of this). The namespace scope and catch-count bounds are rule YAML parameters, not hardcoded
/// here, so this analyzer isn't tied to any one organisation's exception-handling policy.
/// </summary>
public sealed class CatchClauseCountAnalyzer(string namespacePattern, int minCatches = 1, int maxCatches = 1) : ICustomAnalyzer
{
    public string Name => "catch-clause-count";

    public IEnumerable<AnalyzerViolation> Analyze(RepositoryModel model) =>
        model.TryBlocks
            .Where(t => GlobMatcher.IsMatch(t.ContainingType, namespacePattern)
                && (t.CatchClauseCount < minCatches || t.CatchClauseCount > maxCatches))
            .Select(t => new AnalyzerViolation(
                Message: $"Try block in {t.ContainingType}.{t.ContainingMethod} has {t.CatchClauseCount} catch clauses; try blocks in this scope must have between {minCatches} and {maxCatches}.",
                FilePath: t.FilePath,
                Line: t.Line,
                ProjectName: t.ProjectName));
}
