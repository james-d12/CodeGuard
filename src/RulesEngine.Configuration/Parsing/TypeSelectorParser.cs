using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Selectors;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Configuration.Parsing;

public sealed class TypeSelectorParser : ISelectorParser
{
    public string Kind => "type";

    public ITargetSelector Parse(JsonObject node) =>
        new TypeSelector(node.GetOptionalString("namespace") ?? "*");
}
