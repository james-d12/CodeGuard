using YamlDotNet.RepresentationModel;

namespace RulesEngine.Evaluation.Analyzers;

internal static class YamlFieldPath
{
    public static string? Resolve(YamlNode root, string dottedPath)
    {
        YamlNode? current = root;
        foreach (var segment in dottedPath.Split('.'))
        {
            if (current is not YamlMappingNode mapping)
            {
                return null;
            }

            current = mapping.Children
                .FirstOrDefault(entry => entry.Key is YamlScalarNode key && key.Value == segment)
                .Value;

            if (current is null)
            {
                return null;
            }
        }

        return current is YamlScalarNode scalar ? scalar.Value : null;
    }
}
