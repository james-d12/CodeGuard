using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Analyzers;
using RulesEngine.RuleModel.Analyzers;

namespace RulesEngine.Configuration.Parsing;

public sealed class MemberOrderingAnalyzerParser : IAnalyzerParser
{
    public string Kind => "member-ordering";

    public ICustomAnalyzer Parse(JsonObject node) => new MemberOrderingAnalyzer(
        node["order"]?.AsArray().Select(n => n!.GetValue<string>()).ToList());
}
