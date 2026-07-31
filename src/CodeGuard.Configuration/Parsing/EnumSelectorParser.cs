using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class EnumSelectorParser : ISelectorParser
{
    public string Kind => "enum";

    public ITargetSelector Parse(JsonObject node) =>
        new EnumSelector(node.GetOptionalString("namespace") ?? "*");
}
