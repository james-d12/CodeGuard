using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class TypeSelectorParser : ISelectorParser
{
    public string Kind => "type";

    public CapabilityDescriptor Descriptor => new(
        "type",
        "All types (class, record, struct, interface, enum) in a matching namespace.",
        [
            ParameterDescriptor.OptionalGlob("namespace", "Namespace the type is declared in."),
            ParameterDescriptor.OptionalGlob("name", "Type name.")
        ])
    { Produces = CandidateKind.Type };

    public ITargetSelector Parse(JsonObject node) =>
        new TypeSelector(
            node.GetOptionalString("namespace") ?? "*",
            node.GetOptionalString("name") ?? "*");
}
