using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class RecordSelectorParser : ISelectorParser
{
    public string Kind => "record";

    public ITargetSelector Parse(JsonObject node) =>
        new RecordSelector(node.GetOptionalString("namespace") ?? "*");
}
