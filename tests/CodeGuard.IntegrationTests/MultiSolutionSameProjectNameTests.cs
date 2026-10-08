using System.Runtime.CompilerServices;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Analysis.Providers;
using CodeGuard.Analyzers.MSBuild;
using CodeGuard.Evaluation.Analyzers;

namespace CodeGuard.IntegrationTests;

/// <summary>
/// Regression fixture for a repo with two solutions that each contain a *different* project file
/// named <c>Contoso.Api.csproj</c>, declaring types with identical FullNames. (ProjectName, FullName)
/// is not unique there, so analyzers that join syntax facts back to types must key by project path:
/// keyed by name, <see cref="NoPureDelegationOverrideAnalyzer"/> threw on the duplicate dictionary key
/// and <see cref="ImmutableMutationAnalyzer"/> attributed one project's mutations to the other
/// project's record.
/// </summary>
public class MultiSolutionSameProjectNameTests
{
    [Fact]
    public async Task BuildAsync_KeepsBothSameNamedProjects_WithDistinctProjectPaths()
    {
        var model = await BuildAsync();

        var projects = model.Solutions.SelectMany(s => s.Projects).ToList();
        Assert.Equal(2, projects.Count);
        Assert.All(projects, p => Assert.Equal("Contoso.Api", p.Name));
        Assert.Equal(2, projects.Select(p => p.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(projects, p => Assert.All(p.Types, t => Assert.Equal(p.Path, t.ProjectPath)));
    }

    [Fact]
    public async Task NoPureDelegationOverrideAnalyzer_AttributesOverrideToTheCorrectProject()
    {
        var model = await BuildAsync();

        var violations = new NoPureDelegationOverrideAnalyzer("*RepositoryBase").Analyze(model).ToList();

        var violation = Assert.Single(violations);
        Assert.Contains(Path.Combine("Orders", "Contoso.Api"), violation.FilePath);
    }

    [Fact]
    public async Task ImmutableMutationAnalyzer_DoesNotAttributeMutationToSameNamedRecordInOtherProject()
    {
        var model = await BuildAsync();

        var violations = new ImmutableMutationAnalyzer("Contoso.Api.*").Analyze(model).ToList();

        Assert.Empty(violations);
    }

    private static async Task<RepositoryModel> BuildAsync([CallerFilePath] string sourceFilePath = "")
    {
        var root = Path.Combine(Path.GetDirectoryName(sourceFilePath)!, "Fixtures", "MultiSolutionSameProjectName");
        var solutionPaths = new[]
        {
            Path.Combine(root, "Orders", "Orders.sln"),
            Path.Combine(root, "Billing", "Billing.sln")
        };

        var builder = new AnalysisModelBuilder([new MsBuildAnalysisProvider(solutionPaths)]);
        return await builder.BuildAsync(root);
    }
}
