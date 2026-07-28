using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Analyzers;

namespace RulesEngine.Evaluation.Analyzers;

/// <summary>
/// Flags method overrides that do nothing but delegate to the base implementation with the same
/// arguments - a pure delegation override adds a maintenance surface without changing behaviour,
/// so on a repository base class it almost always means the override should just be deleted
/// (docs/RULE_COVERAGE_PLAN.md golden.persistence.repository-base-class, delegation clause only -
/// the "derives from a repository base type" clause is already declarative via
/// `must_inherit_from`). The base type to scope this to is a rule YAML parameter, not hardcoded
/// here, so this analyzer isn't tied to any one organisation's base class name.
/// </summary>
public sealed class NoPureDelegationOverrideAnalyzer(string baseTypePattern) : ICustomAnalyzer
{
    public string Name => "no-pure-delegation-override";

    public IEnumerable<AnalyzerViolation> Analyze(RepositoryModel model)
    {
        var typesByFullName = model.Solutions
            .SelectMany(s => s.Projects)
            .SelectMany(p => p.Types)
            .ToDictionary(t => t.FullName);

        return model.MethodBodyShapes
            .Where(m => m.IsSingleBaseCallDelegation
                && typesByFullName.TryGetValue(m.ContainingType, out var type)
                && type.BaseType is not null
                && GlobMatcher.IsMatch(type.BaseType, baseTypePattern))
            .Select(m => new AnalyzerViolation(
                Message: $"{m.ContainingType}.{m.ContainingMethod} is a pure delegation override of its base class; remove it instead of overriding.",
                FilePath: m.FilePath,
                Line: m.Line,
                ProjectName: m.ProjectName));
    }
}
