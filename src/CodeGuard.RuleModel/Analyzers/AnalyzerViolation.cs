namespace CodeGuard.RuleModel.Analyzers;

public sealed record AnalyzerViolation(
    string Message,
    string? FilePath = null,
    int? Line = null,
    int? Column = null,
    string? Symbol = null,
    string? ProjectName = null,
    int? EndLine = null,
    int? EndColumn = null);
