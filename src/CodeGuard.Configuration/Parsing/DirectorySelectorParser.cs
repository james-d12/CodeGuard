using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class DirectorySelectorParser : ISelectorParser
{
    public string Kind => "directory";

    public ITargetSelector Parse(JsonObject node) => new DirectorySelector(
        node.GetOptionalString("path") ?? "*");
}
