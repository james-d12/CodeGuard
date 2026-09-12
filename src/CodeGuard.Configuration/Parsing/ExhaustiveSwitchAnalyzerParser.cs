using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Analyzers;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public sealed class ExhaustiveSwitchAnalyzerParser : IAnalyzerParser
{
    public string Kind => "exhaustive-switch";

    public CapabilityDescriptor Descriptor => new(
        "exhaustive-switch",
        "Flags switches over an enum that do not cover every member.",
        [
            ParameterDescriptor.OptionalGlob("namespace", "Namespace to analyze.")
        ]);

    public ICustomAnalyzer Parse(JsonObject node) => new ExhaustiveSwitchAnalyzer(
        node.GetOptionalString("namespace") ?? "*");
}
