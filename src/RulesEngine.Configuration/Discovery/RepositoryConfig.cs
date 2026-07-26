namespace RulesEngine.Configuration.Discovery;

public sealed class RulesEngineConfig
{
    public RepositoryConfig Repository { get; init; } = new();
}

public sealed class RepositoryConfig
{
    // YamlDotNet's default object deserializer needs a concrete mutable collection type
    // (List<T>) to populate - it can't target IReadOnlyList<T> directly.
    public List<string> Standards { get; init; } = [];
    public List<string> Rules { get; init; } = [];
    public List<string> Skills { get; init; } = [];
    public List<string> Agents { get; init; } = [];
    public List<string> Source { get; init; } = [];
    public List<string> Tests { get; init; } = [];
}
