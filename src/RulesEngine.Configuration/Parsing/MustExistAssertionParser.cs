using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustExistAssertionParser(SelectorParserRegistry selectorParsers) : IAssertionParser
{
    public string Kind => "must_exist";

    public IAssertion Parse(JsonObject parameters) => new MustExistAssertion(
        parameters["selector"]?.AsObject() ?? throw new RuleParsingException("'must_exist' requires a nested 'selector'."),
        selectorParsers.Parse);
}
