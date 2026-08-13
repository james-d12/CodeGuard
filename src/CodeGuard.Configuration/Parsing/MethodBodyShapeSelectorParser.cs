using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class MethodBodyShapeSelectorParser : ISelectorParser
{
    public string Kind => "method_body_shape";

    public ITargetSelector Parse(JsonObject node) => new MethodBodyShapeSelector(
        node.GetOptionalInt("min_statement_count"),
        node.GetOptionalInt("max_statement_count"),
        node.GetOptionalBoolNullable("is_single_base_call_delegation"),
        node.GetOptionalString("containing_type") ?? "*",
        node.GetOptionalString("containing_method") ?? "*",
        node.GetOptionalString("project") ?? "*");
}
