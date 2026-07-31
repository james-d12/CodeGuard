using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class RepositorySelectorParser : ISelectorParser
{
    public string Kind => "repository";

    public ITargetSelector Parse(JsonObject node) => new RepositorySelector();
}
