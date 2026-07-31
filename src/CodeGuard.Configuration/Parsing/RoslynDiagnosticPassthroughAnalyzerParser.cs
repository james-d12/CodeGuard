using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Analyzers;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public sealed class RoslynDiagnosticPassthroughAnalyzerParser : IAnalyzerParser
{
    public string Kind => "roslyn-diagnostic-passthrough";

    public ICustomAnalyzer Parse(JsonObject node) => new RoslynDiagnosticPassthroughAnalyzer(
        node.GetStringArray("diagnostic_ids"));
}
