using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustNotExistAssertionParser(SelectorParserRegistry selectorParsers) : IAssertionParser
{
    public string Kind => "must_not_exist";

    public IAssertion Parse(JsonObject parameters) => new MustNotExistAssertion(
        parameters["selector"]?.AsObject() ?? throw new RuleParsingException("'must_not_exist' requires a nested 'selector'."),
        selectorParsers.Parse);
}
