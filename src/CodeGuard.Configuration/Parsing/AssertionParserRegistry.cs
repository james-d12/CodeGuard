using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Configuration.Validation;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class AssertionParserRegistry(IEnumerable<IAssertionParser> parsers)
{
    private readonly Dictionary<string, IAssertionParser> _byKind = parsers.ToDictionary(p => p.Kind);

    public IReadOnlyCollection<string> Kinds => _byKind.Keys;

    /// <summary>Every registered assertion's declared capability, ordered by kind.</summary>
    public IReadOnlyList<CapabilityDescriptor> Descriptors =>
        field ??= _byKind.Values.Select(p => p.Descriptor).OrderBy(d => d.Kind, StringComparer.Ordinal).ToList();

    public IAssertion Parse(JsonObject assertionEntry)
    {
        if (assertionEntry.Count != 1)
        {
            throw new RuleParsingException(
                "Each assertion must be a single-key mapping, e.g. 'must_inherit_from: { type: ... }'.");
        }

        var (kind, parametersNode) = assertionEntry.Single();
        if (!_byKind.TryGetValue(kind, out var parser))
        {
            throw new RuleParsingException(
                $"Unknown assertion kind '{kind}'.{KindSuggestion.For(kind, _byKind.Keys)}",
                RuleErrorCodes.UnknownAssertionKind);
        }

        var parameters = parametersNode?.AsObject() ?? new JsonObject();
        return parser.Parse(parameters);
    }
}
