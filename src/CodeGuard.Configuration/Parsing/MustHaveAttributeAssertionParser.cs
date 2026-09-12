using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveAttributeAssertionParser : IAssertionParser
{
    public string Kind => "must_have_attribute";

    public CapabilityDescriptor Descriptor => new(
        "must_have_attribute",
        "Candidate must carry a matching attribute.",
        [
            ParameterDescriptor.RequiredGlob("type", "Attribute type name."),
            new ParameterDescriptor("argument", ParameterType.String, false, "Attribute argument that must also be present.")
        ])
    { AppliesTo = [CandidateKind.Type, CandidateKind.Method, CandidateKind.Property, CandidateKind.Constructor, CandidateKind.Field] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveAttributeAssertion(parameters.GetRequiredString("type"), parameters.GetOptionalString("argument"));
}
