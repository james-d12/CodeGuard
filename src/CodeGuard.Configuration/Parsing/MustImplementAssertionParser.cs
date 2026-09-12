using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustImplementAssertionParser : IAssertionParser
{
    public string Kind => "must_implement";

    public CapabilityDescriptor Descriptor => new(
        "must_implement",
        "Type must implement a matching interface.",
        [
            ParameterDescriptor.RequiredGlob("interface", "Interface name.")
        ])
    { AppliesTo = [CandidateKind.Type] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustImplementAssertion(parameters.GetRequiredString("interface"));
}
