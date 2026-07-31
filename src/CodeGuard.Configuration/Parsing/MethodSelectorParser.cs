using System.Text.Json.Nodes;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class MethodSelectorParser : ISelectorParser
{
    public string Kind => "method";

    public ITargetSelector Parse(JsonObject node) => new MethodSelector(
        node.GetOptionalString("namespace") ?? "*",
        node.GetOptionalString("project") ?? "*",
        node.GetOptionalString("declaring_type") ?? "*",
        node.GetOptionalString("name") ?? "*",
        node.GetOptionalString("accessibility") is { } accessibility ? EnumParsing.ParseSnakeCase<Accessibility>(accessibility) : null,
        node.GetOptionalBoolNullable("is_async"),
        node.GetOptionalBoolNullable("is_static"));
}
