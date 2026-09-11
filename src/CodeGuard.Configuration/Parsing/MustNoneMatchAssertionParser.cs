using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNoneMatchAssertionParser(
    SelectorParserRegistry selectorParsers,
    Func<JsonObject, IAssertion> assertionParser) : IAssertionParser
{
    public string Kind => "must_none_match";

    public CapabilityDescriptor Descriptor => new(
        "must_none_match",
        "No match of the nested selector may satisfy all the nested assertions.",
        [
            new ParameterDescriptor("selector", ParameterType.Selector, true, "Nested target-style selector; any registered selector kind."),
            new ParameterDescriptor("assertions", ParameterType.AssertionList, true, "Nested assertions run against every match of selector. Must be non-empty.")
        ]);

    public IAssertion Parse(JsonObject parameters) =>
        new MustNoneMatchAssertion(
            parameters["selector"]?.AsObject()
                ?? throw new RuleParsingException("'must_none_match' requires a nested 'selector'."),
            selectorParsers.Parse,
            MustAllMatchAssertionParser.ParseNestedAssertions(parameters, "must_none_match", assertionParser));
}
