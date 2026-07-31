using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class InheritsFromSelectorParser : ISelectorParser
{
    public string Kind => "inherits_from";

    public ITargetSelector Parse(JsonObject node) =>
        new InheritsFromSelector(node.GetRequiredString("type"));
}
