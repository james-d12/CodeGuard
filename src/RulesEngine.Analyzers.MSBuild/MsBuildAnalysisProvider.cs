using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Analysis.Providers;
using RulesEngine.Analyzers.Roslyn;
using RoslynProject = Microsoft.CodeAnalysis.Project;

namespace RulesEngine.Analyzers.MSBuild;

public sealed class MsBuildAnalysisProvider(IReadOnlyList<string> solutionPaths) : IAnalysisProvider
{
    public string Name => "MSBuild";

    public async Task ContributeAsync(AnalysisModelBuilderContext context, CancellationToken cancellationToken)
    {
        using var workspace = MSBuildWorkspace.Create();
        workspace.RegisterWorkspaceFailedHandler(e => context.AddDiagnostics([
            new DiagnosticModel(
                Id: "MSBUILD-WORKSPACE",
                Message: e.Diagnostic.Message,
                ProjectName: string.Empty,
                FilePath: string.Empty,
                Line: 0,
                Column: 0)
        ]));

        // A project referenced by more than one solution is only added once, attributed to
        // whichever solution is processed first, so shared projects don't get double-reported.
        var analyzedProjectPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var solutionPath in solutionPaths)
        {
            var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);

            var projectModels = new List<ProjectModel>();

            // Multi-targeted projects produce one Roslyn Project per TFM, all sharing the same
            // FilePath - group them back into a single ProjectModel per project file.
            foreach (var group in solution.Projects.GroupBy(p => p.FilePath!, StringComparer.OrdinalIgnoreCase))
            {
                var projectPath = group.Key;
                if (!analyzedProjectPaths.Add(projectPath))
                {
                    continue;
                }

                var (roslynProject, csharpCompilation) = await ChoosePrimaryAsync(group, cancellationToken);
                if (roslynProject is null || csharpCompilation is null)
                {
                    continue;
                }

                var projectName = Path.GetFileNameWithoutExtension(projectPath);
                var types = RoslynTypeExtractor.ExtractTypes(csharpCompilation, projectName);
                var syntaxFacts = RoslynSyntaxFactExtractor.Extract(csharpCompilation, projectName);
                var diagnostics = RoslynDiagnosticExtractor.Extract(csharpCompilation, projectName);
                context.AddCallSites(syntaxFacts.CallSites);
                context.AddSwitches(syntaxFacts.Switches);
                context.AddThrowSites(syntaxFacts.ThrowSites);
                context.AddMutationSites(syntaxFacts.MutationSites);
                context.AddTryBlocks(syntaxFacts.TryBlocks);
                context.AddMethodBodyShapes(syntaxFacts.MethodBodyShapes);
                context.AddDiagnostics(diagnostics);

                var projectReferenceNames = roslynProject.ProjectReferences
                    .Select(pr => solution.GetProject(pr.ProjectId)?.FilePath)
                    .Where(path => path is not null)
                    .Select(path => Path.GetFileNameWithoutExtension(path!) ?? string.Empty)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var (packageReferences, properties, targetFramework) = EvaluateProjectMetadata(projectPath);

                projectModels.Add(new ProjectModel(
                    Name: projectName,
                    Path: projectPath,
                    TargetFramework: targetFramework,
                    Sdk: "Microsoft.NET.Sdk",
                    ProjectReferences: projectReferenceNames,
                    PackageReferences: packageReferences,
                    Properties: properties,
                    Types: types));
            }

            context.AddSolution(new SolutionModel(solutionPath, projectModels));
        }
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
}
