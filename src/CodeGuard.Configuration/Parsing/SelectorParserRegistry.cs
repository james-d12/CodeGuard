using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class SelectorParserRegistry(IEnumerable<ISelectorParser> parsers)
{
    // Materialized once: `parsers` is an IEnumerable, and both the lookup and the descriptor list
    // need it, so enumerating it twice would break on any lazy sequence.
    private readonly Dictionary<string, ISelectorParser> _byKind = parsers.ToDictionary(p => p.Kind);

    public IReadOnlyCollection<string> Kinds => _byKind.Keys;

    /// <summary>Every registered selector's declared capability, ordered by kind.</summary>
    public IReadOnlyList<CapabilityDescriptor> Descriptors =>
        field ??= _byKind.Values.Select(p => p.Descriptor).OrderBy(d => d.Kind, StringComparer.Ordinal).ToList();

    public ITargetSelector Parse(JsonObject node)
    {
        var kind = node.GetRequiredString("kind");
        if (!_byKind.TryGetValue(kind, out var parser))
        {
            throw new RuleParsingException($"Unknown target selector kind '{kind}'.");
        }

        return parser.Parse(node);
    }
}
