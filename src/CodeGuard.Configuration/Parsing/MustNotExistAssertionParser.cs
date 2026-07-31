using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotExistAssertionParser(SelectorParserRegistry selectorParsers) : IAssertionParser
{
    public string Kind => "must_not_exist";

    public IAssertion Parse(JsonObject parameters) => new MustNotExistAssertion(
        parameters["selector"]?.AsObject() ?? throw new RuleParsingException("'must_not_exist' requires a nested 'selector'."),
        selectorParsers.Parse);
}
