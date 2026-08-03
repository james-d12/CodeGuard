using CodeGuard.Analysis.AnalysisModel;

namespace CodeGuard.Analysis.Providers;

public sealed class AnalysisModelBuilderContext(string rootPath)
{
    private readonly List<SolutionModel> _solutions = [];
    private readonly List<FileModel> _files = [];
    private readonly List<CallSiteModel> _callSites = [];
    private readonly List<SwitchModel> _switches = [];
    private readonly List<ThrowSiteModel> _throwSites = [];
    private readonly List<MutationSiteModel> _mutationSites = [];
    private readonly List<TryBlockModel> _tryBlocks = [];
    private readonly List<MethodBodyShapeModel> _methodBodyShapes = [];
    private readonly List<DiagnosticModel> _diagnostics = [];
    private readonly List<string> _directories = [];

    public string RootPath { get; } = rootPath;

    public void AddSolution(SolutionModel solution) => _solutions.Add(solution);

    public void AddDirectories(IEnumerable<string> directories) => _directories.AddRange(directories);

    public void AddFile(FileModel file) => _files.Add(file);

    public void AddCallSites(IEnumerable<CallSiteModel> callSites) => _callSites.AddRange(callSites);

    public void AddSwitches(IEnumerable<SwitchModel> switches) => _switches.AddRange(switches);

    public void AddThrowSites(IEnumerable<ThrowSiteModel> throwSites) => _throwSites.AddRange(throwSites);

    public void AddMutationSites(IEnumerable<MutationSiteModel> mutationSites) => _mutationSites.AddRange(mutationSites);

    public void AddTryBlocks(IEnumerable<TryBlockModel> tryBlocks) => _tryBlocks.AddRange(tryBlocks);

    public void AddMethodBodyShapes(IEnumerable<MethodBodyShapeModel> methodBodyShapes) => _methodBodyShapes.AddRange(methodBodyShapes);

    public void AddDiagnostics(IEnumerable<DiagnosticModel> diagnostics) => _diagnostics.AddRange(diagnostics);

    public RepositoryModel Build() => new(
        RootPath, _solutions, _files, _callSites, _switches, _throwSites, _mutationSites, _tryBlocks, _methodBodyShapes, _diagnostics,
        _directories);
}
