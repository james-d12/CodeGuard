using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustAnyMatchAssertionParser(
    SelectorParserRegistry selectorParsers,
    Func<JsonObject, IAssertion> assertionParser) : IAssertionParser
{
    public string Kind => "must_any_match";

    public IAssertion Parse(JsonObject parameters) =>
        new MustAnyMatchAssertion(
            parameters["selector"]?.AsObject()
                ?? throw new RuleParsingException("'must_any_match' requires a nested 'selector'."),
            selectorParsers.Parse,
            MustAllMatchAssertionParser.ParseNestedAssertions(parameters, "must_any_match", assertionParser));
}
