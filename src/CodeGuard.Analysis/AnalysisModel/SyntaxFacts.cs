namespace CodeGuard.Analysis.AnalysisModel;

public sealed record SwitchModel(
    string ContainingMethod,
    string ContainingType,
    string ProjectName,
    IReadOnlyList<string> ArmLabels,
    bool HasDefaultOrDiscardArm,
    string FilePath,
    int Line);

public sealed record ThrowSiteModel(
    string ContainingMethod,
    string ContainingType,
    string ProjectName,
    string? ExceptionTypeName,
    bool IsFirstStatementInMethod,
    string FilePath,
    int Line);

public sealed record MutationSiteModel(
    string ContainingMethod,
    string ContainingType,
    string TargetMemberName,
    string ProjectName,
    string FilePath,
    int Line);

public sealed record TryBlockModel(
    string ContainingMethod,
    string ContainingType,
    string ProjectName,
    int CatchClauseCount,
    IReadOnlyList<string> CatchTypeNames,
    string FilePath,
    int Line);

public sealed record MethodBodyShapeModel(
    string ContainingMethod,
    string ContainingType,
    string ProjectName,
    int StatementCount,
    bool IsSingleBaseCallDelegation,
    string FilePath,
    int Line);
