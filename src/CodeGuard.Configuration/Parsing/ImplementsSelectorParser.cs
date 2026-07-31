using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class ImplementsSelectorParser : ISelectorParser
{
    public string Kind => "implements";

    public ITargetSelector Parse(JsonObject node) =>
        new ImplementsSelector(node.GetRequiredString("interface"));
}
