using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class ProjectSelectorParser : ISelectorParser
{
    public string Kind => "project";

    public ITargetSelector Parse(JsonObject node) =>
        new ProjectSelector(node.GetRequiredString("name"));
}
