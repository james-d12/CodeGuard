using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Selectors;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Configuration.Parsing;

public sealed class RepositorySelectorParser : ISelectorParser
{
    public string Kind => "repository";

    public ITargetSelector Parse(JsonObject node) => new RepositorySelector();
}
