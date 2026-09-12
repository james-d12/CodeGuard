using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class ImplementsSelectorParser : ISelectorParser
{
    public string Kind => "implements";

    public CapabilityDescriptor Descriptor => new(
        "implements",
        "Types implementing a matching interface.",
        [
            ParameterDescriptor.RequiredGlob("interface", "Interface name.")
        ])
    { Produces = CandidateKind.Type };

    public ITargetSelector Parse(JsonObject node) =>
        new ImplementsSelector(node.GetRequiredString("interface"));
}
