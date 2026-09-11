using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Configuration.Validation;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public sealed class AnalyzerParserRegistry(IEnumerable<IAnalyzerParser> parsers)
{
    private readonly Dictionary<string, IAnalyzerParser> _byKind = parsers.ToDictionary(p => p.Kind);

    public IReadOnlyCollection<string> Kinds => _byKind.Keys;

    /// <summary>Every registered analyzer's declared capability, ordered by kind.</summary>
    public IReadOnlyList<CapabilityDescriptor> Descriptors =>
        field ??= _byKind.Values.Select(p => p.Descriptor).OrderBy(d => d.Kind, StringComparer.Ordinal).ToList();

    public ICustomAnalyzer Parse(JsonObject node)
    {
        var kind = node.GetRequiredString("kind");
        if (!_byKind.TryGetValue(kind, out var parser))
        {
            throw new RuleParsingException(
                $"Unknown analyzer kind '{kind}'.{KindSuggestion.For(kind, _byKind.Keys)}",
                RuleErrorCodes.UnknownAnalyzerKind);
        }

        return parser.Parse(node);
    }
}
