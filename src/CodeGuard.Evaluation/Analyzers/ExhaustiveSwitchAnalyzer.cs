using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Evaluation.Analyzers;

/// <summary>
/// Flags switch statements/expressions in a configured namespace with no default or discard arm,
/// since this engine can't prove exhaustiveness against the switch's governing type from the
/// captured facts alone - a missing fallback arm means new enum members or type-pattern cases can
/// silently go unhandled (docs/RULE_COVERAGE_PLAN.md skill.domain.event-mapping-exhaustive is one
/// example configuration of this). The namespace scope is a rule YAML parameter, not hardcoded
/// here, so this analyzer isn't tied to any one organisation's layout.
/// </summary>
public sealed class ExhaustiveSwitchAnalyzer(string namespacePattern) : ICustomAnalyzer
{
    public string Name => "exhaustive-switch";

    public IEnumerable<AnalyzerViolation> Analyze(RepositoryModel model) =>
        model.Switches
            .Where(s => GlobMatcher.IsMatch(s.ContainingType, namespacePattern) && !s.HasDefaultOrDiscardArm)
            .Select(s => new AnalyzerViolation(
                Message: $"Switch in {s.ContainingType}.{s.ContainingMethod} has no default/discard arm and cannot be proven exhaustive.",
                FilePath: s.FilePath,
                Line: s.Line,
                ProjectName: s.ProjectName));
}
