namespace RulesEngine.Configuration.Parsing;

internal static class EnumParsing
{
    public static TEnum ParseSnakeCase<TEnum>(string value) where TEnum : struct, Enum
    {
        var pascalCase = string.Concat(value.Split('_').Select(CapitalizeFirst));
        if (!Enum.TryParse<TEnum>(pascalCase, ignoreCase: true, out var parsed))
        {
            throw new RuleParsingException($"'{value}' is not a valid {typeof(TEnum).Name}.");
        }

        return parsed;
    }

    private static string CapitalizeFirst(string value) =>
        value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];
}
