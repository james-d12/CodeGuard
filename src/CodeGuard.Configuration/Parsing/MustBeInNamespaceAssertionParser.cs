using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustBeInNamespaceAssertionParser : IAssertionParser
{
    public string Kind => "must_be_in_namespace";

    public CapabilityDescriptor Descriptor => new(
        "must_be_in_namespace",
        "Type must be declared in a matching namespace.",
        [
            ParameterDescriptor.RequiredGlob("pattern", "Namespace the type must be in.")
        ])
    { AppliesTo = [CandidateKind.Type] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustBeInNamespaceAssertion(parameters.GetRequiredString("pattern"));
}
