using CodeGuard.Analysis.Providers;
using CodeGuard.Analyzers.MSBuild;

namespace CodeGuard.IntegrationTests;

/// <summary>
/// Covers <see cref="MsBuildAnalysisProvider.BuildWorkspaceFailureDiagnostic"/> - the pure logic behind
/// the <c>RegisterWorkspaceFailedHandler</c> callback in <c>ContributeAsync</c> - directly, since driving
/// every raw message shape through a real MSBuildWorkspace failure would be slow and platform-fragile.
/// <see cref="MsBuildAnalysisProviderWorkspaceDiagnosticWiringTests"/> below covers one real,
/// deterministic end-to-end failure to prove the callback is actually wired up to this helper and to
/// <see cref="CodeGuard.Analysis.AnalysisModel.RepositoryModel.Diagnostics"/>.
/// </summary>
public class MsBuildAnalysisProviderWorkspaceDiagnosticTests
{
    [Fact]
    public void BuildWorkspaceFailureDiagnostic_WithProjectPathInMessage_ExtractsProjectNameAndLogSuffix()
    {
        const string raw = "Msbuild failed when processing the file '/repo/src/Foo/Foo.csproj' with message: " +
            "Something went wrong.";

        var (diagnostic, logSuffix) = MsBuildAnalysisProvider.BuildWorkspaceFailureDiagnostic(raw);

        Assert.Equal("MSBUILD-WORKSPACE", diagnostic.Id);
        Assert.Equal("Something went wrong.", diagnostic.Message);
        Assert.Equal("Foo", diagnostic.ProjectName);
        Assert.Equal("/repo/src/Foo/Foo.csproj", diagnostic.FilePath);
        Assert.Equal(0, diagnostic.Line);
        Assert.Equal(0, diagnostic.Column);
        Assert.Equal(" (Foo)", logSuffix);
    }

    [Fact]
    public void BuildWorkspaceFailureDiagnostic_WithoutProjectPathInMessage_LeavesProjectFieldsEmpty()
    {
        const string raw = "Project file not found: '/repo/src/Missing/Missing.csproj'";

        var (diagnostic, logSuffix) = MsBuildAnalysisProvider.BuildWorkspaceFailureDiagnostic(raw);

        Assert.Equal("MSBUILD-WORKSPACE", diagnostic.Id);
        Assert.Equal(raw, diagnostic.Message);
        Assert.Equal(string.Empty, diagnostic.ProjectName);
        Assert.Equal(string.Empty, diagnostic.FilePath);
        Assert.Equal(string.Empty, logSuffix);
    }
}

/// <summary>
/// Drives <see cref="MsBuildAnalysisProvider"/> against a real solution containing a project reference
/// MSBuildWorkspace cannot resolve, so the <c>RegisterWorkspaceFailedHandler</c> callback actually fires -
/// "Project file not found" is deterministic on any platform/SDK version, unlike failures that depend on
/// a specific build task or SDK resolver behavior.
/// </summary>
public class MsBuildAnalysisProviderWorkspaceDiagnosticWiringTests
{
    [Fact]
    public async Task ContributeAsync_WithUnresolvableProjectReference_RecordsMsBuildWorkspaceDiagnostic()
    {
        var tempDir = Directory.CreateTempSubdirectory("codeguard-workspace-diagnostic-");
        try
        {
            var validProjectDir = Path.Combine(tempDir.FullName, "Valid");
            Directory.CreateDirectory(validProjectDir);
            await File.WriteAllTextAsync(Path.Combine(validProjectDir, "Valid.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");

            var missingProjectPath = Path.Combine(tempDir.FullName, "Missing", "Missing.csproj");

            var slnPath = Path.Combine(tempDir.FullName, "WorkspaceDiagnostic.sln");
            const string sln = """
                Microsoft Visual Studio Solution File, Format Version 12.00
                # Visual Studio Version 17
                VisualStudioVersion = 17.0.31903.59
                MinimumVisualStudioVersion = 10.0.40219.1
                Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Valid", "Valid\Valid.csproj", "{509968EA-344C-486F-BD35-FE47551787C6}"
                EndProject
                Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Missing", "Missing\Missing.csproj", "{3B526C75-D7E2-4879-81D9-4237CC3D05E5}"
                EndProject
                Global
                	GlobalSection(SolutionConfigurationPlatforms) = preSolution
                		Debug|Any CPU = Debug|Any CPU
                	EndGlobalSection
                	GlobalSection(ProjectConfigurationPlatforms) = postSolution
                		{509968EA-344C-486F-BD35-FE47551787C6}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                		{509968EA-344C-486F-BD35-FE47551787C6}.Debug|Any CPU.Build.0 = Debug|Any CPU
                		{3B526C75-D7E2-4879-81D9-4237CC3D05E5}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                		{3B526C75-D7E2-4879-81D9-4237CC3D05E5}.Debug|Any CPU.Build.0 = Debug|Any CPU
                	EndGlobalSection
                EndGlobal
                """;
            await File.WriteAllTextAsync(slnPath, sln);

            var builder = new AnalysisModelBuilder([new MsBuildAnalysisProvider([slnPath])]);

            var model = await builder.BuildAsync(tempDir.FullName);

            var diagnostic = Assert.Single(model.Diagnostics, d => d.Id == "MSBUILD-WORKSPACE");
            Assert.Contains("Missing.csproj", diagnostic.Message);
            var solution = Assert.Single(model.Solutions);
            Assert.Single(solution.Projects);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }
}
