using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveCountAssertionParser(SelectorParserRegistry selectorParsers) : IAssertionParser
{
    public string Kind => "must_have_count";

    public IAssertion Parse(JsonObject parameters)
    {
        var selector = parameters["selector"]?.AsObject()
            ?? throw new RuleParsingException("'must_have_count' requires a nested 'selector'.");
        var min = parameters.GetOptionalInt("min");
        var max = parameters.GetOptionalInt("max");
        var exactly = parameters.GetOptionalInt("exactly");

        if (min is null && max is null && exactly is null)
        {
            throw new RuleParsingException("'must_have_count' requires at least one of 'min', 'max', or 'exactly'.");
        }

        return new MustHaveCountAssertion(selector, selectorParsers.Parse, min, max, exactly);
    }
}
