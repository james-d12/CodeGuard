using System.Runtime.CompilerServices;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Analysis.Providers;
using CodeGuard.Analyzers.MSBuild;

namespace CodeGuard.IntegrationTests;

/// <summary>
/// MsBuildAnalysisProvider parallelizes the per-project loop internally (see
/// MsBuildAnalysisProvider.cs), including a call per project into EvaluateProjectMetadata, which
/// creates its own MSBuild Microsoft.Build.Evaluation.ProjectCollection. That's the concurrency risk
/// flagged in the parallelism plan - these tests exercise it under forced parallelism against a real
/// multi-project solution to catch any MSBuild-engine-level races, not just reason about them.
/// </summary>
public class MsBuildAnalysisProviderParallelismTests
{
    [Fact]
    public async Task BuildAsync_ProducesIdenticalModel_RegardlessOfMaxDegreeOfParallelism()
    {
        var solutionPath = GetFixtureSolutionPath();

        var sequential = await BuildAsync(solutionPath, maxDegreeOfParallelism: 1);
        var parallel = await BuildAsync(solutionPath, maxDegreeOfParallelism: 8);

        AssertIdenticalModels(sequential, parallel);
    }

    [Fact]
    public async Task BuildAsync_IsStableAcrossManyRepeatedRuns_UnderForcedParallelism()
    {
        var solutionPath = GetFixtureSolutionPath();

        var baseline = await BuildAsync(solutionPath, maxDegreeOfParallelism: 8);

        for (var i = 0; i < 20; i++)
        {
            var result = await BuildAsync(solutionPath, maxDegreeOfParallelism: 8);
            AssertIdenticalModels(baseline, result);
        }
    }

    private static async Task<RepositoryModel> BuildAsync(string solutionPath, int maxDegreeOfParallelism)
    {
        var builder = new AnalysisModelBuilder([new MsBuildAnalysisProvider([solutionPath], maxDegreeOfParallelism: maxDegreeOfParallelism)]);
        return await builder.BuildAsync(Path.GetDirectoryName(solutionPath)!);
    }

    private static void AssertIdenticalModels(RepositoryModel expected, RepositoryModel actual)
    {
        Assert.Equal(expected.Solutions.Count, actual.Solutions.Count);

        for (var s = 0; s < expected.Solutions.Count; s++)
        {
            var expectedProjects = expected.Solutions[s].Projects.OrderBy(p => p.Name, StringComparer.Ordinal).ToList();
            var actualProjects = actual.Solutions[s].Projects.OrderBy(p => p.Name, StringComparer.Ordinal).ToList();

            Assert.Equal(expectedProjects.Count, actualProjects.Count);

            for (var p = 0; p < expectedProjects.Count; p++)
            {
                Assert.Equal(expectedProjects[p].Name, actualProjects[p].Name);
                Assert.Equal(expectedProjects[p].TargetFramework, actualProjects[p].TargetFramework);
                Assert.Equal(
                    expectedProjects[p].ProjectReferences.OrderBy(r => r, StringComparer.Ordinal),
                    actualProjects[p].ProjectReferences.OrderBy(r => r, StringComparer.Ordinal));
                Assert.Equal(
                    expectedProjects[p].PackageReferences.Select(r => r.Id).OrderBy(id => id, StringComparer.Ordinal),
                    actualProjects[p].PackageReferences.Select(r => r.Id).OrderBy(id => id, StringComparer.Ordinal));
                Assert.Equal(
                    expectedProjects[p].Types.Select(t => t.FullName).OrderBy(n => n, StringComparer.Ordinal),
                    actualProjects[p].Types.Select(t => t.FullName).OrderBy(n => n, StringComparer.Ordinal));
            }
        }
    }

    private static string GetFixtureSolutionPath([CallerFilePath] string sourceFilePath = "") =>
        Path.Combine(Path.GetDirectoryName(sourceFilePath)!, "Fixtures", "SimpleDomainSolution", "SimpleDomainSolution.sln");
}
