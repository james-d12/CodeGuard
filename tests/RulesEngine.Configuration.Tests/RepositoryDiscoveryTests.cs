using RulesEngine.Configuration.Discovery;

namespace RulesEngine.Configuration.Tests;

public class RepositoryDiscoveryTests : IDisposable
{
    private readonly string _repoRoot = Directory.CreateTempSubdirectory("rulesengine-discovery-").FullName;

    [Fact]
    public void Resolve_ReturnsOnlyPathsThatExist()
    {
        Directory.CreateDirectory(Path.Combine(_repoRoot, "rules"));
        Directory.CreateDirectory(Path.Combine(_repoRoot, "src"));
        // Intentionally do not create ".github/skills" or ".github/agents".

        var config = new RulesEngineConfig
        {
            Repository = new RepositoryConfig
            {
                Rules = ["rules"],
                Skills = [".github/skills"],
                Agents = [".github/agents"],
                Source = ["src"]
            }
        };

        var layout = new RepositoryDiscovery().Resolve(_repoRoot, config);

        Assert.Single(layout.RulesPaths);
        Assert.Equal(Path.GetFullPath(Path.Combine(_repoRoot, "rules")), layout.RulesPaths[0]);
        Assert.Single(layout.SourcePaths);
        Assert.Empty(layout.SkillsPaths);
        Assert.Empty(layout.AgentsPaths);
        Assert.Empty(layout.StandardsPaths);
        Assert.Empty(layout.TestsPaths);
    }

    [Fact]
    public void Resolve_ReturnsEmptyLayout_WhenConfigHasNoPaths()
    {
        var layout = new RepositoryDiscovery().Resolve(_repoRoot, new RulesEngineConfig());

        Assert.Empty(layout.RulesPaths);
        Assert.Empty(layout.StandardsPaths);
        Assert.Empty(layout.SkillsPaths);
        Assert.Empty(layout.AgentsPaths);
        Assert.Empty(layout.SourcePaths);
        Assert.Empty(layout.TestsPaths);
    }

    [Fact]
    public void Resolve_ResolvesMultipleConfiguredPathsForOneCategory()
    {
        Directory.CreateDirectory(Path.Combine(_repoRoot, "rules-a"));
        Directory.CreateDirectory(Path.Combine(_repoRoot, "rules-b"));

        var config = new RulesEngineConfig
        {
            Repository = new RepositoryConfig { Rules = ["rules-a", "rules-b", "rules-missing"] }
        };

        var layout = new RepositoryDiscovery().Resolve(_repoRoot, config);

        Assert.Equal(2, layout.RulesPaths.Count);
    }

    public void Dispose() => Directory.Delete(_repoRoot, recursive: true);
}
