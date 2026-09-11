using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotHaveAttributeAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_attribute";

    public CapabilityDescriptor Descriptor => new(
        "must_not_have_attribute",
        "Candidate must not carry a matching attribute.",
        [
            ParameterDescriptor.RequiredGlob("type", "Attribute type that must be absent."),
            new ParameterDescriptor("argument", ParameterType.String, false, "Only fail when this argument is also present.")
        ])
    { AppliesTo = [CandidateKind.Type, CandidateKind.Method, CandidateKind.Property, CandidateKind.Constructor, CandidateKind.Field] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHaveAttributeAssertion(parameters.GetRequiredString("type"), parameters.GetOptionalString("argument"));
}
