namespace RulesEngine.Analysis.AnalysisModel;

public enum Accessibility
{
    Public,
    Private,
    Protected,
    Internal,
    ProtectedInternal,
    PrivateProtected
}

[Flags]
public enum TypeModifiers
{
    None = 0,
    Static = 1,
    Abstract = 2,
    Sealed = 4,
    Partial = 8
}

[Flags]
public enum MethodModifiers
{
    None = 0,
    Static = 1,
    Abstract = 2,
    Virtual = 4,
    Override = 8,
    Async = 16
}
