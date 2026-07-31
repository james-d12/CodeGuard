using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Analyzers;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public sealed class CatchClauseCountAnalyzerParser : IAnalyzerParser
{
    public string Kind => "catch-clause-count";

    public ICustomAnalyzer Parse(JsonObject node) => new CatchClauseCountAnalyzer(
        node.GetOptionalString("namespace") ?? "*",
        node.GetOptionalInt("min_catches") ?? 1,
        node.GetOptionalInt("max_catches") ?? 1);
}
