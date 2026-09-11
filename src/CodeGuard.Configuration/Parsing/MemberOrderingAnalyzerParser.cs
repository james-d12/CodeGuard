using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Analyzers;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public sealed class MemberOrderingAnalyzerParser : IAnalyzerParser
{
    public string Kind => "member-ordering";

    public CapabilityDescriptor Descriptor => new(
        "member-ordering",
        "Flags types whose members are not declared in the configured order.",
        [
            new ParameterDescriptor("order", ParameterType.StringList, false, "Member kinds in required declaration order. A built-in default is used when omitted.")
        ]);

    public ICustomAnalyzer Parse(JsonObject node) => new MemberOrderingAnalyzer(
        node["order"]?.AsArray().Select(n => n!.GetValue<string>()).ToList());
}
