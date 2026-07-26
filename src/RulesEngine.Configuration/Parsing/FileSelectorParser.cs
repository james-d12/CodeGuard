using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Selectors;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Configuration.Parsing;

public sealed class FileSelectorParser : ISelectorParser
{
    public string Kind => "file";

    public ITargetSelector Parse(JsonObject node) =>
        new FileSelector(node.GetOptionalString("path") ?? "*", node.GetOptionalString("extension"));
}
