using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class MethodBodyShapeSelectorParser : ISelectorParser
{
    public string Kind => "method_body_shape";

    public CapabilityDescriptor Descriptor => new(
        "method_body_shape",
        "Method bodies by statement count and shape.",
        [
            ParameterDescriptor.OptionalInt("min_statement_count", "Minimum statement count, inclusive."),
            ParameterDescriptor.OptionalInt("max_statement_count", "Maximum statement count, inclusive."),
            ParameterDescriptor.OptionalBool("is_single_base_call_delegation", "Whether the body is a single delegating call to base."),
            ParameterDescriptor.OptionalGlob("containing_type", "Type the site appears in."),
            ParameterDescriptor.OptionalGlob("containing_method", "Method the site appears in."),
            ParameterDescriptor.OptionalGlob("project", "Project the site belongs to.")
        ])
    { Produces = CandidateKind.MethodBodyShape };

    public ITargetSelector Parse(JsonObject node) => new MethodBodyShapeSelector(
        node.GetOptionalInt("min_statement_count"),
        node.GetOptionalInt("max_statement_count"),
        node.GetOptionalBoolNullable("is_single_base_call_delegation"),
        node.GetOptionalString("containing_type") ?? "*",
        node.GetOptionalString("containing_method") ?? "*",
        node.GetOptionalString("project") ?? "*");
}
