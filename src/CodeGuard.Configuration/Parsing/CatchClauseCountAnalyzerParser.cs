using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Analyzers;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public sealed class CatchClauseCountAnalyzerParser : IAnalyzerParser
{
    public string Kind => "catch-clause-count";

    public CapabilityDescriptor Descriptor => new(
        "catch-clause-count",
        "Flags try blocks whose catch-clause count falls outside the allowed range.",
        [
            ParameterDescriptor.OptionalGlob("namespace", "Namespace to analyze."),
            new ParameterDescriptor("min_catches", ParameterType.Int, false, "Minimum catch clauses, inclusive.", Default: "1"),
            new ParameterDescriptor("max_catches", ParameterType.Int, false, "Maximum catch clauses, inclusive.", Default: "1")
        ]);

    public ICustomAnalyzer Parse(JsonObject node) => new CatchClauseCountAnalyzer(
        node.GetOptionalString("namespace") ?? "*",
        node.GetOptionalInt("min_catches") ?? 1,
        node.GetOptionalInt("max_catches") ?? 1);
}
