using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Analyzers;
using RulesEngine.RuleModel.Analyzers;

namespace RulesEngine.Configuration.Parsing;

public sealed class ExhaustiveSwitchAnalyzerParser : IAnalyzerParser
{
    public string Kind => "exhaustive-switch";

    public ICustomAnalyzer Parse(JsonObject node) => new ExhaustiveSwitchAnalyzer(
        node.GetOptionalString("namespace") ?? "*");
}
