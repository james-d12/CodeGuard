using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Analysis.Providers;
using CodeGuard.Analyzers.Roslyn;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RoslynProject = Microsoft.CodeAnalysis.Project;

namespace CodeGuard.Analyzers.MSBuild;

public sealed class MsBuildAnalysisProvider(
    IReadOnlyList<string> solutionPaths,
    ILogger<MsBuildAnalysisProvider>? logger = null,
    int? maxDegreeOfParallelism = null) : IAnalysisProvider
{
    private readonly ILogger<MsBuildAnalysisProvider> _logger = logger ?? NullLogger<MsBuildAnalysisProvider>.Instance;

    public string Name => "MSBuild";

    public async Task ContributeAsync(AnalysisModelBuilderContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Analyzing {SolutionCount} solution(s) via MSBuildWorkspace", solutionPaths.Count);

        // MSBuildWorkspace itself is not safe for concurrent use (OpenSolutionAsync/workspace-mutating
        // calls), so solutions are opened one at a time on this single shared instance. Once a Solution
        // is loaded it's an immutable snapshot - Project/Compilation/SemanticModel reads on it are safe
        // to fan out across threads, which is what the per-project loop below does.
        using var workspace = MSBuildWorkspace.Create();
        workspace.RegisterWorkspaceFailedHandler(e =>
        {
            _logger.LogWarning("MSBuild workspace diagnostic: {Message}", e.Diagnostic.Message);
            context.AddDiagnostics([
                new DiagnosticModel(
                    Id: "MSBUILD-WORKSPACE",
                    Message: e.Diagnostic.Message,
                    ProjectName: string.Empty,
                    FilePath: string.Empty,
                    Line: 0,
                    Column: 0)
            ]);
        });

        // A project referenced by more than one solution is only added once, attributed to
        // whichever solution is processed first, so shared projects don't get double-reported.
        var analyzedProjectPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var solutionPath in solutionPaths)
        {
            _logger.LogDebug("Opening solution {SolutionPath}", solutionPath);
            Solution solution;
            try
            {
                solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new SolutionLoadException(solutionPath, ex);
            }

            // Multi-targeted projects produce one Roslyn Project per TFM, all sharing the same
            // FilePath - group them back into a single ProjectModel per project file. Dedup happens
            // here, sequentially, before any parallel work starts, so the parallel section below never
            // needs to touch analyzedProjectPaths.
            var groups = solution.Projects
                .GroupBy(p => p.FilePath!, StringComparer.OrdinalIgnoreCase)
                .Where(group => analyzedProjectPaths.Add(group.Key))
                .ToList();

            var results = new ProjectAnalysisResult?[groups.Count];
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = maxDegreeOfParallelism ?? Environment.ProcessorCount
            };

            await Parallel.ForEachAsync(
                Enumerable.Range(0, groups.Count),
                parallelOptions,
                async (i, ct) => results[i] = await AnalyzeProjectAsync(groups[i], solution, ct));

            // Fold sequentially in original group order (not completion order) so output stays
            // deterministic regardless of which project's analysis finished first.
            var projectModels = new List<ProjectModel>();
            foreach (var result in results)
            {
                if (result is null)
                {
                    continue;
                }

                context.AddCallSites(result.CallSites);
                context.AddSwitches(result.Switches);
                context.AddThrowSites(result.ThrowSites);
                context.AddMutationSites(result.MutationSites);
                context.AddTryBlocks(result.TryBlocks);
                context.AddMethodBodyShapes(result.MethodBodyShapes);
                context.AddDiagnostics(result.Diagnostics);
                projectModels.Add(result.ProjectModel);
            }

            _logger.LogInformation("Solution {SolutionPath}: {ProjectCount} project(s) analyzed", solutionPath, projectModels.Count);
            context.AddSolution(new SolutionModel(solutionPath, projectModels));
        }
    }

    private async Task<ProjectAnalysisResult?> AnalyzeProjectAsync(
        IGrouping<string, RoslynProject> group, Solution solution, CancellationToken cancellationToken)
    {
        var projectPath = group.Key;
        var (roslynProject, csharpCompilation) = await ChoosePrimaryAsync(group, cancellationToken);
        if (roslynProject is null || csharpCompilation is null)
        {
            _logger.LogWarning("Project {ProjectPath} produced no usable C# compilation; skipping", projectPath);
            return null;
        }

        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        var types = RoslynTypeExtractor.ExtractTypes(csharpCompilation, projectName);
        var syntaxFacts = RoslynSyntaxFactExtractor.Extract(csharpCompilation, projectName);
        var diagnostics = RoslynDiagnosticExtractor.Extract(csharpCompilation, projectName);

        var projectReferenceNames = roslynProject.ProjectReferences
            .Select(pr => solution.GetProject(pr.ProjectId)?.FilePath)
            .Where(path => path is not null)
            .Select(path => Path.GetFileNameWithoutExtension(path!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var (packageReferences, properties, targetFramework) = EvaluateProjectMetadata(projectPath);

        var projectModel = new ProjectModel(
            Name: projectName,
            Path: projectPath,
            TargetFramework: targetFramework,
            Sdk: "Microsoft.NET.Sdk",
            ProjectReferences: projectReferenceNames,
            PackageReferences: packageReferences,
            Properties: properties,
            Types: types);

        return new ProjectAnalysisResult(
            projectModel,
            syntaxFacts.CallSites,
            syntaxFacts.Switches,
            syntaxFacts.ThrowSites,
            syntaxFacts.MutationSites,
            syntaxFacts.TryBlocks,
            syntaxFacts.MethodBodyShapes,
            diagnostics);
    }

    private static async Task<(RoslynProject? Project, CSharpCompilation? Compilation)> ChoosePrimaryAsync(
        IEnumerable<RoslynProject> candidates,
        CancellationToken cancellationToken)
    {
        RoslynProject? first = null;
        foreach (var candidate in candidates)
        {
            first ??= candidate;

            var compilation = await candidate.GetCompilationAsync(cancellationToken);
            if (compilation is CSharpCompilation csharpCompilation)
            {
                return (candidate, csharpCompilation);
            }
        }

        // None produced a usable compilation - fall back to the first candidate with no compilation,
        // mirroring the old "first successful result, else the first result" fallback.
        return (first, null);
    }

    /// <summary>
    /// Creates its own ProjectCollection per call so concurrent calls (one per project, from the
    /// parallel loop in ContributeAsync) don't share evaluation state.
    /// </summary>
    private static (IReadOnlyList<PackageReferenceModel> PackageReferences, IReadOnlyDictionary<string, string> Properties, string TargetFramework) EvaluateProjectMetadata(
        string projectPath)
    {
        using var collection = new ProjectCollection();
        var evaluated = collection.LoadProject(projectPath);

        var packageReferences = evaluated.GetItems("PackageReference")
            .Select(item => new PackageReferenceModel(item.EvaluatedInclude, item.GetMetadataValue("Version")))
            .ToList();

        var properties = evaluated.AllEvaluatedProperties
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().EvaluatedValue, StringComparer.OrdinalIgnoreCase);

        var targetFramework = evaluated.GetPropertyValue("TargetFramework");

        collection.UnloadAllProjects();

        return (packageReferences, properties, targetFramework);
    }

    private sealed record ProjectAnalysisResult(
        ProjectModel ProjectModel,
        IReadOnlyList<CallSiteModel> CallSites,
        IReadOnlyList<SwitchModel> Switches,
        IReadOnlyList<ThrowSiteModel> ThrowSites,
        IReadOnlyList<MutationSiteModel> MutationSites,
        IReadOnlyList<TryBlockModel> TryBlocks,
        IReadOnlyList<MethodBodyShapeModel> MethodBodyShapes,
        IReadOnlyList<DiagnosticModel> Diagnostics);
}
