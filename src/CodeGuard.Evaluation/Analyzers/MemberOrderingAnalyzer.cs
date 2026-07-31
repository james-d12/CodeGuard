using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Evaluation.Analyzers;

/// <summary>
/// Flags types whose members aren't declared in a configured group order (one of "fields",
/// "constructors", "properties", "methods" per group, default
/// <c>[fields, constructors, properties, methods]</c> - docs/RULE_COVERAGE_PLAN.md
/// coding.type.member-ordering is one example configuration of this, using the default order).
/// Which order to enforce is inherently a convention choice, so it's a rule YAML parameter rather
/// than a single assumption baked into this class - any group name omitted from a custom order
/// sorts after every named group.
/// </summary>
public sealed class MemberOrderingAnalyzer : ICustomAnalyzer
{
    public static readonly IReadOnlyList<string> DefaultOrder = ["fields", "constructors", "properties", "methods"];

    private readonly IReadOnlyDictionary<string, int> _rankByGroup;

    public MemberOrderingAnalyzer(IReadOnlyList<string>? order = null) =>
        _rankByGroup = (order ?? DefaultOrder)
            .Select((name, index) => (name, index))
            .ToDictionary(g => g.name, g => g.index);

    public string Name => "member-ordering";

    public IEnumerable<AnalyzerViolation> Analyze(RepositoryModel model)
    {
        var types = model.Solutions.SelectMany(s => s.Projects).SelectMany(p => p.Types);

        foreach (var type in types)
        {
            var members = type.Fields
                .Select(f => new MemberEntry(RankOf("fields"), f.Line, f.FilePath, f.ProjectName, $"{type.FullName}.{f.Name}"))
                .Concat(type.Constructors.Select(c => new MemberEntry(RankOf("constructors"), c.Line, c.FilePath, c.ProjectName, $"{type.FullName}..ctor")))
                .Concat(type.Properties.Select(p => new MemberEntry(RankOf("properties"), p.Line, p.FilePath, p.ProjectName, $"{type.FullName}.{p.Name}")))
                .Concat(type.Methods.Select(m => new MemberEntry(RankOf("methods"), m.Line, m.FilePath, m.ProjectName, $"{type.FullName}.{m.Name}")))
                .OrderBy(m => m.Line)
                .ToList();

            var maxRankSoFar = -1;
            foreach (var member in members)
            {
                if (member.Rank < maxRankSoFar)
                {
                    yield return new AnalyzerViolation(
                        Message: $"{member.Symbol} is declared out of the configured member order.",
                        FilePath: member.FilePath,
                        Line: member.Line,
                        ProjectName: member.ProjectName);
                    break;
                }

                maxRankSoFar = Math.Max(maxRankSoFar, member.Rank);
            }
        }
    }

    private int RankOf(string group) => _rankByGroup.TryGetValue(group, out var rank) ? rank : int.MaxValue;

    private readonly record struct MemberEntry(int Rank, int Line, string FilePath, string ProjectName, string Symbol);
}
