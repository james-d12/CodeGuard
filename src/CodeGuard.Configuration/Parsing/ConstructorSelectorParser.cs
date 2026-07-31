using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class ConstructorSelectorParser : ISelectorParser
{
    public string Kind => "constructor";

    public ITargetSelector Parse(JsonObject node) => new ConstructorSelector(
        node.GetOptionalString("declaring_type") ?? "*",
        node["parameter_types"] is JsonArray array ? array.Select(n => n!.GetValue<string>()).ToList() : null);
}
