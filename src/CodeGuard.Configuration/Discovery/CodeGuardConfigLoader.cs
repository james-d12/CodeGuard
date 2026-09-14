using YamlDotNet.Core;
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

            return Deserialize(explicitConfigPath);
        }

        var configPath = ResolveConfigFilePath(repoRoot, null);
        if (!File.Exists(configPath))
        {
            return DefaultConfig;
        }

        return Deserialize(configPath);
    }

    private static CodeGuardConfig Deserialize(string configPath)
    {
        try
        {
            return Deserializer.Deserialize<CodeGuardConfig>(File.ReadAllText(configPath));
        }
        catch (YamlException ex)
        {
            // Expected, user-fixable condition (broken YAML in a hand-edited config file), not a
            // pipeline bug - wrapped as InvalidOperationException with the file path baked in so
            // CliRepositoryContext.TryResolve can turn it into a clean, actionable CLI message
            // instead of letting the raw YamlException (line/column blob, no file context) surface.
            throw new InvalidOperationException($"Config file '{configPath}' could not be parsed as YAML: {ex.Message}", ex);
        }
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
            Source = ["CodeGuard"],
            Tests = ["tests"]
        }
    };
}
