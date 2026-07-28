using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Analyzers;
using RulesEngine.RuleModel.Analyzers;

namespace RulesEngine.Configuration.Parsing;

public sealed class ImmutableMutationAnalyzerParser : IAnalyzerParser
{
    public string Kind => "immutable-mutation";

    public ICustomAnalyzer Parse(JsonObject node) => new ImmutableMutationAnalyzer(
        node.GetOptionalString("namespace") ?? "*");
}
