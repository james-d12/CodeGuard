using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Analyzers;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public sealed class ImmutableMutationAnalyzerParser : IAnalyzerParser
{
    public string Kind => "immutable-mutation";

    public ICustomAnalyzer Parse(JsonObject node) => new ImmutableMutationAnalyzer(
        node.GetOptionalString("namespace") ?? "*");
}
