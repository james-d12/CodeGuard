using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotBeInNamespaceAssertionParser : IAssertionParser
{
    public string Kind => "must_not_be_in_namespace";

    public CapabilityDescriptor Descriptor => new(
        "must_not_be_in_namespace",
        "Type must not be declared in a matching namespace.",
        [
            ParameterDescriptor.RequiredGlob("pattern", "Namespace the type must not be in.")
        ])
    { AppliesTo = [CandidateKind.Type] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotBeInNamespaceAssertion(parameters.GetRequiredString("pattern"));
}
