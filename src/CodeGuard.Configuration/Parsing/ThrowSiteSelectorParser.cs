using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class ThrowSiteSelectorParser : ISelectorParser
{
    public string Kind => "throw_site";

    public CapabilityDescriptor Descriptor => new(
        "throw_site",
        "Throw sites by thrown exception type.",
        [
            ParameterDescriptor.OptionalGlob("exception_type", "Exception type being thrown."),
            ParameterDescriptor.OptionalBool("is_first_statement_in_method", "Whether the throw is the method's first statement (a guard clause)."),
            ParameterDescriptor.OptionalGlob("containing_type", "Type the site appears in."),
            ParameterDescriptor.OptionalGlob("containing_method", "Method the site appears in."),
            ParameterDescriptor.OptionalGlob("project", "Project the site belongs to.")
        ])
    { Produces = CandidateKind.ThrowSite };

    public ITargetSelector Parse(JsonObject node) => new ThrowSiteSelector(
        node.GetOptionalString("exception_type") ?? "*",
        node.GetOptionalBoolNullable("is_first_statement_in_method"),
        node.GetOptionalString("containing_type") ?? "*",
        node.GetOptionalString("containing_method") ?? "*",
        node.GetOptionalString("project") ?? "*");
}
