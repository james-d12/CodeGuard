using System.Text.Json.Nodes;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

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
