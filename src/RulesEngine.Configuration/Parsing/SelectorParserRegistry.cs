using System.Text.Json.Nodes;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Configuration.Parsing;

public sealed class SelectorParserRegistry(IEnumerable<ISelectorParser> parsers)
{
    private readonly Dictionary<string, ISelectorParser> _byKind = parsers.ToDictionary(p => p.Kind);

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
