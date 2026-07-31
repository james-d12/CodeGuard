namespace CodeGuard.Analysis.AnalysisModel;

public sealed record RepositoryModel(
    string RootPath,
    IReadOnlyList<SolutionModel> Solutions,
    IReadOnlyList<FileModel> Files,
    IReadOnlyList<CallSiteModel> CallSites,
    IReadOnlyList<SwitchModel> Switches,
    IReadOnlyList<ThrowSiteModel> ThrowSites,
    IReadOnlyList<MutationSiteModel> MutationSites,
    IReadOnlyList<TryBlockModel> TryBlocks,
    IReadOnlyList<MethodBodyShapeModel> MethodBodyShapes,
    IReadOnlyList<DiagnosticModel> Diagnostics);

public sealed record SolutionModel(
    string Path,
    IReadOnlyList<ProjectModel> Projects);

public sealed record FileModel(
    string Path,
    string RelativePath,
    string Extension);
