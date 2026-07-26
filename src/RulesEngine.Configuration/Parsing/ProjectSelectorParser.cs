using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Selectors;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Configuration.Parsing;

public sealed class ProjectSelectorParser : ISelectorParser
{
    public string Kind => "project";

    public ITargetSelector Parse(JsonObject node) =>
        new ProjectSelector(node.GetRequiredString("name"));
}
