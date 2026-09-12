using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotImplementAssertionParser : IAssertionParser
{
    public string Kind => "must_not_implement";

    public CapabilityDescriptor Descriptor => new(
        "must_not_implement",
        "Type must not implement a matching interface.",
        [
            ParameterDescriptor.RequiredGlob("interface", "Interface that must not be implemented.")
        ])
    { AppliesTo = [CandidateKind.Type] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotImplementAssertion(parameters.GetRequiredString("interface"));
}
