using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CodeGuard.Configuration.Discovery;

public static class CodeGuardConfigLoader
{
    private const string DefaultConfigRelativePath = ".codeguard/config.yml";

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public static CodeGuardConfig LoadOrDefault(string repoRoot) => LoadOrDefault(repoRoot, explicitConfigPath: null);

    public static CodeGuardConfig LoadOrDefault(string repoRoot, string? explicitConfigPath)
    {
        if (explicitConfigPath is not null)
        {
            if (!File.Exists(explicitConfigPath))
            {
                throw new FileNotFoundException($"Config file '{explicitConfigPath}' was not found.", explicitConfigPath);
            }

            return Deserializer.Deserialize<CodeGuardConfig>(File.ReadAllText(explicitConfigPath));
        }

        var configPath = ResolveConfigFilePath(repoRoot, null);
        if (!File.Exists(configPath))
        {
            return DefaultConfig;
        }

        var yaml = File.ReadAllText(configPath);
        return Deserializer.Deserialize<CodeGuardConfig>(yaml);
    }

    /// <summary>The config file path <see cref="LoadOrDefault(string,string?)"/> would read from, without loading it.</summary>
    public static string ResolveConfigFilePath(string repoRoot, string? explicitConfigPath) =>
        explicitConfigPath ?? Path.Combine(repoRoot, DefaultConfigRelativePath);

    private static CodeGuardConfig DefaultConfig { get; } = new()
    {
        Repository = new RepositoryConfig
        {
            Rules = ["rules"],
            Skills = [".github/skills"],
            Agents = [".github/agents"],
            Source = ["RuleEngine"],
            Tests = ["tests"]
        }
    };
}
