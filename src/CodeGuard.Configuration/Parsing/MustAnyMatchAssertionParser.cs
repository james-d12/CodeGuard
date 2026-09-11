using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustAnyMatchAssertionParser(
    SelectorParserRegistry selectorParsers,
    Func<JsonObject, IAssertion> assertionParser) : IAssertionParser
{
    public string Kind => "must_any_match";

    public CapabilityDescriptor Descriptor => new(
        "must_any_match",
        "At least one match of the nested selector must satisfy all the nested assertions.",
        [
            new ParameterDescriptor("selector", ParameterType.Selector, true, "Nested target-style selector; any registered selector kind."),
            new ParameterDescriptor("assertions", ParameterType.AssertionList, true, "Nested assertions run against every match of selector. Must be non-empty.")
        ]);

    public IAssertion Parse(JsonObject parameters) =>
        new MustAnyMatchAssertion(
            parameters["selector"]?.AsObject()
                ?? throw new RuleParsingException("'must_any_match' requires a nested 'selector'."),
            selectorParsers.Parse,
            MustAllMatchAssertionParser.ParseNestedAssertions(parameters, "must_any_match", assertionParser));
}
