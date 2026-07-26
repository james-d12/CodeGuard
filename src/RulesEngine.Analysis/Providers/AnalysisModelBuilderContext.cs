using RulesEngine.Analysis.AnalysisModel;

namespace RulesEngine.Analysis.Providers;

public sealed class AnalysisModelBuilderContext(string rootPath)
{
    private readonly List<SolutionModel> _solutions = [];
    private readonly List<FileModel> _files = [];

    public string RootPath { get; } = rootPath;

    public void AddSolution(SolutionModel solution) => _solutions.Add(solution);

    public void AddFile(FileModel file) => _files.Add(file);

    public RepositoryModel Build() => new(RootPath, _solutions, _files);
}
