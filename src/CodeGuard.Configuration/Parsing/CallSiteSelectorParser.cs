using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class CallSiteSelectorParser : ISelectorParser
{
    public string Kind => "call_site";

    public CapabilityDescriptor Descriptor => new(
        "call_site",
        "Invocations, object creations and member accesses matching the given filters.",
        [
            new ParameterDescriptor("site_kind", ParameterType.Enum, false, "Which kind of call site.", AllowedValues: ["invocation", "object_creation", "member_access"]),
            ParameterDescriptor.OptionalGlob("invoked_member", "Member being invoked or accessed."),
            ParameterDescriptor.OptionalGlob("target_type", "Type the member belongs to."),
            ParameterDescriptor.OptionalGlob("project", "Project the site belongs to."),
            ParameterDescriptor.OptionalGlob("containing_method", "Method the site appears in."),
            ParameterDescriptor.OptionalGlob("containing_type", "Type the site appears in."),
            ParameterDescriptor.OptionalInt("argument_index", "Zero-based index of the argument to inspect."),
            ParameterDescriptor.OptionalBool("argument_is_literal", "Whether the argument at argument_index is a literal."),
            new ParameterDescriptor("enclosing_comparison", ParameterType.String, false, "Comparison operator enclosing the call site.")
        ])
    { Produces = CandidateKind.CallSite };

    public ITargetSelector Parse(JsonObject node) => new CallSiteSelector(
        node.GetOptionalString("site_kind") is { } siteKind ? EnumParsing.ParseSnakeCase<CallSiteKind>(siteKind) : null,
        node.GetOptionalString("invoked_member") ?? "*",
        node.GetOptionalString("target_type") ?? "*",
        node.GetOptionalString("project") ?? "*",
        node.GetOptionalString("containing_method") ?? "*",
        node.GetOptionalString("containing_type") ?? "*",
        node.GetOptionalInt("argument_index"),
        node.GetOptionalBoolNullable("argument_is_literal"),
        node.GetOptionalString("enclosing_comparison"));
}
