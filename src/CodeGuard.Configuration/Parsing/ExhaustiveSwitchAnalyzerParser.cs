using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Analyzers;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public sealed class ExhaustiveSwitchAnalyzerParser : IAnalyzerParser
{
    public string Kind => "exhaustive-switch";

    public ICustomAnalyzer Parse(JsonObject node) => new ExhaustiveSwitchAnalyzer(
        node.GetOptionalString("namespace") ?? "*");
}
