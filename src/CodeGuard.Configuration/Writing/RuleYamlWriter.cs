using YamlDotNet.Serialization;

namespace CodeGuard.Configuration.Writing;

public static class RuleYamlWriter
{
    private static readonly ISerializer Serializer = new SerializerBuilder().Build();

    public static string Serialize(object document) => Serializer.Serialize(document);
}
