using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Evaluation.Analyzers;

/// <summary>
/// Flags types implementing a configured marker interface that don't have exactly one matching
/// "companion" type named <c>{TypeName}{companionSuffix}</c> (docs/RULE_COVERAGE_PLAN.md
/// skill.persistence.partition-key-builder-class is one example configuration of this, using
/// marker interface <c>Contoso.Domain.IAggregateRoot</c> and suffix <c>PartitionKeyBuilder</c>) -
/// zero means the companion was never implemented, more than one means it's ambiguous which one
/// is authoritative. The marker interface and companion-name suffix are both required rule YAML
/// parameters (there's no sane repo-agnostic default for either), so this analyzer isn't tied to
/// any one organisation's naming or to partition keys specifically - the same mechanism works for
/// any "every X must have exactly one Y" naming convention (e.g. every command needing exactly one
/// validator).
/// </summary>
public sealed class CompanionTypeCardinalityAnalyzer(
    string markerInterfacePattern,
    string companionSuffix) : ICustomAnalyzer
{
    public string Name => "companion-type-cardinality";

    public IEnumerable<AnalyzerViolation> Analyze(RepositoryModel model)
    {
        var types = model.Solutions.SelectMany(s => s.Projects).SelectMany(p => p.Types).ToList();

        var markerTypes = types.Where(t =>
            t.Interfaces.Any(i => GlobMatcher.IsMatch(i, markerInterfacePattern)));

        foreach (var markerType in markerTypes)
        {
            var expectedCompanionName = $"{markerType.Name}{companionSuffix}";
            var matchingCompanions = types.Count(t => t.Name == expectedCompanionName);

            if (matchingCompanions != 1)
            {
                yield return new AnalyzerViolation(
                    Message: $"{markerType.FullName} has {matchingCompanions} type(s) named '{expectedCompanionName}'; expected exactly one.",
                    FilePath: markerType.FilePath,
                    Line: markerType.Line,
                    ProjectName: markerType.ProjectName);
            }
        }
    }
}
