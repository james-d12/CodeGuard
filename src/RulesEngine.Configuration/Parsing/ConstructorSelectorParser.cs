using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Selectors;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Configuration.Parsing;

public sealed class ConstructorSelectorParser : ISelectorParser
{
    public string Kind => "constructor";

    public ITargetSelector Parse(JsonObject node) => new ConstructorSelector(
        node.GetOptionalString("declaring_type") ?? "*",
        node["parameter_types"] is JsonArray array ? array.Select(n => n!.GetValue<string>()).ToList() : null);
}
