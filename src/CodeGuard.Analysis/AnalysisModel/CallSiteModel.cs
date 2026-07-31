namespace CodeGuard.Analysis.AnalysisModel;

public enum CallSiteKind
{
    Invocation,
    ObjectCreation,
    MemberAccess
}

public sealed record CallSiteArgument(int Index, string? LiteralValue, bool IsLiteral);

public sealed record CallSiteModel(
    CallSiteKind Kind,
    string InvokedMember,
    string? TargetTypeName,
    string ContainingMethod,
    string ContainingType,
    string ProjectName,
    IReadOnlyList<CallSiteArgument> Arguments,
    string FilePath,
    int Line,
    int Column,
    string? EnclosingComparisonOperator = null,
    string? EnclosingComparisonValue = null);
