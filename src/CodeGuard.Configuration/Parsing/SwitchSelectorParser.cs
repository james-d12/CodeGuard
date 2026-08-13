using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class SwitchSelectorParser : ISelectorParser
{
    public string Kind => "switch";

    public ITargetSelector Parse(JsonObject node) => new SwitchSelector(
        node.GetOptionalString("containing_type") ?? "*",
        node.GetOptionalString("containing_method") ?? "*",
        node.GetOptionalString("project") ?? "*",
        node.GetOptionalBoolNullable("has_default_or_discard_arm"));
}
