using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Selectors;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Configuration.Parsing;

public sealed class ImplementsSelectorParser : ISelectorParser
{
    public string Kind => "implements";

    public ITargetSelector Parse(JsonObject node) =>
        new ImplementsSelector(node.GetRequiredString("interface"));
}
