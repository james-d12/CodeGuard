using System.Text.Json.Nodes;
using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Selectors;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Configuration.Parsing;

public sealed class CallSiteSelectorParser : ISelectorParser
{
    public string Kind => "call_site";

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
