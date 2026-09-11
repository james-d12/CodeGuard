using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Analyzers;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public sealed class NoExceptionsAnalyzerParser : IAnalyzerParser
{
    public string Kind => "no-exceptions";

    public CapabilityDescriptor Descriptor => new(
        "no-exceptions",
        "Flags throw sites in a namespace.",
        [
            ParameterDescriptor.OptionalGlob("namespace", "Namespace to analyze."),
            new ParameterDescriptor("allow_guard_clause", ParameterType.Bool, false, "Permit a throw that is the method's first statement.", Default: "false")
        ]);

    public ICustomAnalyzer Parse(JsonObject node) => new NoExceptionsAnalyzer(
        node.GetOptionalString("namespace") ?? "*",
        node.GetOptionalBool("allow_guard_clause", false));
}
