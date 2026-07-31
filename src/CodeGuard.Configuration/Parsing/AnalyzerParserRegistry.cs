using System.Text.Json.Nodes;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public sealed class AnalyzerParserRegistry(IEnumerable<IAnalyzerParser> parsers)
{
    private readonly Dictionary<string, IAnalyzerParser> _byKind = parsers.ToDictionary(p => p.Kind);

    public ICustomAnalyzer Parse(JsonObject node)
    {
        var kind = node.GetRequiredString("kind");
        if (!_byKind.TryGetValue(kind, out var parser))
        {
            throw new RuleParsingException($"Unknown analyzer kind '{kind}'.");
        }

        return parser.Parse(node);
    }
}
