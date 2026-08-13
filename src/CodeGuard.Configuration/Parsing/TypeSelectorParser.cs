using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class TypeSelectorParser : ISelectorParser
{
    public string Kind => "type";

    public ITargetSelector Parse(JsonObject node) =>
        new TypeSelector(
            node.GetOptionalString("namespace") ?? "*",
            node.GetOptionalString("name") ?? "*");
}
