using CodeGuard.Analysis.Providers;
using CodeGuard.Analyzers.MSBuild;

namespace CodeGuard.IntegrationTests;

/// <summary>
/// Covers the "opening the solution itself fails" path (e.g. a target repo's global.json pinning
/// an uninstalled .NET SDK, or - as exercised here, deterministically and without any SDK-version
/// dependency - a syntactically invalid .sln). MsBuildAnalysisProvider must wrap whatever
/// MSBuildWorkspace throws into a SolutionLoadException carrying the solution path, instead of
/// letting the raw exception (and its huge internal stack trace) escape uncaught.
/// </summary>
public class MsBuildAnalysisProviderSolutionLoadFailureTests
{
    [Fact]
    public async Task ContributeAsync_WithUnparsableSolutionFile_ThrowsSolutionLoadException()
    {
        var tempDir = Directory.CreateTempSubdirectory("codeguard-solution-load-failure-");
        try
        {
            var solutionPath = Path.Combine(tempDir.FullName, "broken.sln");
            File.WriteAllText(solutionPath, "this is not a valid solution file");

            var builder = new AnalysisModelBuilder([new MsBuildAnalysisProvider([solutionPath])]);

            var exception = await Assert.ThrowsAsync<SolutionLoadException>(
                () => builder.BuildAsync(tempDir.FullName));

            Assert.Contains("Failed to open solution", exception.Message);
            Assert.Contains(solutionPath, exception.Message);
            Assert.NotNull(exception.InnerException);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }
}
