using CodeGuard.Configuration.Capabilities;
using CodeGuard.Configuration.Validation;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;
using System.Text.Json.Nodes;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotExistAssertionParser(SelectorParserRegistry selectorParsers) : IAssertionParser
{
    public string Kind => "must_not_exist";

    public CapabilityDescriptor Descriptor => new(
        "must_not_exist",
        "Nothing matching the nested selector may exist. The usual shape for a repository-wide prohibition.",
        [
            new ParameterDescriptor("selector", ParameterType.Selector, true, "Nested target-style selector; any registered selector kind.")
        ]);

    public IAssertion Parse(JsonObject parameters) => new MustNotExistAssertion(
        parameters["selector"]?.AsObject() ?? throw new RuleParsingException(
                "'must_not_exist' requires a nested 'selector'.",
                RuleErrorCodes.InvalidParameter, "/selector"),
        selectorParsers.Parse);
}
