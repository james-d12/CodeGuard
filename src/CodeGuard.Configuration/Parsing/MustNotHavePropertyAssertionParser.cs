using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotHavePropertyAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_property";

    public CapabilityDescriptor Descriptor => new(
        "must_not_have_property",
        "Type must not declare a property with a matching name.",
        [
            ParameterDescriptor.RequiredGlob("name", "Property name that must not be present.")
        ])
    { AppliesTo = [CandidateKind.Type] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHavePropertyAssertion(parameters.GetRequiredString("name"));
}
