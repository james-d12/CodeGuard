using System.Text.Json.Nodes;
using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Selectors;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Configuration.Parsing;

public sealed class PropertySelectorParser : ISelectorParser
{
    public string Kind => "property";

    public ITargetSelector Parse(JsonObject node) => new PropertySelector(
        node.GetOptionalString("namespace") ?? "*",
        node.GetOptionalString("project") ?? "*",
        node.GetOptionalString("declaring_type") ?? "*",
        node.GetOptionalString("accessibility") is { } accessibility ? EnumParsing.ParseSnakeCase<Accessibility>(accessibility) : null,
        node.GetOptionalBoolNullable("is_static"));
}
