using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class MutationSiteSelectorParser : ISelectorParser
{
    public string Kind => "mutation_site";

    public ITargetSelector Parse(JsonObject node) => new MutationSiteSelector(
        node.GetOptionalString("target_member") ?? "*",
        node.GetOptionalString("containing_type") ?? "*",
        node.GetOptionalString("containing_method") ?? "*",
        node.GetOptionalString("project") ?? "*");
}
