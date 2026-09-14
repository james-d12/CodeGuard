using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CodeGuard.Configuration.GlobalConfig;

public static class GlobalSettingsStore
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithEnumNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithEnumNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public static GlobalSettings? Load(string settingsFilePath)
    {
        if (!File.Exists(settingsFilePath))
        {
            return null;
        }

        try
        {
            return Deserializer.Deserialize<GlobalSettings>(File.ReadAllText(settingsFilePath));
        }
        catch (YamlException ex)
        {
            // Expected, user-fixable condition, not a pipeline bug - wrapped as
            // InvalidOperationException (with the file path and a fix suggestion baked in) so
            // CliRepositoryContext.TryResolve can turn it into a clean, actionable CLI message.
            throw new InvalidOperationException(
                $"Global settings file '{settingsFilePath}' (from a previous 'codeguard setup' run) could " +
                $"not be parsed as YAML: {ex.Message}. Run 'codeguard setup' again to regenerate it.", ex);
        }
    }

    public static void Save(string settingsFilePath, GlobalSettings settings)
    {
        var directory = Path.GetDirectoryName(settingsFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(settingsFilePath, Serializer.Serialize(settings));
    }
}
