using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Evaluation.Analyzers;

/// <summary>
/// Flags throw sites in a configured namespace. With <paramref name="allowGuardClause"/> left at
/// its default of <c>false</c>, every throw in scope is flagged - a plain "no exceptions in this
/// area" policy. Set it to <c>true</c> to instead only flag throws that aren't the leading
/// guard-clause statement of their method, i.e. "exceptions are only for validating preconditions,
/// not business logic" (docs/RULE_COVERAGE_PLAN.md skill.domain.no-business-exceptions is one
/// example configuration of this - <c>namespace: Contoso.Domain.*, allow_guard_clause: true</c>).
/// The namespace scope and guard-clause toggle are both rule YAML parameters, not hardcoded here,
/// so this analyzer isn't tied to any one organisation's exception policy.
/// </summary>
public sealed class NoExceptionsAnalyzer(string namespacePattern, bool allowGuardClause = false) : ICustomAnalyzer
{
    public string Name => "no-exceptions";

    public IEnumerable<AnalyzerViolation> Analyze(RepositoryModel model) =>
        model.ThrowSites
            .Where(t => GlobMatcher.IsMatch(t.ContainingType, namespacePattern)
                && !(allowGuardClause && t.IsFirstStatementInMethod))
            .Select(t => new AnalyzerViolation(
                Message: allowGuardClause
                    ? $"Throw in {t.ContainingType}.{t.ContainingMethod} is not the leading guard-clause statement; exceptions must not be used for business logic."
                    : $"Throw in {t.ContainingType}.{t.ContainingMethod} is not allowed in this scope.",
                FilePath: t.FilePath,
                Line: t.Line,
                ProjectName: t.ProjectName));
}
