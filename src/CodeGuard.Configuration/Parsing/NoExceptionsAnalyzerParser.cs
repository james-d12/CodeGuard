using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Analyzers;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public sealed class NoExceptionsAnalyzerParser : IAnalyzerParser
{
    public string Kind => "no-exceptions";

    public ICustomAnalyzer Parse(JsonObject node) => new NoExceptionsAnalyzer(
        node.GetOptionalString("namespace") ?? "*",
        node.GetOptionalBool("allow_guard_clause", false));
}
