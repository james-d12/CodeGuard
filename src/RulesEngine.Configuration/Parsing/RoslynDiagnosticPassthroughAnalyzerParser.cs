using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Analyzers;
using RulesEngine.RuleModel.Analyzers;

namespace RulesEngine.Configuration.Parsing;

public sealed class RoslynDiagnosticPassthroughAnalyzerParser : IAnalyzerParser
{
    public string Kind => "roslyn-diagnostic-passthrough";

    public ICustomAnalyzer Parse(JsonObject node) => new RoslynDiagnosticPassthroughAnalyzer(
        node.GetStringArray("diagnostic_ids"));
}
