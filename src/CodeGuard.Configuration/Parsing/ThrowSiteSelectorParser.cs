using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class ThrowSiteSelectorParser : ISelectorParser
{
    public string Kind => "throw_site";

    public ITargetSelector Parse(JsonObject node) => new ThrowSiteSelector(
        node.GetOptionalString("exception_type") ?? "*",
        node.GetOptionalBoolNullable("is_first_statement_in_method"),
        node.GetOptionalString("containing_type") ?? "*",
        node.GetOptionalString("containing_method") ?? "*",
        node.GetOptionalString("project") ?? "*");
}
