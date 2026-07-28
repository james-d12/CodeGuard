using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Evaluation.Selectors;

public sealed class CallSiteSelector(
    CallSiteKind? siteKind = null,
    string invokedMemberPattern = "*",
    string targetTypePattern = "*",
    string projectPattern = "*",
    string containingMethodPattern = "*",
    string containingTypePattern = "*",
    int? argumentIndex = null,
    bool? argumentIsLiteral = null,
    string? enclosingComparisonOperator = null) : ITargetSelector
{
    public string Kind => "call_site";

    public IEnumerable<object> SelectCandidates(RepositoryModel model) =>
        model.CallSites
            .Where(cs => siteKind is null || cs.Kind == siteKind)
            .Where(cs => GlobMatcher.IsMatch(cs.InvokedMember, invokedMemberPattern))
            .Where(cs => targetTypePattern == "*" || GlobMatcher.IsMatch(cs.TargetTypeName ?? string.Empty, targetTypePattern))
            .Where(cs => GlobMatcher.IsMatch(cs.ProjectName, projectPattern))
            .Where(cs => GlobMatcher.IsMatch(cs.ContainingMethod, containingMethodPattern))
            .Where(cs => GlobMatcher.IsMatch(cs.ContainingType, containingTypePattern))
            .Where(MatchesArgumentLiteralness)
            .Where(cs => enclosingComparisonOperator is null || cs.EnclosingComparisonOperator == enclosingComparisonOperator)
            .Cast<object>();

    private bool MatchesArgumentLiteralness(CallSiteModel callSite)
    {
        if (argumentIndex is null || argumentIsLiteral is null)
        {
            return true;
        }

        var argument = callSite.Arguments.FirstOrDefault(a => a.Index == argumentIndex);
        return argument is not null && argument.IsLiteral == argumentIsLiteral;
    }
}
