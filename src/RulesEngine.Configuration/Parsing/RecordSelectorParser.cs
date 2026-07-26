using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Selectors;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Configuration.Parsing;

public sealed class RecordSelectorParser : ISelectorParser
{
    public string Kind => "record";

    public ITargetSelector Parse(JsonObject node) =>
        new RecordSelector(node.GetOptionalString("namespace") ?? "*");
}
