using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Selectors;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Configuration.Parsing;

public sealed class FieldSelectorParser : ISelectorParser
{
    public string Kind => "field";

    public ITargetSelector Parse(JsonObject node) => new FieldSelector(
        node.GetOptionalString("declaring_type") ?? "*",
        node.GetOptionalBoolNullable("is_readonly"),
        node.GetOptionalBoolNullable("is_static"));
}
