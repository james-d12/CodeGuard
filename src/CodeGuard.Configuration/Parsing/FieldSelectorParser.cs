using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class FieldSelectorParser : ISelectorParser
{
    public string Kind => "field";

    public CapabilityDescriptor Descriptor => new(
        "field",
        "Fields matching the given filters.",
        [
            ParameterDescriptor.OptionalGlob("declaring_type", "Type declaring the field."),
            ParameterDescriptor.OptionalBool("is_readonly", "Whether the field is readonly."),
            ParameterDescriptor.OptionalBool("is_static", "Whether the field is static.")
        ])
    { Produces = CandidateKind.Field };

    public ITargetSelector Parse(JsonObject node) => new FieldSelector(
        node.GetOptionalString("declaring_type") ?? "*",
        node.GetOptionalBoolNullable("is_readonly"),
        node.GetOptionalBoolNullable("is_static"));
}
