using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustAllMatchAssertionParser(
    SelectorParserRegistry selectorParsers,
    Func<JsonObject, IAssertion> assertionParser) : IAssertionParser
{
    public string Kind => "must_all_match";

    public IAssertion Parse(JsonObject parameters) =>
        new MustAllMatchAssertion(
            parameters["selector"]?.AsObject()
                ?? throw new RuleParsingException("'must_all_match' requires a nested 'selector'."),
            selectorParsers.Parse,
            ParseNestedAssertions(parameters, "must_all_match", assertionParser));

    internal static List<IAssertion> ParseNestedAssertions(JsonObject parameters, string kind, Func<JsonObject, IAssertion> assertionParser)
    {
        var assertionsNode = parameters["assertions"]?.AsArray()
            ?? throw new RuleParsingException($"'{kind}' requires a nested 'assertions' list.");
        if (assertionsNode.Count == 0)
        {
            throw new RuleParsingException($"'{kind}' requires at least one nested assertion.");
        }

        return assertionsNode.Select(node => assertionParser(node!.AsObject())).ToList();
    }
}
