using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Analyzers;
using RulesEngine.RuleModel.Analyzers;

namespace RulesEngine.Configuration.Parsing;

public sealed class NoPureDelegationOverrideAnalyzerParser : IAnalyzerParser
{
    public string Kind => "no-pure-delegation-override";

    public ICustomAnalyzer Parse(JsonObject node) => new NoPureDelegationOverrideAnalyzer(
        node.GetOptionalString("base_type_pattern") ?? "*");
}
