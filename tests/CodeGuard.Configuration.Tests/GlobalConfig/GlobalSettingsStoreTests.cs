using CodeGuard.Configuration.GlobalConfig;
using YamlDotNet.Core;

namespace CodeGuard.Configuration.Tests.GlobalConfig;

public class GlobalSettingsStoreTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("rulesengine-globalsettings-").FullName;

    [Fact]
    public void Load_ReturnsNull_WhenNoSettingsFileExists()
    {
        var settingsFilePath = Path.Combine(_root, "settings.yml");

        Assert.Null(GlobalSettingsStore.Load(settingsFilePath));
    }

    [Fact]
    public void SaveThenLoad_RoundTripsGitSource()
    {
        var settingsFilePath = Path.Combine(_root, "nested", "settings.yml");
        var saved = new GlobalSettings
        {
            Kind = RuleSourceKind.Git,
            Location = "https://github.com/org/rules-repo.git",
            Branch = "main"
        };

        GlobalSettingsStore.Save(settingsFilePath, saved);
        var loaded = GlobalSettingsStore.Load(settingsFilePath);

        Assert.NotNull(loaded);
        Assert.Equal(RuleSourceKind.Git, loaded.Kind);
        Assert.Equal("https://github.com/org/rules-repo.git", loaded.Location);
        Assert.Equal("main", loaded.Branch);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsDirectorySource_WithNoBranch()
    {
        var settingsFilePath = Path.Combine(_root, "settings.yml");
        var saved = new GlobalSettings { Kind = RuleSourceKind.Directory, Location = "/home/jamie/rules-checkout" };

        GlobalSettingsStore.Save(settingsFilePath, saved);
        var loaded = GlobalSettingsStore.Load(settingsFilePath);

        Assert.NotNull(loaded);
        Assert.Equal(RuleSourceKind.Directory, loaded.Kind);
        Assert.Equal("/home/jamie/rules-checkout", loaded.Location);
        Assert.Null(loaded.Branch);
    }

    [Fact]
    public void Load_ThrowsInvalidOperationException_WhenSettingsFileIsMalformedYaml()
    {
        var settingsFilePath = Path.Combine(_root, "settings.yml");
        File.WriteAllText(settingsFilePath, "[this, is, not, a, map]");

        var ex = Assert.Throws<InvalidOperationException>(() => GlobalSettingsStore.Load(settingsFilePath));

        Assert.Contains(settingsFilePath, ex.Message);
        Assert.Contains("codeguard setup", ex.Message);
        Assert.IsType<YamlException>(ex.InnerException);
    }

    [Fact]
    public void Save_CreatesParentDirectories_WhenTheyDoNotExist()
    {
        var settingsFilePath = Path.Combine(_root, "a", "b", "c", "settings.yml");

        GlobalSettingsStore.Save(settingsFilePath, new GlobalSettings { Kind = RuleSourceKind.Directory, Location = "/x" });

        Assert.True(File.Exists(settingsFilePath));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
