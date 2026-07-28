using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RulesEngine.Configuration.GlobalConfig;

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

    public static GlobalSettings? Load(string settingsFilePath) =>
        File.Exists(settingsFilePath)
            ? Deserializer.Deserialize<GlobalSettings>(File.ReadAllText(settingsFilePath))
            : null;

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
