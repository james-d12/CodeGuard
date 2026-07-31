using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Analyzers;

namespace CodeGuard.Evaluation.Tests.Analyzers;

public sealed class ProjectConventionAnalyzerTests : IDisposable
{
    private readonly string _projectPath = Path.Combine(Path.GetTempPath(), $"rulesengine-projectconvention-{Guid.NewGuid():N}.csproj");

    private ProjectModel WriteProject(string name, string csprojXml)
    {
        File.WriteAllText(_projectPath, csprojXml);
        return new ProjectModel(name, _projectPath, "net10.0", "Microsoft.NET.Sdk", [], [], new Dictionary<string, string>(), []);
    }

    private const string WithScriptsContent = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup>
            <Content Include="Scripts\**\*.sql" />
          </ItemGroup>
        </Project>
        """;

    private const string WithoutScriptsContent = """
        <Project Sdk="Microsoft.NET.Sdk">
        </Project>
        """;

    private static CallSiteModel BootstrapCallSite(string projectName) => new(
        Kind: CallSiteKind.Invocation, InvokedMember: "DeployChanges", TargetTypeName: null,
        ContainingMethod: "Main", ContainingType: "Program", ProjectName: projectName, Arguments: [],
        FilePath: "Program.cs", Line: 10, Column: 1);

    [Fact]
    public void Analyze_DoesNotFlag_ProjectWithRequiredCallSiteAndContentEntry()
    {
        var project = WriteProject("Contoso.Reporting", WithScriptsContent);
        var model = TestModels.RepositoryWithFacts(
            projects: [project],
            callSites: [BootstrapCallSite("Contoso.Reporting")]);
        var analyzer = new ProjectConventionAnalyzer("*.Reporting*");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_Flags_ProjectMissingRequiredCallSite()
    {
        var project = WriteProject("Contoso.Reporting", WithScriptsContent);
        var model = TestModels.RepositoryWithFacts(projects: [project]);
        var analyzer = new ProjectConventionAnalyzer("*.Reporting*");

        var violations = analyzer.Analyze(model).ToList();

        var violation = Assert.Single(violations);
        Assert.Contains("call-site", violation.Message);
        Assert.DoesNotContain("Content Include", violation.Message);
    }

    [Fact]
    public void Analyze_Flags_ProjectMissingRequiredContentEntry()
    {
        var project = WriteProject("Contoso.Reporting", WithoutScriptsContent);
        var model = TestModels.RepositoryWithFacts(
            projects: [project],
            callSites: [BootstrapCallSite("Contoso.Reporting")]);
        var analyzer = new ProjectConventionAnalyzer("*.Reporting*");

        var violations = analyzer.Analyze(model).ToList();

        var violation = Assert.Single(violations);
        Assert.Contains("Content Include", violation.Message);
        Assert.DoesNotContain("call-site matching", violation.Message);
    }

    [Fact]
    public void Analyze_Flags_ProjectMissingBothCallSiteAndContentEntry()
    {
        var project = WriteProject("Contoso.Reporting", WithoutScriptsContent);
        var model = TestModels.RepositoryWithFacts(projects: [project]);
        var analyzer = new ProjectConventionAnalyzer("*.Reporting*");

        var violations = analyzer.Analyze(model).ToList();

        var violation = Assert.Single(violations);
        Assert.Contains("a call-site matching '*DeployChanges*' and a <Content Include=> entry referencing 'Scripts'", violation.Message);
    }

    [Fact]
    public void Analyze_Flags_WhenMatchingCallSiteBelongsToADifferentProject()
    {
        var project = WriteProject("Contoso.Reporting", WithScriptsContent);
        var model = TestModels.RepositoryWithFacts(
            projects: [project],
            callSites: [BootstrapCallSite("Contoso.OtherProject")]);
        var analyzer = new ProjectConventionAnalyzer("*.Reporting*");

        var violations = analyzer.Analyze(model).ToList();

        var violation = Assert.Single(violations);
        Assert.Contains("call-site", violation.Message);
    }

    [Fact]
    public void Analyze_DoesNotFlag_WhenContentElementHasNoIncludeAttribute()
    {
        var project = WriteProject("Contoso.Reporting", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <Content Update="appsettings.json" />
                <Content Include="Scripts\**\*.sql" />
              </ItemGroup>
            </Project>
            """);
        var model = TestModels.RepositoryWithFacts(
            projects: [project],
            callSites: [BootstrapCallSite("Contoso.Reporting")]);
        var analyzer = new ProjectConventionAnalyzer("*.Reporting*");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_DoesNotFlag_ProjectNotMatchingConfiguredPattern()
    {
        var project = WriteProject("Contoso.Domain", WithoutScriptsContent);
        var model = TestModels.RepositoryWithFacts(projects: [project]);
        var analyzer = new ProjectConventionAnalyzer("*.Reporting*");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_SupportsDifferentConventions_NotJustDbUp()
    {
        var project = WriteProject("Contoso.AppHost", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <Content Include="Resources\**\*.json" />
              </ItemGroup>
            </Project>
            """);
        var callSite = new CallSiteModel(
            Kind: CallSiteKind.Invocation, InvokedMember: "Run", TargetTypeName: null,
            ContainingMethod: "Main", ContainingType: "Program", ProjectName: "Contoso.AppHost", Arguments: [],
            FilePath: "Program.cs", Line: 10, Column: 1);
        var model = TestModels.RepositoryWithFacts(projects: [project], callSites: [callSite]);
        var analyzer = new ProjectConventionAnalyzer("*.AppHost", requiredCallPattern: "Run", requiredContentFolder: "Resources");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Name_IsProjectConvention()
    {
        var analyzer = new ProjectConventionAnalyzer("*.Reporting*");

        Assert.Equal("project-convention", analyzer.Name);
    }

    public void Dispose()
    {
        if (File.Exists(_projectPath))
        {
            File.Delete(_projectPath);
        }
    }
}
