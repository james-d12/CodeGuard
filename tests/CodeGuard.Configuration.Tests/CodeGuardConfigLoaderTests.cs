using CodeGuard.Configuration.Discovery;

namespace CodeGuard.Configuration.Tests;

public class CodeGuardConfigLoaderTests : IDisposable
{
    private readonly string _repoRoot = Directory.CreateTempSubdirectory("codeguard-config-").FullName;

    [Fact]
    public void LoadOrDefault_ParsesConfigFile_WhenPresent()
    {
        var rulesEngineDir = Directory.CreateDirectory(Path.Combine(_repoRoot, ".codeguard"));
        File.WriteAllText(Path.Combine(rulesEngineDir.FullName, "config.yml"), """
            repository:
              rules:
                - "custom-rules"
              skills:
                - ".github/skills"
              source:
                - "lib"
              tests:
                - "spec"
            """);

        var config = CodeGuardConfigLoader.LoadOrDefault(_repoRoot);

        Assert.Equal(["custom-rules"], config.Repository.Rules);
        Assert.Equal([".github/skills"], config.Repository.Skills);
        Assert.Equal(["lib"], config.Repository.Source);
        Assert.Equal(["spec"], config.Repository.Tests);
        Assert.Empty(config.Repository.Agents);
    }

    [Fact]
    public void LoadOrDefault_ReturnsDefaultLayout_WhenNoConfigFileExists()
    {
        var config = CodeGuardConfigLoader.LoadOrDefault(_repoRoot);

        Assert.Equal(["rules"], config.Repository.Rules);
        Assert.NotEmpty(config.Repository.Source);
    }

    [Fact]
    public void LoadOrDefault_UsesExplicitConfigPath_WhenProvided()
    {
        var explicitConfigPath = Path.Combine(_repoRoot, "custom-config.yml");
        File.WriteAllText(explicitConfigPath, """
            repository:
              rules:
                - "elsewhere-rules"
            """);

        var config = CodeGuardConfigLoader.LoadOrDefault(_repoRoot, explicitConfigPath);

        Assert.Equal(["elsewhere-rules"], config.Repository.Rules);
    }

    [Fact]
    public void LoadOrDefault_Throws_WhenExplicitConfigPathDoesNotExist()
    {
        var missingPath = Path.Combine(_repoRoot, "does-not-exist.yml");

        Assert.Throws<FileNotFoundException>(() => CodeGuardConfigLoader.LoadOrDefault(_repoRoot, missingPath));
    }

    public void Dispose() => Directory.Delete(_repoRoot, recursive: true);
}
