namespace RulesEngine.Analysis.AnalysisModel;

public sealed record DiagnosticModel(
    string Id,
    string Message,
    string ProjectName,
    string FilePath,
    int Line,
    int Column);
