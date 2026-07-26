using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;
using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Analysis.Providers;
using RulesEngine.Analyzers.Roslyn;

namespace RulesEngine.Analyzers.MSBuild;

public sealed class MsBuildAnalysisProvider(string solutionPath) : IAnalysisProvider
{
    public string Name => "MSBuild";

    public async Task ContributeAsync(AnalysisModelBuilderContext context, CancellationToken cancellationToken)
    {
#pragma warning disable CS0618 // AnalyzerManager(string) is obsolete in favor of an internal IOPath overload
        var manager = new AnalyzerManager(solutionPath);
#pragma warning restore CS0618
        using var workspace = new AdhocWorkspace();

        var projectModels = new List<ProjectModel>();
        foreach (var projectAnalyzer in manager.Projects.Values)
        {
            var results = projectAnalyzer.Build();
            var result = results.FirstOrDefault(r => r.Succeeded) ?? results.First();

            var types = await ExtractTypesAsync(result, workspace, cancellationToken);

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

    private static async Task<IReadOnlyList<TypeModel>> ExtractTypesAsync(
        IAnalyzerResult result,
        AdhocWorkspace workspace,
        CancellationToken cancellationToken)
    {
        var roslynProject = result.AddToWorkspace(workspace);
        var compilation = await roslynProject.GetCompilationAsync(cancellationToken);

        if (compilation is not Microsoft.CodeAnalysis.CSharp.CSharpCompilation csharpCompilation)
        {
            return [];
        }

        return RoslynTypeExtractor.ExtractTypes(csharpCompilation, Path.GetFileNameWithoutExtension(result.ProjectFilePath));
    }
}
