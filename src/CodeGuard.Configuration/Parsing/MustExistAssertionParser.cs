using CodeGuard.Configuration.Capabilities;
using CodeGuard.Configuration.Validation;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;
using System.Text.Json.Nodes;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustExistAssertionParser(SelectorParserRegistry selectorParsers) : IAssertionParser
{
    public string Kind => "must_exist";

    public CapabilityDescriptor Descriptor => new(
        "must_exist",
        "At least one thing matching the nested selector must exist.",
        [
            new ParameterDescriptor("selector", ParameterType.Selector, true, "Nested target-style selector; any registered selector kind.")
        ]);

    public IAssertion Parse(JsonObject parameters) => new MustExistAssertion(
        parameters["selector"]?.AsObject() ?? throw new RuleParsingException(
                "'must_exist' requires a nested 'selector'.",
                RuleErrorCodes.InvalidParameter, "/selector"),
        selectorParsers.Parse);
}
