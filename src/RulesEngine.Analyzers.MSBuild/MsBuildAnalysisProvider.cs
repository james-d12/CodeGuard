using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;
using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Analysis.Providers;
using RulesEngine.Analyzers.Roslyn;

namespace RulesEngine.Analyzers.MSBuild;

public sealed class MsBuildAnalysisProvider(IReadOnlyList<string> solutionPaths) : IAnalysisProvider
{
    public string Name => "MSBuild";

    public async Task ContributeAsync(AnalysisModelBuilderContext context, CancellationToken cancellationToken)
    {
        using var workspace = new AdhocWorkspace();

        // A project referenced by more than one solution is only built/added once, attributed to
        // whichever solution is processed first, so shared projects don't get double-reported.
        var analyzedProjectPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var solutionPath in solutionPaths)
        {
#pragma warning disable CS0618 // AnalyzerManager(string) is obsolete in favor of an internal IOPath overload
            var manager = new AnalyzerManager(solutionPath);
#pragma warning restore CS0618

            var projectModels = new List<ProjectModel>();
            foreach (var (projectPath, projectAnalyzer) in manager.Projects)
            {
                if (!analyzedProjectPaths.Add(projectPath))
                {
                    continue;
                }

                var results = projectAnalyzer.Build();
                var result = results.FirstOrDefault(r => r.Succeeded) ?? results.First();

                var (types, syntaxFacts, diagnostics) = await ExtractTypesAsync(result, workspace, cancellationToken);
                context.AddCallSites(syntaxFacts.CallSites);
                context.AddSwitches(syntaxFacts.Switches);
                context.AddThrowSites(syntaxFacts.ThrowSites);
                context.AddMutationSites(syntaxFacts.MutationSites);
                context.AddTryBlocks(syntaxFacts.TryBlocks);
                context.AddMethodBodyShapes(syntaxFacts.MethodBodyShapes);
                context.AddDiagnostics(diagnostics);

                projectModels.Add(new ProjectModel(
                    Name: Path.GetFileNameWithoutExtension(result.ProjectFilePath),
                    Path: result.ProjectFilePath,
                    TargetFramework: result.TargetFramework ?? string.Empty,
                    Sdk: "Microsoft.NET.Sdk",
                    ProjectReferences: result.ProjectReferences.Select(p => Path.GetFileNameWithoutExtension(p)).ToList(),
                    PackageReferences: result.PackageReferences
                        .Select(p => new PackageReferenceModel(p.Key, p.Value.GetValueOrDefault("Version") ?? string.Empty))
                        .ToList(),
                    Properties: result.Properties,
                    Types: types));
            }

            context.AddSolution(new SolutionModel(solutionPath, projectModels));
        }
    }

    private static async Task<(IReadOnlyList<TypeModel> Types, ExtractedSyntaxFacts SyntaxFacts, IReadOnlyList<DiagnosticModel> Diagnostics)> ExtractTypesAsync(
        IAnalyzerResult result,
        AdhocWorkspace workspace,
        CancellationToken cancellationToken)
    {
        var roslynProject = result.AddToWorkspace(workspace);
        var compilation = await roslynProject.GetCompilationAsync(cancellationToken);

        if (compilation is not Microsoft.CodeAnalysis.CSharp.CSharpCompilation csharpCompilation)
        {
            return ([], new ExtractedSyntaxFacts([], [], [], [], [], []), []);
        }

        var projectName = Path.GetFileNameWithoutExtension(result.ProjectFilePath);
        var types = RoslynTypeExtractor.ExtractTypes(csharpCompilation, projectName);
        var syntaxFacts = RoslynSyntaxFactExtractor.Extract(csharpCompilation, projectName);
        var diagnostics = RoslynDiagnosticExtractor.Extract(csharpCompilation, projectName);
        return (types, syntaxFacts, diagnostics);
    }
}
