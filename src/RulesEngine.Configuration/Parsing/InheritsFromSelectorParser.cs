using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Selectors;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Configuration.Parsing;

public sealed class InheritsFromSelectorParser : ISelectorParser
{
    public string Kind => "inherits_from";

    public ITargetSelector Parse(JsonObject node) =>
        new InheritsFromSelector(node.GetRequiredString("type"));
}
