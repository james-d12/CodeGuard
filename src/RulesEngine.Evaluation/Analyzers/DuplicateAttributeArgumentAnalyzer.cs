using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Analyzers;

namespace RulesEngine.Evaluation.Analyzers;

/// <summary>
/// Flags duplicate attribute-argument values across the entire repository - members carrying a
/// configured attribute are grouped by the constructor-argument literal at a configured index, and
/// every member sharing a value with another member is flagged
/// (docs/RULE_COVERAGE_PLAN.md skill.reporting.eventid-reservation-blocks is one example
/// configuration of this, using an <c>EventIdAttribute</c> and its 0th argument as a reservation
/// ID). Unlike every other analyzer in this repo, this is a genuinely cross-cutting
/// whole-repository pass rather than a per-candidate check. The attribute name and argument index
/// are both rule YAML parameters, not hardcoded here, so this analyzer works for any
/// "no two members may share this attribute value" convention, not just event ID reservations.
/// </summary>
public sealed class DuplicateAttributeArgumentAnalyzer(string attributeNamePattern, int argumentIndex = 0) : ICustomAnalyzer
{
    public string Name => "duplicate-attribute-argument";

    public IEnumerable<AnalyzerViolation> Analyze(RepositoryModel model)
    {
        var types = model.Solutions.SelectMany(s => s.Projects).SelectMany(p => p.Types);

        var reservations =
            (from type in types
             from reservation in Collect(type)
             select reservation)
            .ToList();

        return reservations
            .GroupBy(r => r.Value)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .Select(r => new AnalyzerViolation(
                Message: $"{r.Symbol} has attribute argument value '{r.Value}', which is already used elsewhere in the repository.",
                FilePath: r.FilePath,
                Line: r.Line,
                ProjectName: r.ProjectName));
    }

    private IEnumerable<Reservation> Collect(TypeModel type) =>
        type.Methods.Select(m => (Symbol: $"{type.FullName}.{m.Name}", m.Attributes, m.FilePath, m.Line, m.ProjectName))
            .Concat(type.Properties.Select(p => (Symbol: $"{type.FullName}.{p.Name}", p.Attributes, p.FilePath, p.Line, p.ProjectName)))
            .Concat(type.Fields.Select(f => (Symbol: $"{type.FullName}.{f.Name}", f.Attributes, f.FilePath, f.Line, f.ProjectName)))
            .SelectMany(member => member.Attributes
                .Where(a => GlobMatcher.IsMatch(a.TypeName, attributeNamePattern) && a.ConstructorArgumentLiterals.Count > argumentIndex)
                .Select(a => new Reservation(member.Symbol, a.ConstructorArgumentLiterals[argumentIndex], member.FilePath, member.Line, member.ProjectName)));

    private readonly record struct Reservation(string Symbol, string Value, string FilePath, int Line, string ProjectName);
}
