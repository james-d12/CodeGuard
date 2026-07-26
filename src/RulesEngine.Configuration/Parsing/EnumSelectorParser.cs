using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Selectors;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Configuration.Parsing;

public sealed class EnumSelectorParser : ISelectorParser
{
    public string Kind => "enum";

    public ITargetSelector Parse(JsonObject node) =>
        new EnumSelector(node.GetOptionalString("namespace") ?? "*");
}
