using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class PropertySelectorParser : ISelectorParser
{
    public string Kind => "property";

    public CapabilityDescriptor Descriptor => new(
        "property",
        "Properties matching the given filters.",
        [
            ParameterDescriptor.OptionalGlob("namespace", "Namespace of the declaring type."),
            ParameterDescriptor.OptionalGlob("project", "Project the site belongs to."),
            ParameterDescriptor.OptionalGlob("declaring_type", "Type declaring the property."),
            new ParameterDescriptor("accessibility", ParameterType.Enum, false, "Declared accessibility.", AllowedValues: ["public", "private", "protected", "internal", "protected_internal", "private_protected"]),
            ParameterDescriptor.OptionalBool("is_static", "Whether the property is static.")
        ])
    { Produces = CandidateKind.Property };

    public ITargetSelector Parse(JsonObject node) => new PropertySelector(
        node.GetOptionalString("namespace") ?? "*",
        node.GetOptionalString("project") ?? "*",
        node.GetOptionalString("declaring_type") ?? "*",
        node.GetOptionalString("accessibility") is { } accessibility ? EnumParsing.ParseSnakeCase<Accessibility>(accessibility) : null,
        node.GetOptionalBoolNullable("is_static"));
}
