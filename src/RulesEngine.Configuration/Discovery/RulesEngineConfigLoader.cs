using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RulesEngine.Configuration.Discovery;

public static class RulesEngineConfigLoader
{
    private const string DefaultConfigRelativePath = ".rulesengine/config.yml";

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public static RulesEngineConfig LoadOrDefault(string repoRoot) => LoadOrDefault(repoRoot, explicitConfigPath: null);

    public static RulesEngineConfig LoadOrDefault(string repoRoot, string? explicitConfigPath)
    {
        if (explicitConfigPath is not null)
        {
            if (!File.Exists(explicitConfigPath))
            {
                throw new FileNotFoundException($"Config file '{explicitConfigPath}' was not found.", explicitConfigPath);
            }

            return Deserializer.Deserialize<RulesEngineConfig>(File.ReadAllText(explicitConfigPath));
        }

        var configPath = Path.Combine(repoRoot, DefaultConfigRelativePath);
        if (!File.Exists(configPath))
        {
            return DefaultConfig;
        }

        var yaml = File.ReadAllText(configPath);
        return Deserializer.Deserialize<RulesEngineConfig>(yaml);
    }

    private static RulesEngineConfig DefaultConfig { get; } = new()
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
