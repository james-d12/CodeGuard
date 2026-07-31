using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Analyzers;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public sealed class MemberOrderingAnalyzerParser : IAnalyzerParser
{
    public string Kind => "member-ordering";

    public ICustomAnalyzer Parse(JsonObject node) => new MemberOrderingAnalyzer(
        node["order"]?.AsArray().Select(n => n!.GetValue<string>()).ToList());
}
