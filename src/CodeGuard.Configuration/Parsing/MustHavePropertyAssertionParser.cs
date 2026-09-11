using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHavePropertyAssertionParser : IAssertionParser
{
    public string Kind => "must_have_property";

    public CapabilityDescriptor Descriptor => new(
        "must_have_property",
        "Type must declare a property with a matching name.",
        [
            ParameterDescriptor.RequiredGlob("name", "Property name.")
        ])
    { AppliesTo = [CandidateKind.Type] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustHavePropertyAssertion(parameters.GetRequiredString("name"));
}
