using CodeGuard.Configuration.Capabilities;
using CodeGuard.Configuration.Validation;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;
using System.Text.Json.Nodes;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveCountAssertionParser(SelectorParserRegistry selectorParsers) : IAssertionParser
{
    public string Kind => "must_have_count";

    public CapabilityDescriptor Descriptor => new(
        "must_have_count",
        "The nested selector must match a given number of things. At least one of min, max or exactly is required.",
        [
            new ParameterDescriptor("selector", ParameterType.Selector, true, "Nested target-style selector; any registered selector kind."),
            ParameterDescriptor.OptionalInt("min", "Minimum match count, inclusive."),
            ParameterDescriptor.OptionalInt("max", "Maximum match count, inclusive."),
            ParameterDescriptor.OptionalInt("exactly", "Exact match count.")
        ]);

    public IAssertion Parse(JsonObject parameters)
    {
        var selector = parameters["selector"]?.AsObject()
            ?? throw new RuleParsingException(
                "'must_have_count' requires a nested 'selector'.",
                RuleErrorCodes.InvalidParameter, "/selector");
        var min = parameters.GetOptionalInt("min");
        var max = parameters.GetOptionalInt("max");
        var exactly = parameters.GetOptionalInt("exactly");

        if (min is null && max is null && exactly is null)
        {
            throw new RuleParsingException(
                "'must_have_count' requires at least one of 'min', 'max', or 'exactly'.",
                RuleErrorCodes.InvalidParameter);
        }

        return new MustHaveCountAssertion(selector, selectorParsers.Parse, min, max, exactly);
    }
}
