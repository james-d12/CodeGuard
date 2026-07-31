using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Analyzers;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public sealed class NoPureDelegationOverrideAnalyzerParser : IAnalyzerParser
{
    public string Kind => "no-pure-delegation-override";

    public ICustomAnalyzer Parse(JsonObject node) => new NoPureDelegationOverrideAnalyzer(
        node.GetOptionalString("base_type_pattern") ?? "*");
}
