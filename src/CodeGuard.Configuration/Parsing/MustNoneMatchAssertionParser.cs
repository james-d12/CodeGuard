using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNoneMatchAssertionParser(
    SelectorParserRegistry selectorParsers,
    Func<JsonObject, IAssertion> assertionParser) : IAssertionParser
{
    public string Kind => "must_none_match";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNoneMatchAssertion(
            parameters["selector"]?.AsObject()
                ?? throw new RuleParsingException("'must_none_match' requires a nested 'selector'."),
            selectorParsers.Parse,
            MustAllMatchAssertionParser.ParseNestedAssertions(parameters, "must_none_match", assertionParser));
}
