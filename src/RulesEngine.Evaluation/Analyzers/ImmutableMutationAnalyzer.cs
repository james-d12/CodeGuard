using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Analyzers;

namespace RulesEngine.Evaluation.Analyzers;

/// <summary>
/// Flags field/property mutations against C# record types in a configured namespace outside their
/// constructor - records are meant to be immutable value objects, so any assignment after
/// construction (Roslyn's constructor symbol name is literally ".ctor") defeats that guarantee
/// (docs/RULE_COVERAGE_PLAN.md skill.domain.immutable-mutation). The namespace scope is a rule
/// YAML parameter, not hardcoded here, so this analyzer isn't tied to any one organisation's layout.
/// </summary>
public sealed class ImmutableMutationAnalyzer(string namespacePattern) : ICustomAnalyzer
{
    public string Name => "immutable-mutation";

    public IEnumerable<AnalyzerViolation> Analyze(RepositoryModel model)
    {
        var recordTypeNames = model.Solutions
            .SelectMany(s => s.Projects)
            .SelectMany(p => p.Types)
            .Where(t => t.Kind == TypeKind.Record && GlobMatcher.IsMatch(t.FullName, namespacePattern))
            .Select(t => t.FullName)
            .ToHashSet();

        return model.MutationSites
            .Where(m => recordTypeNames.Contains(m.ContainingType) && m.ContainingMethod != ".ctor")
            .Select(m => new AnalyzerViolation(
                Message: $"Mutation of {m.ContainingType}.{m.TargetMemberName} outside its constructor violates record immutability.",
                FilePath: m.FilePath,
                Line: m.Line,
                ProjectName: m.ProjectName));
    }
}
