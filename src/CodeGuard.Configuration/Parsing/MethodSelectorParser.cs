using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class MethodSelectorParser : ISelectorParser
{
    public string Kind => "method";

    public CapabilityDescriptor Descriptor => new(
        "method",
        "Methods matching the given filters.",
        [
            ParameterDescriptor.OptionalGlob("namespace", "Namespace of the declaring type."),
            ParameterDescriptor.OptionalGlob("project", "Project the site belongs to."),
            ParameterDescriptor.OptionalGlob("declaring_type", "Type declaring the method."),
            ParameterDescriptor.OptionalGlob("name", "Method name."),
            new ParameterDescriptor("accessibility", ParameterType.Enum, false, "Declared accessibility.", AllowedValues: ["public", "private", "protected", "internal", "protected_internal", "private_protected"]),
            ParameterDescriptor.OptionalBool("is_async", "Whether the method is async."),
            ParameterDescriptor.OptionalBool("is_static", "Whether the method is static.")
        ])
    { Produces = CandidateKind.Method };

    public ITargetSelector Parse(JsonObject node) => new MethodSelector(
        node.GetOptionalString("namespace") ?? "*",
        node.GetOptionalString("project") ?? "*",
        node.GetOptionalString("declaring_type") ?? "*",
        node.GetOptionalString("name") ?? "*",
        node.GetOptionalString("accessibility") is { } accessibility ? EnumParsing.ParseSnakeCase<Accessibility>(accessibility) : null,
        node.GetOptionalBoolNullable("is_async"),
        node.GetOptionalBoolNullable("is_static"));
}
