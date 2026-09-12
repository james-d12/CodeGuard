using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class EnumSelectorParser : ISelectorParser
{
    public string Kind => "enum";

    public CapabilityDescriptor Descriptor => new(
        "enum",
        "Enum types in a matching namespace.",
        [
            ParameterDescriptor.OptionalGlob("namespace", "Namespace the enum is declared in.")
        ])
    { Produces = CandidateKind.Type };

    public ITargetSelector Parse(JsonObject node) =>
        new EnumSelector(node.GetOptionalString("namespace") ?? "*");
}
