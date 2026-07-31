using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class FileSelectorParser : ISelectorParser
{
    public string Kind => "file";

    public ITargetSelector Parse(JsonObject node) =>
        new FileSelector(node.GetOptionalString("path") ?? "*", node.GetOptionalString("extension"));
}
