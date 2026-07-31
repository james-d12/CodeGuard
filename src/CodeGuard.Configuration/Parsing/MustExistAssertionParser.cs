using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustExistAssertionParser(SelectorParserRegistry selectorParsers) : IAssertionParser
{
    public string Kind => "must_exist";

    public IAssertion Parse(JsonObject parameters) => new MustExistAssertion(
        parameters["selector"]?.AsObject() ?? throw new RuleParsingException("'must_exist' requires a nested 'selector'."),
        selectorParsers.Parse);
}
