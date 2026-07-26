using System.Text.Json.Nodes;
using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Selectors;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Configuration.Parsing;

public sealed class MethodSelectorParser : ISelectorParser
{
    public string Kind => "method";

    public ITargetSelector Parse(JsonObject node) => new MethodSelector(
        node.GetOptionalString("namespace") ?? "*",
        node.GetOptionalString("project") ?? "*",
        node.GetOptionalString("declaring_type") ?? "*",
        node.GetOptionalString("accessibility") is { } accessibility ? EnumParsing.ParseSnakeCase<Accessibility>(accessibility) : null,
        node.GetOptionalBoolNullable("is_async"),
        node.GetOptionalBoolNullable("is_static"));
}
