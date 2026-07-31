using CodeGuard.Cli.Support;
using CodeGuard.Configuration.Discovery;
using CodeGuard.Configuration.GlobalConfig;

namespace CodeGuard.Cli.Tests;

/// <summary>
/// Covers the rules-path resolution precedence documented on <see cref="CliRepositoryContext.Resolve"/>
/// and docs/SETUP_COMMAND_PLAN.md. Every test passes an explicit, temp-directory `globalSettingsRoot`
/// so tier 4 (a prior `setup` run) never reads or writes the machine's real app-data directory.
/// </summary>
public class CliRepositoryContextTests : IDisposable
{
    private readonly string _repoRoot = Directory.CreateTempSubdirectory("codeguard-clictx-repo-").FullName;
    private readonly string _globalSettingsRoot = Directory.CreateTempSubdirectory("codeguard-clictx-global-").FullName;
    private readonly List<string> _tempDirs = [];

    [Fact]
    public void Resolve_UsesRulesSourceOverride_AboveEverythingElse()
    {
        var repoConfiguredRules = CreateRulesDir("repo-configured");
        WriteRepoConfig(repoConfiguredRules);
        SaveGlobalSettings(CreateRulesDir("global-configured"));
        var overrideRules = CreateRulesDir("override");

        var context = CliRepositoryContext.Resolve(
            _repoRoot, configPath: null, rulesSource: overrideRules, branch: null, globalSettingsRoot: _globalSettingsRoot);

        Assert.Equal([overrideRules], context.Layout.RulesPaths);
    }

    [Fact]
    public void Resolve_PrefersExplicitConfigPath_OverRepoConfigFile()
    {
        WriteRepoConfig(CreateRulesDir("repo-configured"));
        var explicitConfigRules = CreateRulesDir("explicit-config");
        var explicitConfigPath = Path.Combine(_repoRoot, "explicit-config.yml");
        File.WriteAllText(explicitConfigPath, $"""
            repository:
              rules:
                - "{explicitConfigRules.Replace("\\", "/")}"
            """);

        var context = CliRepositoryContext.Resolve(
            _repoRoot, configPath: explicitConfigPath, globalSettingsRoot: _globalSettingsRoot);

        Assert.Equal([explicitConfigRules], context.Layout.RulesPaths);
    }

    [Fact]
    public void Resolve_PrefersRepoConfigFile_OverGlobalSettings()
    {
        var repoConfiguredRules = CreateRulesDir("repo-configured");
        WriteRepoConfig(repoConfiguredRules);
        SaveGlobalSettings(CreateRulesDir("global-configured"));

        var context = CliRepositoryContext.Resolve(_repoRoot, configPath: null, globalSettingsRoot: _globalSettingsRoot);

        Assert.Equal([repoConfiguredRules], context.Layout.RulesPaths);
    }

    [Fact]
    public void Resolve_FallsBackToGlobalSettings_WhenRepoHasNoConfig()
    {
        var globalConfiguredRules = CreateRulesDir("global-configured");
        SaveGlobalSettings(globalConfiguredRules);

        var context = CliRepositoryContext.Resolve(_repoRoot, configPath: null, globalSettingsRoot: _globalSettingsRoot);

        Assert.Equal([globalConfiguredRules], context.Layout.RulesPaths);
    }

    [Fact]
    public void Resolve_ReturnsNoRulesPaths_WhenNothingIsConfiguredAnywhere()
    {
        var context = CliRepositoryContext.Resolve(_repoRoot, configPath: null, globalSettingsRoot: _globalSettingsRoot);

        Assert.Empty(context.Layout.RulesPaths);
    }

    [Fact]
    public void TryRequireRulesConfigured_ReturnsFalseAndWritesHint_WhenNoRulesPaths()
    {
        var context = new CliRepositoryContext
        {
            RepoRoot = _repoRoot,
            Layout = new RepositoryLayout([], [], [], [], [], [])
        };
        var writer = new StringWriter();

        var result = context.TryRequireRulesConfigured(writer);

        Assert.False(result);
        Assert.Contains("No rules directory is configured.", writer.ToString());
        Assert.Contains("codeguard setup", writer.ToString());
        Assert.Contains("--rules-source", writer.ToString());
    }

    [Fact]
    public void TryRequireRulesConfigured_ReturnsTrueAndWritesNothing_WhenRulesPathsPresent()
    {
        var context = new CliRepositoryContext
        {
            RepoRoot = _repoRoot,
            Layout = new RepositoryLayout([], [CreateRulesDir("configured")], [], [], [], [])
        };
        var writer = new StringWriter();

        var result = context.TryRequireRulesConfigured(writer);

        Assert.True(result);
        Assert.Equal("", writer.ToString());
    }

    private string CreateRulesDir(string name)
    {
        var dir = Directory.CreateTempSubdirectory($"codeguard-clictx-{name}-").FullName;
        _tempDirs.Add(dir);
        return dir;
    }

    private void WriteRepoConfig(string rulesDir)
    {
        var configDir = Directory.CreateDirectory(Path.Combine(_repoRoot, ".codeguard"));
        File.WriteAllText(Path.Combine(configDir.FullName, "config.yml"), $"""
            repository:
              rules:
                - "{rulesDir.Replace("\\", "/")}"
            """);
    }

    private void SaveGlobalSettings(string rulesDir) =>
        GlobalSettingsStore.Save(
            GlobalSettingsPaths.SettingsFilePath(_globalSettingsRoot),
            new GlobalSettings { Kind = RuleSourceKind.Directory, Location = rulesDir });

    public void Dispose()
    {
        Directory.Delete(_repoRoot, recursive: true);
        Directory.Delete(_globalSettingsRoot, recursive: true);
        foreach (var dir in _tempDirs)
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
