namespace RulesEngine.Analysis.AnalysisModel;

public sealed record RepositoryModel(
    string RootPath,
    IReadOnlyList<SolutionModel> Solutions,
    IReadOnlyList<FileModel> Files);

public sealed record SolutionModel(
    string Path,
    IReadOnlyList<ProjectModel> Projects);

public sealed record FileModel(
    string Path,
    string RelativePath,
    string Extension);
