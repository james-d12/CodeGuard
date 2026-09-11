using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class SwitchSelectorParser : ISelectorParser
{
    public string Kind => "switch";

    public CapabilityDescriptor Descriptor => new(
        "switch",
        "Switch statements and expressions matching the given filters.",
        [
            ParameterDescriptor.OptionalGlob("containing_type", "Type the site appears in."),
            ParameterDescriptor.OptionalGlob("containing_method", "Method the site appears in."),
            ParameterDescriptor.OptionalGlob("project", "Project the site belongs to."),
            ParameterDescriptor.OptionalBool("has_default_or_discard_arm", "Whether the switch has a default or discard arm.")
        ])
    { Produces = CandidateKind.Switch };

    public ITargetSelector Parse(JsonObject node) => new SwitchSelector(
        node.GetOptionalString("containing_type") ?? "*",
        node.GetOptionalString("containing_method") ?? "*",
        node.GetOptionalString("project") ?? "*",
        node.GetOptionalBoolNullable("has_default_or_discard_arm"));
}
