using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class MutationSiteSelectorParser : ISelectorParser
{
    public string Kind => "mutation_site";

    public CapabilityDescriptor Descriptor => new(
        "mutation_site",
        "Assignments to, or mutations of, a matching member.",
        [
            ParameterDescriptor.OptionalGlob("target_member", "Member being mutated."),
            ParameterDescriptor.OptionalGlob("containing_type", "Type the site appears in."),
            ParameterDescriptor.OptionalGlob("containing_method", "Method the site appears in."),
            ParameterDescriptor.OptionalGlob("project", "Project the site belongs to.")
        ])
    { Produces = CandidateKind.MutationSite };

    public ITargetSelector Parse(JsonObject node) => new MutationSiteSelector(
        node.GetOptionalString("target_member") ?? "*",
        node.GetOptionalString("containing_type") ?? "*",
        node.GetOptionalString("containing_method") ?? "*",
        node.GetOptionalString("project") ?? "*");
}
