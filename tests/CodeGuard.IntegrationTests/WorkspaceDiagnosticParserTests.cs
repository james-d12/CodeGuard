using CodeGuard.Analyzers.MSBuild;

namespace CodeGuard.IntegrationTests;

/// <summary>
/// Covers <see cref="WorkspaceDiagnosticParser"/> against the exact message shapes MSBuildWorkspace
/// produces for a project-load failure - captured verbatim from a real run against a solution whose
/// obj/ still carried a Windows-only NuGet fallback folder after being copied to Linux.
/// </summary>
public class WorkspaceDiagnosticParserTests
{
    [Fact]
    public void Parse_ProjectLoadFailureWithExceptionStackTrace_ExtractsPathAndTrimsStackFrames()
    {
        const string raw = "Msbuild failed when processing the file '/repo/src/Foo/Foo.csproj' with message: " +
            "The \"ResolvePackageAssets\" task failed unexpectedly. NuGet.Packaging.Core.PackagingException: " +
            "Unable to find fallback package folder 'C:\\Program Files (x86)\\Microsoft Visual Studio\\Shared\\NuGetPackages'.    " +
            "at NuGet.Packaging.FallbackPackagePathResolver..ctor(String userPackageFolder, IEnumerable`1 fallbackPackageFolders)    " +
            "at Microsoft.NET.Build.Tasks.NuGetPackageResolver.CreateResolver(IEnumerable`1 packageFolders)";

        var (projectPath, message) = WorkspaceDiagnosticParser.Parse(raw);

        Assert.Equal("/repo/src/Foo/Foo.csproj", projectPath);
        Assert.Equal(
            "The \"ResolvePackageAssets\" task failed unexpectedly. NuGet.Packaging.Core.PackagingException: " +
            "Unable to find fallback package folder 'C:\\Program Files (x86)\\Microsoft Visual Studio\\Shared\\NuGetPackages'.",
            message);
        Assert.DoesNotContain("NuGet.Packaging.FallbackPackagePathResolver", message);
    }

    [Fact]
    public void Parse_ProjectLoadFailureWithoutStackTrace_ExtractsPathAndKeepsMessageIntact()
    {
        const string raw = "Msbuild failed when processing the file '/repo/src/Foo.AppHost/Foo.AppHost.csproj' with message: " +
            "Foo.AppHost is an Aspire AppHost project but necessary dependencies aren't present. " +
            "Are you missing an Aspire.Hosting.AppHost PackageReference?";

        var (projectPath, message) = WorkspaceDiagnosticParser.Parse(raw);

        Assert.Equal("/repo/src/Foo.AppHost/Foo.AppHost.csproj", projectPath);
        Assert.Equal(
            "Foo.AppHost is an Aspire AppHost project but necessary dependencies aren't present. " +
            "Are you missing an Aspire.Hosting.AppHost PackageReference?",
            message);
    }

    [Fact]
    public void Parse_MessageWithoutProjectPath_ReturnsNullPathAndTrimmedMessage()
    {
        const string raw = "Some other workspace diagnostic with no project path in it.";

        var (projectPath, message) = WorkspaceDiagnosticParser.Parse(raw);

        Assert.Null(projectPath);
        Assert.Equal(raw, message);
    }
}
