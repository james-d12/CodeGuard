using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using CodeGuard.Analysis.Providers;
using CodeGuard.Analyzers.MSBuild;
using CodeGuard.Analyzers.Repository;

namespace CodeGuard.Benchmarks;

/// <summary>
/// Benchmarks AnalysisModelBuilder.BuildAsync against this repo's own CodeGuard.sln (10 real
/// projects, tracked in git) - a realistic multi-project input the tiny SimpleDomainSolution test
/// fixture doesn't provide. Uses the Monitoring strategy because a solution build/analyze pass
/// takes seconds, not microseconds - the usual pipeline-overhead-measuring strategy doesn't apply.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 1, iterationCount: 5)]
public class AnalysisModelBuildBenchmarks
{
    private string _repoRoot = "";
    private string _solutionPath = "";

    [GlobalSetup]
    public void Setup()
    {
        _repoRoot = FindRepoRoot();
        _solutionPath = Path.Combine(_repoRoot, "CodeGuard.sln");
    }

    private static string FindRepoRoot([CallerFilePath] string sourceFilePath = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFilePath)!, "..", ".."));

    [Benchmark]
    public async Task<int> BuildAsync()
    {
        var builder = new AnalysisModelBuilder(
        [
            new RepositoryFileProvider(),
            new MsBuildAnalysisProvider([_solutionPath])
        ]);
        var model = await builder.BuildAsync(_repoRoot);
        return model.Solutions.Sum(s => s.Projects.Count);
    }
}
