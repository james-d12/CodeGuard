using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class DiagnosticSelectorParser : ISelectorParser
{
    public string Kind => "diagnostic";

    public ITargetSelector Parse(JsonObject node) => new DiagnosticSelector(
        node.GetOptionalString("id") ?? "*",
        node.GetOptionalString("project") ?? "*");
}
